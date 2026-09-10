-- Se ejecuta automáticamente al crear el contenedor de Postgres (ver docker-compose.yml).
-- El conector de Semantic Kernel para Postgres necesita que la extensión pgvector
-- ya esté habilitada en la base de datos antes de crear las tablas/colecciones.
CREATE EXTENSION IF NOT EXISTS vector;
