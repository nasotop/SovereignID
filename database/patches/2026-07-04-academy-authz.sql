-- Patch incremental para activar autorizacion global e institucional del MVP.
-- Aplicar sobre una BD existente antes de levantar los servicios actualizados.

DO $$
BEGIN
    CREATE TYPE global_user_role AS ENUM ('platform_admin');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

ALTER TYPE user_role ADD VALUE IF NOT EXISTS 'viewer';

CREATE TABLE IF NOT EXISTS user_global_roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    role global_user_role NOT NULL,
    granted_at timestamp NOT NULL DEFAULT now(),
    revoked_at timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_user_global_role
    ON user_global_roles (user_id, role);

CREATE INDEX IF NOT EXISTS user_global_roles_user_id_idx
    ON user_global_roles (user_id);

CREATE INDEX IF NOT EXISTS user_global_roles_role_idx
    ON user_global_roles (role);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'user_global_roles_user_id_fkey'
    ) THEN
        ALTER TABLE user_global_roles
            ADD CONSTRAINT user_global_roles_user_id_fkey
            FOREIGN KEY (user_id)
            REFERENCES users (id)
            DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

COMMENT ON TABLE user_global_roles IS 'Roles globales de plataforma. Para MVP se usa platform_admin seeded por wallet.';
COMMENT ON COLUMN user_global_roles.role IS 'Rol global, ej: platform_admin';
COMMENT ON COLUMN user_global_roles.revoked_at IS 'Si tiene valor, el rol global esta inactivo';
