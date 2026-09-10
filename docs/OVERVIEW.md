# Visión general — de qué trata AiPortfolio, para qué sirve y dónde se aplica

> **Lectura recomendada antes de tocar el proyecto.** Explica el *por qué* detrás
> del código: qué problema resuelve cada pieza, los conceptos que hay que
> manejar, los beneficios y los escenarios reales de uso en una empresa.
> Para el *cómo* (arrancar, probar, configurar) ver [`../README.md`](../README.md).

---

## 1. Qué es, en una frase

Un **backend en .NET que le pone "cerebro documental" y automatización a un
asistente conversacional**: responde preguntas usando los documentos y datos
reales de la empresa (no lo que el modelo memorizó de internet), puede
**ejecutar acciones** (consultar un ERP, crear una cotización, agendar), e
incluye un modelo de **ML clásico** para clasificación/predicción. Todo
desacoplado del proveedor de IA (hoy Ollama local, mañana OpenAI, sin tocar el
código de negocio).

No es "un chatbot". Es la **plantilla de infraestructura** para construir
cualquier asistente empresarial serio.

---

## 2. Las 4 piezas y qué problema resuelve cada una

### Pieza A — RAG (respuestas fundamentadas en tus datos)

**Problema:** un LLM "pelado" inventa. Si le preguntas "¿cuál es la política de
descuentos de mi empresa?", se la imagina. Un vendedor no puede decirle a un
cliente "sí, integramos con SAP" si es mentira.

**Cómo lo resuelve:** antes de responder, el sistema busca en la base de
conocimiento (por *significado*, no por palabras clave) los fragmentos más
relevantes y se los da al modelo como contexto. El modelo redacta **solo con
eso**. Si nada supera el umbral de similitud, lo dice en vez de inventar.

**Beneficio:** menos respuestas incorrectas, trazabilidad (`sourcesUsed` dice de
qué documento salió), y datos **actualizables al instante** — subes un documento
nuevo y ya está disponible, sin reentrenar nada.

### Pieza B — Function calling / Agente (ejecutar acciones, no solo hablar)

**Problema:** un asistente que solo responde texto es una enciclopedia. El valor
está en que **haga cosas**: "créame la cotización", "abre el ticket", "consúltame
el stock".

**Cómo lo resuelve:** se le da al modelo un catálogo de funciones. El modelo
decide **por su cuenta** cuál necesita, el código la ejecuta contra el sistema
real (ERP, CRM, calendario) y el modelo continúa con el resultado.

**Beneficio:** se pasa de "asistente que informa" a "asistente que opera".

### Pieza C — ML.NET (clasificación / predicción, no generación)

**Problema:** no todo es texto libre. "Clasifica 5.000 tickets/día", "¿este lead
es caliente o frío?", "¿este cliente va a cancelar?". Para eso un LLM es caro,
lento y no determinista.

**Cómo lo resuelve:** un modelo entrenado con datos históricos etiquetados.
Rápido (milisegundos), barato (corre en tu servidor) y determinista.

**Beneficio:** automatización de decisiones repetitivas a escala, con costo casi
nulo por predicción.

### Pieza D — Agnóstico al proveedor

**Problema:** si atas el código al SDK de OpenAI y mañana quieres cambiar por
costo, privacidad o regulación, reescribes medio sistema.

**Cómo lo resuelve:** el código de negocio depende de interfaces (`IChatClient`,
`IEmbeddingGenerator`). Cambiar de Ollama on-premise a OpenAI a Azure es cambiar
`appsettings.json`.

**Beneficio:** datos sensibles (precios, nómina, contratos) pueden procesarse
**on-premise con Ollama** sin salir de la red; y lo que necesite más calidad, a
OpenAI. Decisión de arquitectura, no de reescritura.

---

## 3. Conceptos que hay que saber

