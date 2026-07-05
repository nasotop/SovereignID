-- Seed del primer platform_admin del MVP.
-- Uso:
--   psql -U sovereignid -d sovereignid \
--     -v wallet_address='0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa' \
--     -v email='admin@sovereignid.local' \
--     -v display_name='Platform Admin' \
--     -f database/seed-platform-admin.sql

\set did 'did:ethr:sepolia:' :wallet_address

INSERT INTO users (wallet_address, did, email, display_name)
VALUES (lower(:'wallet_address'), lower(:'did'), :'email', :'display_name')
ON CONFLICT (wallet_address) DO UPDATE
SET did = EXCLUDED.did,
    email = COALESCE(users.email, EXCLUDED.email),
    display_name = COALESCE(users.display_name, EXCLUDED.display_name),
    is_active = true;

INSERT INTO user_global_roles (user_id, role)
SELECT id, 'platform_admin'::global_user_role
FROM users
WHERE wallet_address = lower(:'wallet_address')
ON CONFLICT (user_id, role) DO UPDATE
SET revoked_at = NULL;
