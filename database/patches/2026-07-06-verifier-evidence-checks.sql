-- Verifier evidence checks: audit columns + integrity_failed verdict value.

ALTER TYPE verification_result ADD VALUE IF NOT EXISTS 'integrity_failed';

ALTER TABLE verification_logs
  ADD COLUMN IF NOT EXISTS signature_validation_source varchar(32),
  ADD COLUMN IF NOT EXISTS revocation_source varchar(16);

COMMENT ON COLUMN verification_logs.signature_validation_source IS
  'Fuente del veredicto de signature_valid: on_chain, bd_fallback_inconclusive, bd_fallback_rejected, not_evaluated';

COMMENT ON COLUMN verification_logs.revocation_source IS
  'Fuente(s) que reportaron revocación: bd, on_chain, both';
