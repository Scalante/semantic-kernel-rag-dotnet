# AiPortfolio — Backend .NET con IA aplicada (proyecto de portafolio)

Proyecto de portafolio construido para demostrar, con código real y ejecutable,
los 5 temas que pide la vacante **"Desarrollador Back End .NET Senior Experto
en IA"**: Microsoft.Extensions.AI, Semantic Kernel (agentes, function calling,
prompt chaining), RAG, bases de datos vectoriales (pgvector) y ML.NET.

Ver [`ROADMAP.md`](ROADMAP.md) para el resumen completo de la vacante, el
razonamiento detrás de cada decisión técnica y los enlaces oficiales de
Microsoft Learn usados como referencia para cada módulo.

Documentación del repositorio:

- [`docs/OVERVIEW.md`](docs/OVERVIEW.md) — **lectura recomendada**: qué resuelve
  cada pieza (RAG, function calling, ML.NET, agnóstico al proveedor), los
  conceptos que hay que saber y los escenarios de aplicación en una empresa
  (ventas, soporte, RRHH, legal, operaciones, finanzas).
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — flujo de trabajo con Git y qué revisar
  antes de dar por terminado un cambio. Aplica a personas y asistentes de IA.

## Estado del proyecto

El scaffold inicial se escribió siguiendo la documentación oficial de Microsoft
Learn para cada paquete (enlaces citados como comentario en cada archivo). Sobre
esa base:

- **Target framework: .NET 10** (LTS). Requiere el SDK de .NET 10 instalado.
- `dotnet build` de la solución completa: **compila sin errores ni advertencias**.
- `dotnet test`: **3/3 pruebas de ML.NET en verde**.
- Flujo RAG completo verificado de punta a punta con Ollama + Postgres/pgvector
  (Docker): `POST /api/rag/seed` → `POST /api/chat` responde con el dato exacto
  de los documentos, usando function calling.
- Paquetes fijados a versiones explícitas y **coherentes entre sí**: toda la
  línea de Semantic Kernel en **1.74** (`Microsoft.SemanticKernel` 1.74.0,
  `...Connectors.OpenAI` 1.74.0, `...Connectors.Ollama` 1.74.0-alpha,
  `...Connectors.PgVector` 1.74.0-preview), `Microsoft.Extensions.VectorData.Abstractions`
  10.1.0, `Microsoft.Extensions.AI` 10.4.1. Se fija a 1.74 (y no a la última 1.80)
  porque el conector de Postgres solo se publica hasta 1.74.0-preview y las
  versiones posteriores de `VectorData.Abstractions` rompen su API de búsqueda.

Lo que todavía requiere infraestructura externa para probarse de punta a punta
(no cubierto por el build ni los tests):

1. Postgres + pgvector con la base `aiportfolio` (`docker compose up -d`, o
   crearla a mano — ver sección 2b).
2. Ollama local con dos modelos descargados: `nomic-embed-text` (embeddings) y
   uno conversacional con function calling como `llama3.1`. Sin API keys.

El endpoint `POST /api/tickets/classify` (ML.NET) funciona sin nada de lo
anterior.

El objetivo sigue siendo el mismo: que puedas **defender cada línea en la
entrevista**, no solo que compile.

## Qué incluye

| Tema de la vacante | Dónde está en el código |
|---|---|
| Microsoft.Extensions.AI (integración agnóstica de LLM) | `src/AiPortfolio.Infrastructure.AI/KernelFactory.cs` |
| Semantic Kernel — agentes, function calling, prompt chaining | `src/AiPortfolio.Infrastructure.AI/ChatAgentService.cs`, `Plugins/KnowledgeBasePlugin.cs` |
| RAG (Retrieval-Augmented Generation) | `src/AiPortfolio.Infrastructure.Rag/PostgresRagService.cs` |
| Bases de datos vectoriales (pgvector en Postgres) | `src/AiPortfolio.Infrastructure.Rag/Records/KnowledgeDocumentRecord.cs`, `docker-compose.yml` |
| ML.NET (clasificación clásica) | `src/AiPortfolio.Infrastructure.MLNet/` |

Arquitectura: Clean Architecture (Domain → Application → Infrastructure → Api),
la misma que ya conoces de tu curso de arquitectura backend empresarial.

## Cómo correrlo localmente

### 1. Levantar Postgres + pgvector (con Docker — recomendado)

```bash
docker compose up -d
```

Esto crea un contenedor con Postgres 16 + `pgvector`, escuchando en
`localhost:55432` (puerto alto para no chocar con instalaciones nativas de
PostgreSQL en 5432/5433). En el primer arranque ejecuta `init-pgvector.sql` y
deja la base `aiportfolio` lista con `CREATE EXTENSION vector`. No hay nada más
que configurar.

Comprobar que quedó bien:

```bash
docker exec -it aiportfolio-postgres psql -U postgres -d aiportfolio -c "\dx"
```

Debe listar la extensión `vector`.

### 2. Elegir proveedor de LLM y de embeddings

