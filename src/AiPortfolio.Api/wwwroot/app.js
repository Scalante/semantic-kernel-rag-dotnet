// Panel de pruebas de AiPortfolio. Vanilla JS + fetch contra la propia API
// (mismo origen, sin CORS). Sin build: se sirve tal cual desde wwwroot.

const $ = (id) => document.getElementById(id);

// --------------------------------------------------------------------------
// Tabs
// --------------------------------------------------------------------------
(function initTabs() {
  const tabs = [...document.querySelectorAll('.tabs [role="tab"]')];

  function activate(name) {
    for (const t of tabs) {
      const on = t.dataset.tab === name;
      t.setAttribute("aria-selected", String(on));
      $(`panel-${t.dataset.tab}`).hidden = !on;
    }
    try {
      localStorage.setItem("aiportfolio.tab", name);
    } catch {
      /* modo incógnito / storage bloqueado */
    }
  }

  tabs.forEach((t) => t.addEventListener("click", () => activate(t.dataset.tab)));

  let initial = "kb";
  try {
    const saved = localStorage.getItem("aiportfolio.tab");
    if (saved && tabs.some((t) => t.dataset.tab === saved)) initial = saved;
  } catch {
    /* noop */
  }
  activate(initial);
})();

// --------------------------------------------------------------------------
// Botones de ejemplo — rellenan los campos con un clic
// --------------------------------------------------------------------------
(function initExamples() {
  // Un solo campo de texto: [etiqueta, valor]
  const single = {
    "examples-chat": {
      target: "chatQuestion",
      items: [
        ["Vacaciones", "¿Cuántos días de vacaciones acumulo al año y con cuánta anticipación debo pedirlas?"],
        ["Vacaciones anticipadas", "¿Puedo tomar vacaciones si llevo solo 3 meses en la empresa?"],
        ["Trabajo remoto", "¿Qué necesito para que me aprueben trabajar remoto?"],
        ["Soporte TI", "Se me dañó la laptop y no puedo trabajar. ¿Qué hago y en cuánto me responden?"],
        ["Cruza 2 documentos", "¿Cuántas vacaciones tengo al año y a qué canal escribo si se me daña el equipo?"],
        ["Dato que no está", "¿Cuál es el monto del bono de fin de año?"],
        ["General (sin RAG)", "Explícame en dos frases qué es una API REST."],
      ],
    },
    "examples-search": {
      target: "searchQuery",
      items: [
        ["Vacaciones", "cuántos días de vacaciones por mes"],
        ["Trabajo remoto", "requisitos de internet para trabajo remoto"],
        ["Soporte", "mi computador no enciende"],
        ["Onboarding", "qué hago el primer día en la empresa"],
        ["Contraseñas", "rotación de contraseñas y MFA"],
        ["Sin resultados", "receta para hacer arroz"],
      ],
    },
    "examples-classify": {
      target: "ticketText",
      items: [
        ["Bug", "Al exportar el reporte a Excel la aplicación se congela y hay que cerrarla."],
        ["Pregunta", "¿El sistema se puede integrar con Google Calendar?"],
        ["Mejora", "Sería genial poder filtrar la tabla por varias columnas a la vez."],
        ["Ambiguo", "El botón de guardar es poco claro."],
      ],
    },
  };

  for (const [containerId, { target, items }] of Object.entries(single)) {
    const box = $(containerId);
    if (!box) continue;
    box.append(label());
    for (const [text, value] of items) {
      const b = exampleButton(text);
      b.addEventListener("click", () => {
        const el = $(target);
        el.value = value;
        el.focus();
      });
      box.appendChild(b);
    }
  }

  // Documento completo: rellena título + fuente + contenido
  const docExamples = {
    "Política de viajes": {
      title: "politica gastos de viaje",
      source: "politica-viajes.txt",
      content:
        "Los gastos de viaje se reembolsan contra factura dentro de los 10 días hábiles siguientes al regreso. " +
        "El tope diario de alimentación es de 90.000 COP en viajes nacionales y 45 USD en el exterior. " +
        "El transporte en taxi o app está cubierto para trayectos de trabajo; no se cubren multas ni minibar. " +
        "Los vuelos se reservan siempre en clase económica.",
    },
    "Política de horas extra": {
      title: "politica horas extra",
      source: "politica-horas-extra.txt",
      content:
        "Las horas extra deben autorizarse por el líder de equipo ANTES de realizarse. " +
        "Se pagan al 125% en días hábiles y al 175% en domingos y festivos. " +
        "El máximo permitido es de 2 horas extra por día y 12 por semana. " +
        "El personal de dirección no genera horas extra.",
    },
  };

  const docBox = $("examples-doc");
  if (docBox) {
    docBox.append(label());
    for (const [text, doc] of Object.entries(docExamples)) {
      const b = exampleButton(text);
      b.addEventListener("click", () => {
        $("docTitle").value = doc.title;
        $("docSource").value = doc.source;
        $("docContent").value = doc.content;
      });
      docBox.appendChild(b);
    }
  }

  function label() {
    const s = document.createElement("span");
    s.className = "examples-label";
    s.textContent = "Ejemplos:";
    return s;
  }
  function exampleButton(text) {
    const b = document.createElement("button");
    b.type = "button";
    b.className = "example-btn";
    b.textContent = text;
    return b;
  }
})();

