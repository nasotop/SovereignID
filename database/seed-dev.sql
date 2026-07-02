-- SovereignID — seed de desarrollo (idempotente, refresh canónico)
--
-- Titular demo: wallet 0xf6461f392288b5732a7703e8b83f64cab134eada
-- DID titular:  did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada
--
-- Credenciales demo (UUID fijos para /verifier):
--   aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa  TITULO         active
--   bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb  NOTAS          revoked
--   cccccccc-cccc-cccc-cccc-cccccccccccc  CERTIFICACION  expired
--
-- Re-ejecutar: .\scripts\seed-dev.ps1

BEGIN;

-- Catálogo MVP (4 tipos)
INSERT INTO credential_types (
  code,
  name,
  description,
  allows_expiration,
  jsonld_context_url,
  schema_version,
  is_active
)
VALUES
  (
    'TITULO',
    'Titulo Universitario',
    'Titulo universitario emitido por institucion acreditada',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'NOTAS',
    'Certificado de Notas',
    'Certificado de notas academicas',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'CERTIFICACION',
    'Certificacion Profesional',
    'Certificacion de competencias profesionales',
    true,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'DIPLOMA',
    'Diploma',
    'Diploma de estudios',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  )
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  allows_expiration = EXCLUDED.allows_expiration,
  jsonld_context_url = EXCLUDED.jsonld_context_url,
  schema_version = EXCLUDED.schema_version,
  is_active = EXCLUDED.is_active;

-- Institucion emisora demo (Duoc UC)
INSERT INTO institutions (
  id,
  code,
  legal_name,
  display_name,
  did,
  issuer_wallet_address,
  public_key,
  country_code,
  is_active
)
VALUES (
  '11111111-1111-1111-1111-111111111111',
  'DUOC',
  'Duoc UC SpA',
  'Duoc UC',
  'did:ethr:sepolia:0x1111111111111111111111111111111111111111',
  '0x1111111111111111111111111111111111111111',
  '0xissuer-public-key-dev',
  'CL',
  true
)
ON CONFLICT (id) DO UPDATE SET
  code = EXCLUDED.code,
  legal_name = EXCLUDED.legal_name,
  display_name = EXCLUDED.display_name,
  did = EXCLUDED.did,
  issuer_wallet_address = EXCLUDED.issuer_wallet_address,
  public_key = EXCLUDED.public_key,
  country_code = EXCLUDED.country_code,
  is_active = EXCLUDED.is_active,
  deactivated_at = NULL;

-- Carrera demo
INSERT INTO careers (
  id,
  institution_id,
  code,
  name,
  is_active
)
VALUES (
  '33333333-3333-3333-3333-333333333333',
  '11111111-1111-1111-1111-111111111111',
  'ING-SW',
  'Ingenieria en Software',
  true
)
ON CONFLICT (id) DO UPDATE SET
  institution_id = EXCLUDED.institution_id,
  code = EXCLUDED.code,
  name = EXCLUDED.name,
  is_active = EXCLUDED.is_active;

-- Estudiante titular demo
INSERT INTO students (
  id,
  institution_id,
  external_reference,
  enrollment_year,
  is_active
)
VALUES (
  '22222222-2222-2222-2222-222222222222',
  '11111111-1111-1111-1111-111111111111',
  'DEV-HOLDER-F646',
  2024,
  true
)
ON CONFLICT (id) DO UPDATE SET
  institution_id = EXCLUDED.institution_id,
  external_reference = EXCLUDED.external_reference,
  enrollment_year = EXCLUDED.enrollment_year,
  is_active = EXCLUDED.is_active;

