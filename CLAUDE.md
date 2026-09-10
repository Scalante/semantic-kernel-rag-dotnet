# CLAUDE.md — contexto permanente para asistentes de IA

Este archivo lo lee la IA al empezar. Las personas: ver [`README.md`](README.md).

## Leer primero

1. [`docs/OVERVIEW.md`](docs/OVERVIEW.md) — qué resuelve el proyecto, conceptos y
   casos de uso empresariales. **El *por qué*.**
2. [`README.md`](README.md) — cómo arrancarlo, probarlo y configurarlo.
3. [`CONTRIBUTING.md`](CONTRIBUTING.md) — flujo de Git y checklist de "cambio
   terminado". **De cumplimiento obligatorio.**
4. [`ROADMAP.md`](ROADMAP.md) — la vacante objetivo y el plan por fases.

## Qué es esto

Proyecto de **portafolio** para una vacante de "Desarrollador Back End .NET
Senior experto en IA". Backend .NET 10, Clean Architecture, que demuestra:
Microsoft.Extensions.AI · Semantic Kernel (agentes, function calling) · RAG ·
pgvector · ML.NET. El objetivo es poder **defender cada línea en una entrevista**.

## Reglas que no se saltan

- **No hacer `git commit`, `git push`, ni abrir/mergear PRs sin aprobación
  explícita de la persona.** Ver `CONTRIBUTING.md` paso 3.
- Antes de dar un cambio por terminado: `dotnet build` (0/0), `dotnet test` (en
  verde) y **verificado en vivo** corriendo la API. Checklist completo en
  `CONTRIBUTING.md`.
- Versiones de paquetes **explícitas** (sin `*`). Toda la línea de Semantic
  Kernel está fijada a **1.74** a propósito: el conector
  `Microsoft.SemanticKernel.Connectors.PgVector` solo se publica hasta
  `1.74.0-preview` y las versiones nuevas de `VectorData.Abstractions` rompen su
  API de búsqueda. No subir SK a 1.80 sin resolver eso.
- Respetar las capas: `Domain` sin dependencias · `Application` solo
  interfaces/DTOs · el detalle de proveedor (OpenAI/Ollama/pgvector) no sale de
  `KernelFactory` / `EmbeddingGeneratorFactory` · `Api` solo compone.
- Documentación al día **en el mismo cambio** (README / OVERVIEW / comentarios XML).

## Datos del entorno (a fecha de la última sesión)

- **.NET 10 SDK** (`global.json` lo fija). TFM común en `Directory.Build.props`.
- **Base de datos**: contenedor Docker `pgvector/pgvector:pg16` en
  `localhost:55432` (5432 y 5433 los ocupan Postgres nativos en esta máquina).
  `docker compose up -d`. Base `aiportfolio`, user/pass `postgres`/`postgres`.
- **IA local**: Ollama en `http://localhost:11434`. Modelos: `nomic-embed-text`
  (embeddings RAG, 768 dim) y `llama3.1` (chat con function calling; los `gemma`
  no soportan tools). Sin API keys.
- **Puertos de la API**: perfil `http` → `http://localhost:5080`, perfil `https`
  → `https://localhost:7080`. Definidos en `Properties/launchSettings.json`
  (fijos y altos porque el rango 50000+ que asigna VS está reservado por Windows).
- **Re-seed**: la ingesta es idempotente (el `Id` del documento se deriva de su
  `Source`), así que repetir `/api/rag/seed` sobrescribe y no duplica. La búsqueda
  además colapsa duplicados por `Source`. Datos duplicados de pruebas antiguas se
  limpian con `TRUNCATE knowledge_documents;` y un `/api/rag/seed`.