/** Llama a la API. Lanza Error con el cuerpo si la respuesta no es 2xx. */
async function api(path, { method = "GET", body } = {}) {
  const res = await fetch(path, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} en ${path}\n${text.slice(0, 800)}`);
  }
  return text ? JSON.parse(text) : null;
}

function showError(err) {
  const banner = $("errorBanner");
  banner.textContent = String(err.message || err);
  banner.hidden = false;
}
function clearError() {
  $("errorBanner").hidden = true;
}

/** Envuelve un handler de submit: limpia error, pone el botón en "busy". */
function wire(formId, btnId, handler) {
  $(formId).addEventListener("submit", async (e) => {
    e.preventDefault();
    clearError();
    const btn = $(btnId);
    btn.setAttribute("aria-busy", "true");
    try {
      await handler();
    } catch (err) {
      showError(err);
    } finally {
      btn.removeAttribute("aria-busy");
    }
  });
}

const pct = (score) => `${Math.round((score ?? 0) * 100)}%`;

// --------------------------------------------------------------------------
// Estado
// --------------------------------------------------------------------------
(async function loadStatus() {
  const bar = $("statusBar");
  try {
    const h = await api("/api/health");
    bar.className = "status ok";
    bar.innerHTML =
      `API <strong>ok</strong> · LLM: <strong>${h.llm.provider}/${h.llm.model}</strong>` +
      ` · Embeddings: <strong>${h.embeddings.provider}/${h.embeddings.model}</strong>` +
      ` · umbral relevancia: <strong>${h.embeddings.minRelevanceScore}</strong>`;
  } catch (err) {
    bar.className = "status down";
    bar.textContent = "No se pudo contactar la API. ¿Está corriendo? " + err.message;
  }
})();

// --------------------------------------------------------------------------
// 1 · Base de conocimiento
// --------------------------------------------------------------------------
$("seedBtn").addEventListener("click", async () => {
  clearError();
  const btn = $("seedBtn");
  btn.setAttribute("aria-busy", "true");
  try {
    const r = await api("/api/rag/seed", { method: "POST" });
    $("seedResult").textContent = `✓ ${r.documentosIngestados} documentos ingestados`;
  } catch (err) {
    showError(err);
  } finally {
    btn.removeAttribute("aria-busy");
  }
});

wire("ingestForm", "ingestBtn", async () => {
  await api("/api/rag/documents", {
    method: "POST",
    body: {
      title: $("docTitle").value.trim(),
      source: $("docSource").value.trim(),
      content: $("docContent").value.trim(),
    },
  });
  $("ingestResult").textContent = "✓ documento ingestado";
  $("ingestForm").reset();
});

// --------------------------------------------------------------------------
// 2 · Chat
// --------------------------------------------------------------------------
let conversationId = null;

function renderConvId() {
  $("convId").textContent = conversationId ? `conv: ${conversationId.slice(0, 8)}…` : "";
}

$("chatResetBtn").addEventListener("click", () => {
  conversationId = null;
  renderConvId();
  $("chatAnswer").hidden = true;
});

wire("chatForm", "chatBtn", async () => {
  const question = $("chatQuestion").value.trim();
  const r = await api("/api/chat", {
    method: "POST",
    body: { question, conversationId },
  });
  conversationId = r.conversationId;
  renderConvId();

  $("chatAnswerText").textContent = r.answer;
  const chips = $("chatSources");
  chips.innerHTML = "";
  for (const s of r.sourcesUsed ?? []) {
    const el = document.createElement("span");
    el.className = "chip";
    el.textContent = s;
    chips.appendChild(el);
  }
  $("chatAnswer").hidden = false;
});

// --------------------------------------------------------------------------
// 3 · Búsqueda vectorial
// --------------------------------------------------------------------------
wire("searchForm", "searchBtn", async () => {
  const query = encodeURIComponent($("searchQuery").value.trim());
  const topK = $("searchTopK").value || 3;
  const results = await api(`/api/rag/search?query=${query}&topK=${topK}`, { method: "POST" });

  const box = $("searchResults");
  box.innerHTML = "";
  if (!results.length) {
    box.innerHTML = `<p class="muted">Sin resultados por encima del umbral de relevancia.</p>`;
    return;
  }
  for (const r of results) {
    const div = document.createElement("div");
    div.className = "result";
    div.innerHTML = `
      <h4>${escapeHtml(r.title)} <span class="muted mono">${escapeHtml(r.source)}</span></h4>
      <div class="snippet">${escapeHtml(r.content)}</div>
      <div class="bar-wrap">
        <div class="bar"><div class="bar-fill" style="width:${pct(r.score)}"></div></div>
        <span class="mono muted">${(r.score ?? 0).toFixed(3)}</span>
      </div>`;
    box.appendChild(div);
  }
});

// --------------------------------------------------------------------------
// 4 · Clasificador de tickets
// --------------------------------------------------------------------------
wire("classifyForm", "classifyBtn", async () => {
  const r = await api("/api/tickets/classify", {
    method: "POST",
    body: { description: $("ticketText").value.trim() },
  });
  $("ticketCategory").textContent = r.category;
  $("ticketBar").style.width = pct(r.confidence);
  $("ticketConfidence").textContent = (r.confidence ?? 0).toFixed(3);
  $("classifyResult").hidden = false;
});

// --------------------------------------------------------------------------
function escapeHtml(s) {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;",
  }[c]));
}