| Concepto | Qué es | Dónde vive en el repo |
|---|---|---|
| **LLM** | Modelo que predice el siguiente *token*; de ahí sale responder, resumir, traducir. No "sabe" datos como una BD; genera lo más probable. | `KernelFactory.cs` |
| **Token / context window** | Unidad mínima de texto (~4 chars); la ventana de contexto es cuántos tokens caben entre entrada y salida. | — |
| **System prompt** | Instrucción de rol/comportamiento que va antes de la conversación. | `ChatAgentService.cs` |
| **Alucinación** | Cuando el modelo inventa datos con seguridad. Es *el* problema que ataca RAG. | — |
| **Embedding** | Convertir un texto en un **vector** (lista de números) que representa su *significado*. Textos parecidos → vectores cercanos. | `EmbeddingGeneratorFactory.cs` |
| **Modelo de embeddings ≠ LLM de chat** | Familias distintas: uno genera texto, el otro genera vectores. `nomic-embed-text` (768 dim) vs `llama3.1`. | `appsettings.json` → `Rag` |
| **Dimensión del vector** | Fija por modelo (nomic = 768, text-embedding-3-small = 1536). Si cambias de modelo, cambias la columna y reindexas. | `KnowledgeDocumentRecord.cs` |
| **Similitud de coseno** | Métrica de qué tan "alineados" están dos vectores (1 = idénticos). Es como se busca. | `KnowledgeDocumentRecord.cs` (`DistanceFunction`) |
| **Base de datos vectorial** | Almacén optimizado para "dame los N más parecidos a este vector". Aquí: **pgvector**, extensión de PostgreSQL. | `docker-compose.yml`, `PostgresRagService.cs` |
| **top-K** | Cuántos resultados trae la búsqueda (aquí 3). | `PostgresRagService.cs` |
| **Umbral de relevancia** | Score mínimo de similitud para no pasar "ruido" al LLM ni citarlo como fuente. | `RagOptions.MinRelevanceScore` (0.65) |
| **RAG** | Retrieval (buscar) → Augmented (pegar al prompt) → Generation (el LLM responde con eso). | `PostgresRagService` + `KnowledgeBasePlugin` |
| **Chunking** | Partir documentos largos en trozos antes de generar embeddings. | `SampleKnowledgeSeeder.cs` (aquí, 1 doc = 1 trozo) |
| **Grounding** | "Aterrizar" la respuesta en fuentes reales. | `KnowledgeBasePlugin.cs` |
| **Function calling / tools** | El LLM recibe una lista de funciones y decide llamarlas; el código las ejecuta y le devuelve el resultado. | `[KernelFunction]` en `KnowledgeBasePlugin.cs` |
| **Agente / prompt chaining** | LLM en bucle: razona → llama tool → observa → repite. La cadena pregunta→búsqueda→respuesta es el chaining. | `ChatAgentService.cs` |
| **Abstracción agnóstica** | Programar contra `IChatClient` / `IEmbeddingGenerator`, no contra el SDK concreto. | `KernelFactory.cs`, `EmbeddingGeneratorFactory.cs` |
| **ML supervisado (ML.NET)** | Aprende de ejemplos etiquetados a predecir una categoría/valor. Featurización de texto = TF-IDF/n-gramas. No es generativo. | `TicketClassifierTrainer.cs` |

**Regla mental:** LLM/RAG para lo que varía y es lenguaje; ML.NET para lo
repetitivo y estructurado con datos etiquetados.

---

## 4. Beneficios

### Técnicos
- **Menos alucinaciones** → confiable para uso real, no solo demos.
- **Conocimiento actualizable sin reentrenar** → subes documentos, listo.
- **Trazabilidad** → cada respuesta cita su fuente; auditable.
- **Portabilidad de proveedor** → sin *vendor lock-in*.
- **Clean Architecture** → cada pieza se prueba y reemplaza aislada.
- **Coste controlado** → embeddings y ML corren local; el LLM caro solo cuando aporta.

### De negocio
- **Reduce el tiempo de búsqueda de información** — se pregunta en lenguaje natural.
- **Estandariza respuestas** — todos responden la política oficial, no lo que cada quien recuerda.
- **Onboarding más rápido** — el empleado nuevo pregunta al asistente, no interrumpe a un senior.
- **Escala el conocimiento de los expertos** — lo que sabe "la persona de 10 años" queda accesible 24/7.
- **Automatiza tareas de bajo valor** — clasificar, enrutar, priorizar, generar borradores.

---

## 5. Escenarios de aplicación por área

### Ventas / Comercial

Base de conocimiento: catálogo, listas de precios, política de descuentos y
aprobaciones, contratos tipo, *battle cards* vs. competidores, casos de éxito por
industria, FAQ técnicas.

| El vendedor pregunta / pide | Qué usa el sistema |
|---|---|
| "¿Descuento máximo sin aprobación de gerencia en un deal de USD 50k?" | **RAG** sobre la política de descuentos |
| "¿Tenemos integración nativa con SAP S/4HANA?" | **RAG** sobre documentación de producto |
| "Dame 3 casos de éxito en retail" | **RAG** sobre repositorio de casos |
| "¿Qué nos diferencia de [competidor] en seguridad?" | **RAG** sobre *battle cards* |
| "Crea una cotización de 100 licencias con 12% de descuento en PDF" | **Function calling** → precios en ERP + PDF |
| "Registra esta oportunidad en el CRM" | **Function calling** → API del CRM |
| "Agéndame una demo el jueves" | **Function calling** → calendario |
| Un lead entra por el formulario web | **ML.NET** → *lead scoring* (caliente/tibio/frío) para priorizar |
| Cartera de clientes actuales | **ML.NET** → predicción de *churn* para actuar antes |

### Atención al cliente / Soporte