Todo el proyecto puede correr **sin ninguna API key** usando
[Ollama](https://ollama.com) local. `appsettings.json` ya viene configurado así
por defecto (`Ai:Provider = "Ollama"`, `Rag:EmbeddingProvider = "Ollama"`).

```bash
ollama serve
ollama pull nomic-embed-text   # embeddings del módulo RAG (768 dimensiones)
ollama pull llama3.1           # LLM conversacional CON soporte de function calling
```

> **Function calling:** la demo central (`/api/chat` usando RAG por su cuenta)
> necesita un modelo con soporte de *tools*. `llama3.1` y `qwen2.5` lo tienen;
> los modelos `gemma` **no**. Ajusta `Ai:ModelId` al modelo que hayas descargado.

**Alternativa con OpenAI:**
- LLM: `Ai:Provider = "OpenAi"`, `Ai:ModelId = "gpt-4o-mini"`, key en `Ai:ApiKey`.
- Embeddings: `Rag:EmbeddingProvider = "OpenAi"`, `Rag:EmbeddingModelId =
  "text-embedding-3-small"`, key en `OpenAI:ApiKey`, y cambia la dimensión del
  vector en `KnowledgeDocumentRecord` de **768 → 1536** (y recrea la colección).

> **Relevancia (`Rag:MinRelevanceScore`, por defecto `0.65`):** la búsqueda
> vectorial descarta los documentos cuya similitud de coseno con la pregunta
> quede por debajo de este umbral, de modo que `ChatResponse.SourcesUsed` solo
> cite documentos que realmente aportan. `POST /api/rag/search` devuelve el
> `score` de cada resultado para poder calibrarlo. Con `nomic-embed-text` los
> scores quedan comprimidos (un acierto ~0.72, el ruido ~0.52), por eso 0.65.

### 2b. Alternativa: Postgres nativo (sin Docker)

Solo si ya tienes PostgreSQL instalado **y con la extensión `pgvector`
disponible** (`SELECT * FROM pg_available_extensions WHERE name='vector'` debe
devolver una fila; en Windows suele haber que instalarla aparte):

```sql
CREATE DATABASE aiportfolio;
\c aiportfolio
CREATE EXTENSION IF NOT EXISTS vector;
```

Y ajusta el puerto en `appsettings.json` → `ConnectionStrings:Postgres`
(normalmente `Port=5432` para una instalación nativa).

### 3. Restaurar, compilar y ejecutar

```bash
dotnet restore
dotnet build
dotnet run --project src/AiPortfolio.Api --launch-profile http
```

El perfil `http` escucha en `http://localhost:5080` (el perfil `https` usa
`https://localhost:7080`). Swagger queda en `http://localhost:5080/swagger`.

> Los puertos viven en `src/AiPortfolio.Api/Properties/launchSettings.json`. Se
> eligieron altos y fijos porque los que asigna Visual Studio por defecto (rango
> 50000+) caen en el rango reservado de Windows/Hyper-V y Kestrel no puede
> enlazarlos.

### 4. Probar el flujo de RAG end-to-end

```bash
# 1) Carga los documentos de ejemplo (políticas ficticias de una empresa)
curl -X POST http://localhost:5080/api/rag/seed

# 2) Pregúntale al agente algo que solo puede responder buscando en esos documentos
curl -X POST http://localhost:5080/api/chat \
  -H "Content-Type: application/json" \
  -d '{"question": "¿Cuántos días de vacaciones acumulo por mes?"}'
```

La respuesta trae `sourcesUsed` con el/los documento(s) que el agente realmente
consultó (p. ej. `["politica-vacaciones.txt"]`), o vacío si respondió sin RAG.

El agente debería usar function calling (`search_company_knowledge`) para
recuperar el fragmento correcto de `politica-vacaciones.txt` y responder con
esa información — esa es la demo central de RAG + Semantic Kernel para la
entrevista.

### 5. Probar el clasificador de ML.NET

```bash
curl -X POST http://localhost:5080/api/tickets/classify \
  -H "Content-Type: application/json" \
  -d '{"description": "La aplicación se cierra sola al exportar un PDF"}'
```

Debería devolver `"category": "Bug"` con una confianza (score) asociada.

### 6. Correr las pruebas

```bash
dotnet test
```

## Qué mostrar en la entrevista

- **"Implementé RAG de punta a punta"**: mostrar `PostgresRagService.cs` +
  `KnowledgeBasePlugin.cs` + la demo del endpoint `/api/chat` respondiendo con
  datos reales de los documentos ingeridos.
- **"Usé Semantic Kernel para function calling y agentes"**:
  `ChatAgentService.cs`, explicando `FunctionChoiceBehavior.Auto()` y cómo el
  LLM decide por sí mismo si necesita buscar contexto antes de responder.
- **"Diseñé la integración con LLMs de forma agnóstica al proveedor"**:
  `KernelFactory.cs`, mostrando que cambiar de OpenAI a Ollama es cambiar una
  configuración, no el código de negocio.
- **"También trabajé con ML.NET, no solo con LLMs"**: `TicketClassifierService.cs`
  y la diferencia conceptual entre IA generativa (LLMs) y ML clásico
  (clasificación supervisada), que es exactamente la distinción que pide la
  vacante.

## Estructura del proyecto

Ver el diagrama completo en [`ROADMAP.md`](ROADMAP.md#4-estructura-del-proyecto).
