# Flujo de trabajo con Git

Guía de colaboración para este repositorio (`semantic-kernel-rag-dotnet`). Aplica
tanto a personas como a asistentes de IA que hagan cambios aquí.

## Ramas

- **`develop`** — rama de integración y base de trabajo por defecto. Todo cambio
  nuevo parte de aquí y se fusiona aquí primero.
- **`main`** — rama estable / presentable. Es la que alguien clona primero al
  mirar el repo (portafolio). No se le hace push directo; solo se actualiza
  publicando `develop` cuando lo que hay ahí ya está listo para enseñar
  (ver "Publicar a `main`" más abajo). No hay despliegue automático.
- **Ramas de trabajo** — una por cada cambio, con prefijo según el tipo:

  | Prefijo     | Uso                                                 |
  |-------------|-----------------------------------------------------|
  | `feature/`  | Funcionalidad nueva                                 |
  | `fix/`      | Corrección de bug                                   |
  | `refactor/` | Reordenar/limpiar código sin cambiar comportamiento |
  | `docs/`     | Documentación                                       |
  | `chore/`    | Tooling, configuración, dependencias, mantenimiento |
  | `test/`     | Añadir o arreglar pruebas                           |

  Nombre descriptivo en minúsculas y con guiones, ej.
  `feature/filtro-por-score-de-similitud`, `fix/puerto-kestrel-reservado`.

## Flujo paso a paso

1. Partir siempre de `develop` actualizado:
   ```bash
   git checkout develop
   git pull
   git checkout -b <prefijo>/<descripcion>
   ```
2. Hacer los cambios localmente. Antes de pasar al siguiente paso, revisar
   "Antes de dar por terminado un cambio" más abajo.
3. **ESPERAR APROBACIÓN** para hacer commit y push:
   - **La IA NO debe hacer `git commit` ni `git push` de forma automática.**
   - Debe esperar a que la persona pruebe el cambio en su entorno local y lo
     apruebe explícitamente (ej. "está listo, haz commit y push").
   - Esto evita llenar el historial de commits pequeños de prueba y corrección.
   - Con la aprobación dada:
     ```bash
     git add .
     git commit -m "tipo(alcance): descripcion"
     git push -u origin <prefijo>/<descripcion>
     ```
4. **El Pull Request lo abre y fusiona la persona**, no la IA. La IA no crea PRs
   ni hace merge por su cuenta.
5. Cuando ya esté fusionado, la persona lo indica (ej. "ya mergeé, haz pull"). En
   ese momento:
   ```bash
   git checkout develop
   git pull
   git branch -d <prefijo>/<descripcion>
   git fetch --prune   # limpia la referencia remota si el merge ya borró la rama
   ```

No dejar ramas locales de features ya fusionadas — se eliminan en el mismo paso
del `pull`.

## Publicar a `main` (`develop` → `main`)

`develop` es donde se integra y prueba todo. Publicar a `main` es un paso aparte
y explícito, no automático:

1. La persona abre un PR `develop` → `main` en GitHub cuando decide que lo que
   hay en `develop` ya está para enseñar. La IA no lo abre por su cuenta.
2. **Usar "Create a merge commit", no "Squash and merge"** — para conservar la
   traza de cada cambio.
3. La persona lo indica (ej. "ya mergeé a main") y en ese momento:
   ```bash
   git checkout main
   git pull
   git checkout develop
   ```

`develop` y `main` no tienen por qué coincidir siempre — se publica cuando se
decide, no en cada cambio.

## Antes de dar por terminado un cambio

No es "cambio terminado" hasta que cumple todo esto. Se verifica **antes** de
pedir aprobación para commit (paso 3 de arriba), no días después como auditoría
aparte:

- **`dotnet build` de la solución completa: 0 errores y 0 advertencias.**
  ```bash
  dotnet build AiPortfolio.sln
  ```
- **`dotnet test` en verde.**
  ```bash
  dotnet test AiPortfolio.sln
  ```
- **Verificado en vivo**, no solo "debería funcionar leyendo el código": correr
  la API y probar el flujo real por Swagger o `curl`. Si el cambio toca RAG o
  chat, con el contenedor de Postgres arriba (`docker compose up -d`) y Ollama
  con sus modelos (`nomic-embed-text`, `llama3.1`). El endpoint
  `/api/tickets/classify` no necesita infraestructura.
  ```bash
  dotnet run --project src/AiPortfolio.Api --launch-profile http
  ```
- **Clean Architecture respetada** (ver [`README.md`](README.md) y
  [`docs/OVERVIEW.md`](docs/OVERVIEW.md)):
  - `Domain` no referencia a nadie.
  - `Application` solo define interfaces, DTOs y contratos — sin SDKs ni
    Infrastructure.
  - `Infrastructure.*` implementa las interfaces de `Application`; el detalle de
    proveedor (OpenAI, Ollama, pgvector) no se filtra fuera de sus *factories*
    (`KernelFactory`, `EmbeddingGeneratorFactory`).
  - `Api` solo compone (DI + endpoints), no tiene lógica de negocio.
- **Configuración nueva**: exponerla en `src/AiPortfolio.Api/appsettings.json`
  (con su `_comentario`), enlazarla en `Program.cs`, y darle un valor por defecto
  sensato en las `*Options`.
- **Versiones de paquetes**: explícitas (sin comodines `*`) y coherentes con la
  línea de Semantic Kernel **1.74** que usa el repo. Si algo obliga a subir de
  versión, dejar el porqué en un comentario del `.csproj` (como ya está hecho).
- **Documentación al día, en el mismo cambio**:
  - `README.md` si cambia algo visible para quien lee el repo por fuera (cómo
    correrlo, puertos, endpoints, lista de lo que incluye).
  - `docs/OVERVIEW.md` si cambia *qué hace* el proyecto o se añade una pieza.
  - Comentario XML (`/// <summary>`) de las clases que toques, si el
    comportamiento descrito ya no es exacto.

## Mensajes de commit

Formato: `tipo(alcance opcional): descripción breve en español`, en minúsculas.

```
feat(rag): filtrar resultados por umbral de similitud de coseno
fix(api): mover Kestrel a un puerto fuera del rango reservado de Windows
docs(overview): agregar escenarios de aplicación por área
refactor(chat): rastrear fuentes reales en el plugin en vez de parsear el texto
chore(deps): fijar toda la línea de Semantic Kernel a 1.74
test(mlnet): cubrir la categoría "Mejora" del clasificador
```

Tipos: `feat`, `fix`, `docs`, `refactor`, `style`, `chore`, `test`.

- Un commit = un cambio coherente (no mezclar cosas distintas).
- Sin `--no-verify`, sin `--force` a `develop` / `main`.