- **RAG** sobre manuales, base de conocimiento e histórico de tickets resueltos.
- **ML.NET** → clasificación y enrutamiento automático de tickets; detección de *sentiment* negativo para escalar. *(Este proyecto ya trae el clasificador de tickets como demo.)*
- **Function calling** → "reinicia el servicio del cliente 4471", "genera una nota de crédito", "consulta el estado del envío".

### RRHH / Talento

- **RAG** sobre políticas (vacaciones, trabajo remoto, beneficios, viáticos), convenios, manual del empleado. *(Demo de este proyecto.)*
- **Function calling** → "solicita mis vacaciones del 10 al 20", "descárgame mi certificado laboral".
- **ML.NET** → cribado inicial de CVs, predicción de rotación.

### Legal / Compliance

- **RAG** sobre contratos firmados, cláusulas tipo, normativa aplicable.
- **Function calling** → generar borrador de contrato desde plantilla.
- **ML.NET** → clasificación de documentos entrantes, detección de cláusulas de riesgo.

### Operaciones / Logística / Manufactura

- **RAG** sobre procedimientos (SOPs), fichas técnicas, manuales de maquinaria.
- **Function calling** → "consulta inventario en bodega Norte", "crea una orden de compra".
- **ML.NET** → mantenimiento predictivo (anomalías en sensores), previsión de demanda.

### Finanzas

- **RAG** sobre políticas de gasto, manual de cierre contable, normativa fiscal.
- **Function calling** → "concilia estas 3 cuentas", "genera el reporte de gastos del Q3".
- **ML.NET** → detección de anomalías/fraude, categorización automática de gastos.

### Marketing

- **RAG** sobre guía de marca, mensajes clave, campañas pasadas → borradores *on-brand*.
- **ML.NET** → segmentación de clientes, predicción de respuesta a campaña.

---

## 6. Cómo elegir qué pieza usar

| Si la tarea es… | Usa |
|---|---|
| Responder preguntas sobre documentos internos | **RAG** |
| Que el asistente ejecute algo en otro sistema | **Function calling** |
| Clasificar / puntuar / predecir a gran escala, con datos etiquetados | **ML.NET** |
| Resumir, redactar, conversar sobre texto libre | **LLM** directo |
| Datos sensibles que no pueden salir de la red | **Ollama on-premise** (agnóstico) |

---

## 7. Dónde NO aplica (para ser honesto en la entrevista)

- **Cálculos exactos y críticos** (contabilidad, nómina): el LLM redacta, pero el número lo pone un sistema determinista vía function calling, nunca el modelo.
- **Decisiones legales/médicas vinculantes**: asiste, no decide.
- **Cuando no hay documentos que indexar**: RAG no tiene de dónde sacar; primero hay que tener el conocimiento escrito.
- **Volumen bajo**: si son 10 consultas al mes, un buscador normal alcanza.

---

## 8. El pitch para la entrevista

> "Construí un backend en .NET 10 con Clean Architecture que integra los 5
> pilares de IA aplicada que pide la vacante, y los tres —Microsoft.Extensions.AI,
> Semantic Kernel y RAG— trabajando juntos en un caso real: un asistente
> empresarial que responde con los documentos de la compañía usando búsqueda
> vectorial en pgvector, decide por su cuenta cuándo consultar la base vía
> function calling, filtra los resultados por score de similitud para no citar
> ruido, y es agnóstico al proveedor: corre con Ollama on-premise para datos
> sensibles o con OpenAI cambiando una línea de configuración. Aparte, un módulo
> de ML.NET —clasificación de tickets— como contraste entre IA generativa y ML
> supervisado. Está probado de punta a punta: `docker compose up`, seed de
> documentos, y el endpoint de chat respondiendo con datos reales."

---

## 9. Mapa: pieza ↔ archivo

| Pieza | Archivos clave |
|---|---|
| LLM agnóstico | `src/AiPortfolio.Infrastructure.AI/KernelFactory.cs` |
| Agente + function calling | `src/AiPortfolio.Infrastructure.AI/ChatAgentService.cs`, `Plugins/KnowledgeBasePlugin.cs` |
| RAG (retrieval + filtro por score) | `src/AiPortfolio.Infrastructure.Rag/PostgresRagService.cs` |
| Embeddings agnósticos | `src/AiPortfolio.Infrastructure.Rag/EmbeddingGeneratorFactory.cs` |
| BD vectorial (pgvector) | `src/AiPortfolio.Infrastructure.Rag/Records/KnowledgeDocumentRecord.cs`, `docker-compose.yml`, `init-pgvector.sql` |
| ML.NET (clasificación) | `src/AiPortfolio.Infrastructure.MLNet/` |
| Endpoints REST | `src/AiPortfolio.Api/Program.cs` |
| Interfaces / DTOs (contratos entre capas) | `src/AiPortfolio.Application/` |
