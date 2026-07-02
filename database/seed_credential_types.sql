-- MVP credential type catalog (idempotent)
INSERT INTO credential_types (code, name, description, allows_expiration, jsonld_context_url, schema_version, is_active)
VALUES
  (
    'TITULO',
    'Título universitario',
    'Título de grado o postgrado',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'NOTAS',
    'Certificado de notas',
    'Certificado de calificaciones',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'CERTIFICACION',
    'Certificación',
    'Certificación de competencias',
    true,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  ),
  (
    'DIPLOMA',
    'Diploma',
    'Diploma académico',
    false,
    'https://www.w3.org/2018/credentials/v1',
    '1.0',
    true
  )
ON CONFLICT (code) DO NOTHING;