-- Wallet primaria del titular (login SIWE + filtro holder)
INSERT INTO student_wallets (
  id,
  student_id,
  wallet_address,
  did,
  status,
  is_primary,
  activated_at,
  rotated_at,
  rotation_reason
)
VALUES (
  '44444444-4444-4444-4444-444444444444',
  '22222222-2222-2222-2222-222222222222',
  '0xf6461f392288b5732a7703e8b83f64cab134eada',
  'did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada',
  'active',
  true,
  TIMESTAMPTZ '2024-03-01 12:00:00+00',
  NULL,
  NULL
)
ON CONFLICT (id) DO UPDATE SET
  student_id = EXCLUDED.student_id,
  wallet_address = EXCLUDED.wallet_address,
  did = EXCLUDED.did,
  status = EXCLUDED.status,
  is_primary = EXCLUDED.is_primary,
  activated_at = EXCLUDED.activated_at,
  rotated_at = EXCLUDED.rotated_at,
  rotation_reason = EXCLUDED.rotation_reason;

-- Credencial 1: TITULO active
INSERT INTO credentials (
  id,
  institution_id,
  credential_type_id,
  student_id,
  career_id,
  issued_to_wallet_id,
  subject_did,
  issuer_did,
  ipfs_cid,
  ipfs_gateway_url,
  content_hash,
  transaction_hash,
  block_number,
  chain_id,
  eip712_signature,
  status,
  issued_at,
  expires_at,
  revoked_at,
  metadata
)
VALUES (
  'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  '11111111-1111-1111-1111-111111111111',
  (SELECT id FROM credential_types WHERE code = 'TITULO'),
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333',
  '44444444-4444-4444-4444-444444444444',
  'did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada',
  'did:ethr:sepolia:0x1111111111111111111111111111111111111111',
  'bafybeigdyrztdevtitulo000000000000000001',
  'https://ipfs.io/ipfs/bafybeigdyrztdevtitulo000000000000000001',
  '0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1',
  '0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb1',
  123456,
  11155111,
  '0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc',
  'active',
  TIMESTAMPTZ '2025-11-15 00:00:00+00',
  NULL,
  NULL,
  '{"seed": "dev", "type": "TITULO"}'::jsonb
)
ON CONFLICT (id) DO UPDATE SET
  institution_id = EXCLUDED.institution_id,
  credential_type_id = EXCLUDED.credential_type_id,
  student_id = EXCLUDED.student_id,
  career_id = EXCLUDED.career_id,
  issued_to_wallet_id = EXCLUDED.issued_to_wallet_id,
  subject_did = EXCLUDED.subject_did,
  issuer_did = EXCLUDED.issuer_did,
  ipfs_cid = EXCLUDED.ipfs_cid,
  ipfs_gateway_url = EXCLUDED.ipfs_gateway_url,
  content_hash = EXCLUDED.content_hash,
  transaction_hash = EXCLUDED.transaction_hash,
  block_number = EXCLUDED.block_number,
  chain_id = EXCLUDED.chain_id,
  eip712_signature = EXCLUDED.eip712_signature,
  status = EXCLUDED.status,
  issued_at = EXCLUDED.issued_at,
  expires_at = EXCLUDED.expires_at,
  revoked_at = EXCLUDED.revoked_at,
  revoked_by_user_id = NULL,
  revocation_reason = NULL,
  revocation_tx_hash = NULL,
  metadata = EXCLUDED.metadata;

