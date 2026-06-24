```mermaid
erDiagram
  "public.credential_types" {
    id integer PK
    code character varying(40) 
    name character varying(120) 
    description text(NULL) 
    allows_expiration boolean 
    jsonld_context_url character varying(500) 
    schema_version character varying(20) 
    is_active boolean 
    created_at timestamp without time zone 
  }
  "public.institutions" {
    id uuid PK
    code character varying(40) 
    legal_name character varying(200) 
    display_name character varying(120) 
    did character varying(200) 
    issuer_wallet_address character varying(42) 
    public_key text 
    country_code character(2) 
    website_url character varying(300)(NULL) 
    is_active boolean 
    registered_at timestamp without time zone 
    deactivated_at timestamp without time zone(NULL) 
  }
  "public.users" {
    id uuid PK
    wallet_address character varying(42) 
    did character varying(200) 
    email character varying(200)(NULL) 
    display_name character varying(120)(NULL) 
    is_active boolean 
    created_at timestamp without time zone 
    last_login_at timestamp without time zone(NULL) 
  }
  "public.institution_users" {
    id uuid PK
    institution_id uuid FK
    user_id uuid FK
    granted_by_user_id uuid(NULL) FK
    granted_at timestamp without time zone 
    revoked_at timestamp without time zone(NULL) 
  }
  "public.institution_users" }o--|| "public.institutions" : institution_users_institution_id_fkey
  "public.institution_users" }o--|| "public.users" : institution_users_user_id_fkey
  "public.institution_users" }o--|| "public.users" : institution_users_granted_by_user_id_fkey
  "public.students" {
    id uuid PK
    institution_id uuid FK
    external_reference character varying(80)(NULL) 
    enrollment_year integer(NULL) 
    is_active boolean 
    created_at timestamp without time zone 
  }
  "public.students" }o--|| "public.institutions" : students_institution_id_fkey
  "public.student_wallets" {
    id uuid PK
    student_id uuid FK
    wallet_address character varying(42) 
    did character varying(200) 
    is_primary boolean 
    activated_at timestamp without time zone 
    rotated_at timestamp without time zone(NULL) 
    rotation_reason character varying(80)(NULL) 
  }
  "public.student_wallets" }o--|| "public.students" : student_wallets_student_id_fkey
  "public.careers" {
    id uuid PK
    institution_id uuid FK
    code character varying(40) 
    name character varying(200) 
    is_active boolean 
    created_at timestamp without time zone 
  }
  "public.careers" }o--|| "public.institutions" : careers_institution_id_fkey
  "public.credentials" {
    id uuid PK
    institution_id uuid FK
    credential_type_id integer FK
    student_id uuid FK
    career_id uuid(NULL) FK
    issued_to_wallet_id uuid FK
    subject_did character varying(200) 
    issuer_did character varying(200) 
    ipfs_cid character varying(80) 
    ipfs_gateway_url character varying(500) 
    content_hash character(66) 
    transaction_hash character(66) 
    block_number bigint 
    chain_id integer 
    eip712_signature character varying(132) 
    issued_at timestamp without time zone 
    expires_at timestamp without time zone(NULL) 
    revoked_at timestamp without time zone(NULL) 
    revoked_by_user_id uuid(NULL) FK
    revocation_reason text(NULL) 
    revocation_tx_hash character(66)(NULL) 
    created_at timestamp without time zone 
    metadata jsonb(NULL) 
  }
  "public.credentials" }o--|| "public.institutions" : credentials_institution_id_fkey
  "public.credentials" }o--|| "public.credential_types" : credentials_credential_type_id_fkey
  "public.credentials" }o--|| "public.students" : credentials_student_id_fkey
  "public.credentials" }o--|| "public.careers" : credentials_career_id_fkey
  "public.credentials" }o--|| "public.student_wallets" : credentials_issued_to_wallet_id_fkey
  "public.credentials" }o--|| "public.users" : credentials_revoked_by_user_id_fkey
  "public.verification_logs" {
    id uuid PK
    credential_id uuid(NULL) FK
    credential_id_query character varying(80) 
    error_detail character varying(500)(NULL) 
    verifier_ip inet 
    verifier_user_agent character varying(500)(NULL) 
    verifier_country character(2)(NULL) 
    verifier_referer character varying(500)(NULL) 
    verified_at timestamp without time zone 
    duration_ms integer(NULL) 
    signature_valid boolean(NULL) 
    hash_matches boolean(NULL) 
    on_chain_exists boolean(NULL) 
    not_revoked boolean(NULL) 
    not_expired boolean(NULL) 
  }
  "public.verification_logs" }o--|| "public.credentials" : verification_logs_credential_id_fkey
  "public.audit_logs" {
    id bigint PK
    institution_id uuid(NULL) FK
    actor_user_id uuid(NULL) FK
    actor_wallet_address character varying(42)(NULL) 
    action character varying(80) 
    entity_type character varying(60) 
    entity_id character varying(80) 
    success boolean 
    error_message text(NULL) 
    ip_address inet(NULL) 
    user_agent character varying(500)(NULL) 
    request_id uuid(NULL) 
    payload jsonb(NULL) 
    created_at timestamp without time zone 
  }
  "public.audit_logs" }o--|| "public.institutions" : audit_logs_institution_id_fkey
  "public.audit_logs" }o--|| "public.users" : audit_logs_actor_user_id_fkey
  "public.institution_metrics_daily" {
    id bigint PK
    institution_id uuid FK
    metric_date date 
    credentials_issued integer 
    credentials_revoked integer 
    verifications_total integer 
    verifications_valid integer 
    verifications_invalid integer 
    unique_students_active integer 
    computed_at timestamp without time zone 
  }
  "public.institution_metrics_daily" }o--|| "public.institutions" : institution_metrics_daily_institution_id_fkey
  "public.auth_challenges" {
    id uuid PK
    nonce character varying(40) 
    wallet_address character varying(42)(NULL) 
    chain_id integer 
    issued_at timestamp without time zone 
    expires_at timestamp without time zone 
    consumed_at timestamp without time zone(NULL) 
  }
```
