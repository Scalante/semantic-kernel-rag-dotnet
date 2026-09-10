# Roadmap — Proyecto Portafolio: Backend .NET con IA Aplicada

Este documento resume la vacante objetivo, los temas a dominar, los recursos oficiales de Microsoft para cada uno, y el plan de construcción del proyecto de portafolio.

## 1. Resumen de la vacante objetivo

**Cargo:** Desarrollador Back End .NET Senior, experto en IA
**Publicada por:** Recruiting Colombia SAS (agencia, en nombre de una empresa no revelada)
**Modalidad:** Remoto | Contrato indefinido | Salario hasta $7.650.000 COP

**Requisitos:**
- Profesional en Ingeniería de Sistemas o afines
- Mínimo 5 años de experiencia como Desarrollador Back End .NET
- Experiencia comprobable en proyectos de IA aplicada
- Dominio de APIs, arquitecturas escalables, buenas prácticas

**Conocimientos técnicos indispensables:**
1. Microsoft Extensions AI — integración agnóstica con OpenAI, Anthropic, Ollama u otros LLMs
2. Semantic Kernel — agentes de IA, prompt chaining, function calling, integración con servicios externos
3. Arquitectura RAG (Retrieval-Augmented Generation) — combinar LLMs con información empresarial, reducción de alucinaciones
4. Bases de datos vectoriales — embeddings, Pinecone/Milvus/Qdrant/pgvector en PostgreSQL
5. ML.NET — clasificación, regresión, detección de anomalías

## 2. Por qué este proyecto tiene sentido para tu perfil

Ya tienes una base fuerte en .NET/C#, Clean Architecture, SQL Server y PostgreSQL (de tu experiencia con sistemas geoespaciales/PostGIS). Ese conocimiento de PostgreSQL es una ventaja real: **pgvector es solo una extensión de Postgres**, así que no es infraestructura nueva, es una capa más sobre algo que ya dominas. El proyecto está diseñado para construir sobre esa base, no para empezar desde cero.

## 3. Recursos oficiales por tema

### Microsoft.Extensions.AI
- [Microsoft.Extensions.AI libraries - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [AI Samples for .NET - Code Samples | Microsoft Learn](https://learn.microsoft.com/en-us/samples/dotnet/ai-samples/ai-samples/)
- [Introducing Microsoft.Extensions.AI Preview - .NET Blog](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/)

### Semantic Kernel
- [Semantic Kernel documentation | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/)
- [How to quickly start with Semantic Kernel | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide)
- [Introduction to Semantic Kernel | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/overview/)
- [In-depth Semantic Kernel Demos | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/get-started/detailed-samples)
- [Understanding the kernel in Semantic Kernel | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/concepts/kernel)

### RAG (Retrieval-Augmented Generation)
- [Integrate Your Data into AI Apps with RAG - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/conceptual/rag)
- [Quickstart - Build a minimal .NET AI RAG app | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/how-to/build-vector-search-app)
- [Adding RAG to Semantic Kernel Agents | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-rag)

### Bases de datos vectoriales (pgvector)
- [Semantic Kernel Postgres connector | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/postgres-connector)
- [Vector search using Semantic Kernel Vector Store connectors | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/vector-search)
- [Semantic Kernel Vector Store code samples | Microsoft Learn](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/code-samples)

### ML.NET
- [ML.NET documentation | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/machine-learning/)
- [ML.NET tutorials | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/)
- [ML.NET Tutorial - Get started in 10 minutes | .NET](https://dotnet.microsoft.com/en-us/learn/ml-dotnet/get-started-tutorial/intro)
- [Overview of ML.NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/machine-learning/overview)

## 4. Estructura del proyecto

Un solo backend en .NET (Clean Architecture) que demuestra los 5 temas trabajando juntos: un asistente empresarial que responde preguntas sobre documentos propios (RAG), puede ejecutar acciones mediante function calling (Semantic Kernel), es agnóstico al proveedor de LLM (Microsoft.Extensions.AI), y expone un endpoint de clasificación con ML.NET.

```
AiPortfolio.sln
├── Directory.Build.props            → TargetFramework (net10.0) y opciones comunes a todos los proyectos
├── global.json                      → fija el SDK de .NET 10
├── src/
│   ├── AiPortfolio.Api/              → ASP.NET Core Web API (punto de entrada, endpoints REST)
│   ├── AiPortfolio.Application/      → Casos de uso, interfaces, DTOs
│   ├── AiPortfolio.Domain/           → Entidades y lógica de dominio pura
│   ├── AiPortfolio.Infrastructure.AI/    → Semantic Kernel, Microsoft.Extensions.AI, plugins/agentes
│   ├── AiPortfolio.Infrastructure.Rag/   → Embeddings, pgvector, búsqueda por similitud
│   └── AiPortfolio.Infrastructure.MLNet/ → Modelo de clasificación/anomalías con ML.NET
├── tests/
│   └── AiPortfolio.Tests/
├── docs/OVERVIEW.md                 → conceptos, beneficios y casos de uso empresariales
├── CONTRIBUTING.md                  → flujo de trabajo con Git
├── docker-compose.yml               → Postgres + pgvector para desarrollo local (host: puerto 55432)
└── init-pgvector.sql                → habilita la extensión pgvector al crear el contenedor
```

## 5. Plan de trabajo por fases

**Fase 1 — Fundamentos de Semantic Kernel + Microsoft.Extensions.AI**
Configurar el Kernel, conectar un proveedor de LLM (empezar con OpenAI o Ollama local para no depender de costos), crear un plugin simple con function calling.

**Fase 2 — RAG con pgvector**
Levantar Postgres con la extensión pgvector (Docker), generar embeddings de documentos de ejemplo, implementar búsqueda por similitud, conectar la respuesta del LLM a los documentos recuperados.

**Fase 3 — Agente con prompt chaining**
Extender el plugin de la Fase 1 a un agente que encadene pasos (analizar pregunta → buscar en RAG → decidir si necesita ejecutar una función externa → responder).

**Fase 4 — ML.NET**
Agregar un modelo simple de clasificación o detección de anomalías (por ejemplo, sobre datos ficticios de tickets de soporte), entrenarlo y exponerlo como endpoint.

**Fase 5 — Pulido**
Dockerizar todo el backend (ya tienes esa base de tu curso de contenerización), agregar logging/observabilidad con lo que aprendiste de OpenTelemetry, y escribir el README final como si fuera un proyecto real de portafolio.

## 6. Qué mostrar en la entrevista

Con este proyecto terminado puedes responder con hechos concretos a las preguntas de la vacante: "sí, implementé un flujo RAG con pgvector", "sí, usé Semantic Kernel para function calling", en vez de "he estudiado sobre esto". El repo en GitHub es la evidencia.