-- Credencial 2: NOTAS revoked
INSERT INTO credentials (
  id,
  institution_id,
  credential_type_id,
  student_id,
  career_id,
  issued_to_wallet_id,
  subject_did,
  issuer_did,
  ipfs_cid,
  ipfs_gateway_url,
  content_hash,
  transaction_hash,
  block_number,
  chain_id,
  eip712_signature,
  status,
  issued_at,
  expires_at,
  revoked_at,
  metadata
)
VALUES (
  'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  '11111111-1111-1111-1111-111111111111',
  (SELECT id FROM credential_types WHERE code = 'NOTAS'),
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333',
  '44444444-4444-4444-4444-444444444444',
  'did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada',
  'did:ethr:sepolia:0x1111111111111111111111111111111111111111',
  'bafybeigdyrztdevnotas0000000000000000002',
  'https://ipfs.io/ipfs/bafybeigdyrztdevnotas0000000000000000002',
  '0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2',
  '0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb2',
  123457,
  11155111,
  '0xdddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd',
  'revoked',
  TIMESTAMPTZ '2025-10-22 00:00:00+00',
  NULL,
  TIMESTAMPTZ '2025-12-01 10:00:00+00',
  '{"seed": "dev", "type": "NOTAS"}'::jsonb
)
ON CONFLICT (id) DO UPDATE SET
  institution_id = EXCLUDED.institution_id,
  credential_type_id = EXCLUDED.credential_type_id,
  student_id = EXCLUDED.student_id,
  career_id = EXCLUDED.career_id,
  issued_to_wallet_id = EXCLUDED.issued_to_wallet_id,
  subject_did = EXCLUDED.subject_did,
  issuer_did = EXCLUDED.issuer_did,
  ipfs_cid = EXCLUDED.ipfs_cid,
  ipfs_gateway_url = EXCLUDED.ipfs_gateway_url,
  content_hash = EXCLUDED.content_hash,
  transaction_hash = EXCLUDED.transaction_hash,
  block_number = EXCLUDED.block_number,
  chain_id = EXCLUDED.chain_id,
  eip712_signature = EXCLUDED.eip712_signature,
  status = EXCLUDED.status,
  issued_at = EXCLUDED.issued_at,
  expires_at = EXCLUDED.expires_at,
  revoked_at = EXCLUDED.revoked_at,
  revoked_by_user_id = NULL,
  revocation_reason = 'Revocacion demo (seed dev)',
  revocation_tx_hash = NULL,
  metadata = EXCLUDED.metadata;

-- Credencial 3: CERTIFICACION expired
INSERT INTO credentials (
  id,
  institution_id,
  credential_type_id,
  student_id,
  career_id,
  issued_to_wallet_id,
  subject_did,
  issuer_did,
  ipfs_cid,
  ipfs_gateway_url,
  content_hash,
  transaction_hash,
  block_number,
  chain_id,
  eip712_signature,
  status,
  issued_at,
  expires_at,
  revoked_at,
  metadata
)
VALUES (
  'cccccccc-cccc-cccc-cccc-cccccccccccc',
  '11111111-1111-1111-1111-111111111111',
  (SELECT id FROM credential_types WHERE code = 'CERTIFICACION'),
  '22222222-2222-2222-2222-222222222222',
  NULL,
  '44444444-4444-4444-4444-444444444444',
  'did:ethr:sepolia:0xf6461f392288b5732a7703e8b83f64cab134eada',
  'did:ethr:sepolia:0x1111111111111111111111111111111111111111',
  'bafybeigdyrztdevcert00000000000000000003',
  'https://ipfs.io/ipfs/bafybeigdyrztdevcert00000000000000000003',
  '0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa3',
  '0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb3',
  123458,
  11155111,
  '0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee',
  'expired',
  TIMESTAMPTZ '2024-06-01 00:00:00+00',
  TIMESTAMPTZ '2024-12-31 23:59:59+00',
  NULL,
  '{"seed": "dev", "type": "CERTIFICACION"}'::jsonb
)
ON CONFLICT (id) DO UPDATE SET
  institution_id = EXCLUDED.institution_id,
  credential_type_id = EXCLUDED.credential_type_id,
  student_id = EXCLUDED.student_id,
  career_id = EXCLUDED.career_id,
  issued_to_wallet_id = EXCLUDED.issued_to_wallet_id,
  subject_did = EXCLUDED.subject_did,
  issuer_did = EXCLUDED.issuer_did,
  ipfs_cid = EXCLUDED.ipfs_cid,
  ipfs_gateway_url = EXCLUDED.ipfs_gateway_url,
  content_hash = EXCLUDED.content_hash,
  transaction_hash = EXCLUDED.transaction_hash,
  block_number = EXCLUDED.block_number,
  chain_id = EXCLUDED.chain_id,
  eip712_signature = EXCLUDED.eip712_signature,
  status = EXCLUDED.status,
  issued_at = EXCLUDED.issued_at,
  expires_at = EXCLUDED.expires_at,
  revoked_at = EXCLUDED.revoked_at,
  revoked_by_user_id = NULL,
  revocation_reason = NULL,
  revocation_tx_hash = NULL,
  metadata = EXCLUDED.metadata;

COMMIT;
