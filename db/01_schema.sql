-- DocVault PostgreSQL Schema
-- Run as superuser, then create app user with limited permissions

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ===== REFERENCE TABLES =====

CREATE TABLE IF NOT EXISTS roles (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS departments (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(100) NOT NULL UNIQUE,
    code       VARCHAR(20)  NOT NULL UNIQUE,
    is_active  BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS financial_years (
    id        SERIAL PRIMARY KEY,
    label     VARCHAR(20) NOT NULL UNIQUE,  -- e.g. '2025-2026'
    fy_start  DATE NOT NULL,
    fy_end    DATE NOT NULL,
    is_locked BOOLEAN NOT NULL DEFAULT FALSE,
    locked_at TIMESTAMPTZ,
    locked_by VARCHAR(256)
);

CREATE TABLE IF NOT EXISTS document_types (
    id        SERIAL PRIMARY KEY,
    name      VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- ===== IDENTITY TABLES (ASP.NET Identity compatible) =====

CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id"               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name"             VARCHAR(256),
    "NormalizedName"   VARCHAR(256),
    "ConcurrencyStamp" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id"                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "FullName"             VARCHAR(200) NOT NULL DEFAULT '',
    "Department"           VARCHAR(100) NOT NULL DEFAULT '',
    "UserStatus"           VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    "CreatedAt"            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastLoginAt"          TIMESTAMPTZ,
    "RevokedAt"            TIMESTAMPTZ,
    "RevokedBy"            VARCHAR(256),
    "UserName"             VARCHAR(256),
    "NormalizedUserName"   VARCHAR(256),
    "Email"                VARCHAR(256),
    "NormalizedEmail"      VARCHAR(256),
    "EmailConfirmed"       BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash"         TEXT,
    "SecurityStamp"        TEXT,
    "ConcurrencyStamp"     TEXT,
    "PhoneNumber"          TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled"     BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd"           TIMESTAMPTZ,
    "LockoutEnabled"       BOOLEAN NOT NULL DEFAULT TRUE,
    "AccessFailedCount"    INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_users_normalized_email    ON "AspNetUsers"("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_normalized_username ON "AspNetUsers"("NormalizedUserName");

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "RoleId" UUID NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id"         SERIAL PRIMARY KEY,
    "UserId"     UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ClaimType"  TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider"       VARCHAR(128) NOT NULL,
    "ProviderKey"         VARCHAR(128) NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId"              UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId"        UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "LoginProvider" VARCHAR(128) NOT NULL,
    "Name"          VARCHAR(128) NOT NULL,
    "Value"         TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id"         SERIAL PRIMARY KEY,
    "RoleId"     UUID NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    "ClaimType"  TEXT,
    "ClaimValue" TEXT
);

-- ===== DOCUMENT TABLES =====

CREATE TABLE IF NOT EXISTS documents (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title             VARCHAR(300) NOT NULL,
    description       TEXT,
    original_filename VARCHAR(500) NOT NULL,
    stored_filename   VARCHAR(500) NOT NULL,
    file_path         TEXT NOT NULL,
    content_type      VARCHAR(200) NOT NULL,
    file_size_bytes   BIGINT NOT NULL,
    sha256_hash       VARCHAR(64) NOT NULL,
    status            VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    tags              TEXT[],
    is_deleted        BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at        TIMESTAMPTZ,
    uploaded_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    submitted_at      TIMESTAMPTZ,

    user_id           UUID NOT NULL REFERENCES "AspNetUsers"("Id"),
    department_id     INTEGER NOT NULL REFERENCES departments(id),
    financial_year_id INTEGER NOT NULL REFERENCES financial_years(id),
    document_type_id  INTEGER NOT NULL REFERENCES document_types(id)
);

CREATE INDEX IF NOT EXISTS idx_documents_user        ON documents(user_id);
CREATE INDEX IF NOT EXISTS idx_documents_dept_fy     ON documents(department_id, financial_year_id);
CREATE INDEX IF NOT EXISTS idx_documents_type        ON documents(document_type_id);
CREATE INDEX IF NOT EXISTS idx_documents_uploaded_at ON documents(uploaded_at DESC);
CREATE INDEX IF NOT EXISTS idx_documents_status      ON documents(status);
CREATE INDEX IF NOT EXISTS idx_documents_tags        ON documents USING GIN(tags);

CREATE TABLE IF NOT EXISTS document_versions (
    id              SERIAL PRIMARY KEY,
    document_id     UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    stored_filename VARCHAR(500) NOT NULL,
    file_path       TEXT NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    sha256_hash     VARCHAR(64) NOT NULL,
    version_number  INTEGER NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID NOT NULL REFERENCES "AspNetUsers"("Id")
);

-- ===== REQUIRED DOCUMENTS =====

CREATE TABLE IF NOT EXISTS required_documents (
    id                SERIAL PRIMARY KEY,
    department_id     INTEGER NOT NULL REFERENCES departments(id),
    document_type_id  INTEGER NOT NULL REFERENCES document_types(id),
    financial_year_id INTEGER NOT NULL REFERENCES financial_years(id),
    due_date          DATE,
    is_active         BOOLEAN NOT NULL DEFAULT TRUE,
    UNIQUE (department_id, document_type_id, financial_year_id)
);

-- ===== REFRESH TOKENS =====

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id                SERIAL PRIMARY KEY,
    user_id           UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    token_hash        VARCHAR(88) NOT NULL UNIQUE,
    expires_at        TIMESTAMPTZ NOT NULL,
    is_revoked        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at        TIMESTAMPTZ,
    replaced_by_token VARCHAR(88)
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON refresh_tokens(user_id);

-- ===== AUDIT / ACTIVITY LOGS (append-only) =====

CREATE TABLE IF NOT EXISTS activity_logs (
    id          BIGSERIAL PRIMARY KEY,
    user_id     UUID REFERENCES "AspNetUsers"("Id"),
    action      VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id   VARCHAR(200),
    details     TEXT,
    ip_address  VARCHAR(50),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_activity_logs_user       ON activity_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_activity_logs_created_at ON activity_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_activity_logs_action     ON activity_logs(action);

-- Prevent UPDATE and DELETE on activity_logs
CREATE OR REPLACE FUNCTION prevent_activity_log_mutation()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'activity_logs is append-only: updates and deletes are not permitted';
END;
$$;

DROP TRIGGER IF EXISTS trg_activity_logs_no_update ON activity_logs;
CREATE TRIGGER trg_activity_logs_no_update
BEFORE UPDATE ON activity_logs FOR EACH ROW EXECUTE FUNCTION prevent_activity_log_mutation();

DROP TRIGGER IF EXISTS trg_activity_logs_no_delete ON activity_logs;
CREATE TRIGGER trg_activity_logs_no_delete
BEFORE DELETE ON activity_logs FOR EACH ROW EXECUTE FUNCTION prevent_activity_log_mutation();

-- ===== NOTIFICATIONS =====

CREATE TABLE IF NOT EXISTS notifications (
    id         SERIAL PRIMARY KEY,
    user_id    UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    title      VARCHAR(300) NOT NULL,
    message    TEXT NOT NULL,
    is_read    BOOLEAN NOT NULL DEFAULT FALSE,
    type       VARCHAR(30) NOT NULL DEFAULT 'INFO',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    read_at    TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_notifications_user ON notifications(user_id, is_read);

-- ===== SEED DATA =====

INSERT INTO "AspNetRoles"("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
    (gen_random_uuid(), 'Admin', 'ADMIN', gen_random_uuid()::text),
    (gen_random_uuid(), 'User',  'USER',  gen_random_uuid()::text)
ON CONFLICT DO NOTHING;

INSERT INTO departments(name, code) VALUES
    ('Physics', 'PHY'), ('Chemistry', 'CHE'), ('Mathematics', 'MAT'),
    ('English', 'ENG'), ('Malayalam', 'MAL'), ('Computer Science', 'CS'),
    ('Biology', 'BIO'), ('History', 'HIS'), ('Economics', 'ECO'),
    ('Commerce', 'COM')
ON CONFLICT DO NOTHING;

INSERT INTO document_types(name) VALUES
    ('Syllabus'), ('LessonPlan'), ('AttendanceRegister'), ('MarksList'),
    ('MinutesOfMeeting'), ('EventReport'), ('ResearchPaper'),
    ('AuditReport'), ('CircularReceived'), ('Other')
ON CONFLICT DO NOTHING;

INSERT INTO financial_years(label, fy_start, fy_end) VALUES
    ('2020-2021', '2020-04-01', '2021-03-31'),
    ('2021-2022', '2021-04-01', '2022-03-31'),
    ('2022-2023', '2022-04-01', '2023-03-31'),
    ('2023-2024', '2023-04-01', '2024-03-31'),
    ('2024-2025', '2024-04-01', '2025-03-31'),
    ('2025-2026', '2025-04-01', '2026-03-31'),
    ('2026-2027', '2026-04-01', '2027-03-31'),
    ('2027-2028', '2027-04-01', '2028-03-31'),
    ('2028-2029', '2028-04-01', '2029-03-31'),
    ('2029-2030', '2029-04-01', '2030-03-31'),
    ('2030-2031', '2030-04-01', '2031-03-31')
ON CONFLICT DO NOTHING;
