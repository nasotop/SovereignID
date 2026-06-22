## ADDED Requirements

### Requirement: PostgreSQL service in Docker Compose

The development stack SHALL include a `postgres` service in `docker-compose.yml` using the official PostgreSQL 16 image.

The service MUST expose port 5432 to the host (configurable via environment variable).

The service MUST attach to the existing `sovereign-net` network.

The service MUST use a named Docker volume for data persistence across restarts.

#### Scenario: Postgres starts with compose stack

- **WHEN** developer runs `docker compose up -d`
- **THEN** the `postgres` container is running and healthy
- **AND** port 5432 (or configured `POSTGRES_PORT`) is reachable from the host

### Requirement: Schema initialization from canonical SQL script

On first startup with an empty data volume, PostgreSQL MUST execute `database/BBDD_SovereignID.sql` automatically via `/docker-entrypoint-initdb.d/`.

The script MUST NOT require a `CREATE DATABASE` statement; the database name MUST be configured via `POSTGRES_DB`.

After successful initialization, the database MUST contain all tables, ENUM types, indexes, foreign keys, and comments defined in the canonical script.

#### Scenario: Fresh volume applies schema

- **WHEN** postgres starts with an empty volume for the first time
- **THEN** all 12 tables from `BBDD_SovereignID.sql` exist in the `sovereignid` database
- **AND** ENUM types `user_role`, `credential_status`, `verification_result`, and `wallet_status` exist

#### Scenario: Existing volume skips re-initialization

- **WHEN** postgres restarts with a non-empty volume
- **THEN** init scripts are NOT re-executed
- **AND** existing schema and data remain intact

### Requirement: Database reset for development

The project MUST provide a documented command (`make db-reset`) that destroys the postgres volume and recreates the database from the canonical script.

#### Scenario: Developer resets schema after script change

- **WHEN** developer modifies `BBDD_SovereignID.sql`
- **WHEN** developer runs `make db-reset`
- **THEN** the postgres volume is removed
- **AND** on next startup the updated script is applied to a fresh database

### Requirement: Connection configuration via environment variables

The project MUST document the following environment variables in `.env.example`:

- `POSTGRES_DB` (default: `sovereignid`)
- `POSTGRES_USER` (default: `sovereignid`)
- `POSTGRES_PASSWORD` (development default, not for production)
- `POSTGRES_PORT` (default: `5432`)

A connection string template MUST be documented for future microservice integration.

#### Scenario: Developer configures local connection

- **WHEN** developer copies `.env.example` to `.env`
- **THEN** they can connect to postgres using documented host, port, database, user, and password values

### Requirement: SQL script as schema source of truth

The file `database/BBDD_SovereignID.sql` MUST remain the authoritative definition of the database schema.

Application code MUST NOT define or migrate schema in this change.

Future .NET persistence layers MUST use database-first scaffolding (`dotnet ef dbcontext scaffold`) against this schema, not code-first migrations.

#### Scenario: Schema exploration without application coupling

- **WHEN** developer connects to postgres with `psql` or a GUI client
- **THEN** they can inspect tables, constraints, and comments without any .NET service running

### Requirement: No application database connection in this phase

The `auth-api` and `web` services MUST NOT connect to postgres in this change.

The `auth-api` MUST continue using `InMemoryChallengeStore` for auth challenges.

#### Scenario: Auth works independently of postgres

- **WHEN** only `auth-api` and `web` are running (without postgres)
- **THEN** `GET /auth/nonce` and `POST /auth/verify` continue to function as before

#### Scenario: Postgres runs without auth-api dependency

- **WHEN** only the `postgres` service is started
- **THEN** the database is accessible and the schema is initialized
- **AND** no auth-api container is required

### Requirement: No seed data

The database initialization MUST NOT insert seed data (institutions, users, credential types, or other reference data).

#### Scenario: Empty tables after init

- **WHEN** schema initialization completes on a fresh volume
- **THEN** all tables exist but contain zero rows

### Requirement: Documentation in CONTEXT.md

`CONTEXT.md` MUST document the `database/` directory, the postgres dev stack, the reset workflow, and the decision that SQL is the schema source of truth with future database-first EF.

#### Scenario: New developer finds database setup

- **WHEN** a developer reads `CONTEXT.md`
- **THEN** they find instructions to start postgres, reset the database, and connect for schema exploration
