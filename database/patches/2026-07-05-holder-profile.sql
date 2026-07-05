CREATE TABLE IF NOT EXISTS holder_profiles (
  user_id uuid PRIMARY KEY REFERENCES users (id) ON DELETE CASCADE,
  full_name varchar(180),
  birth_date date,
  contact_email varchar(200),
  country_code varchar(2),
  phone_number varchar(40),
  created_at timestamp without time zone NOT NULL DEFAULT now(),
  updated_at timestamp without time zone NOT NULL DEFAULT now()
);

COMMENT ON TABLE holder_profiles IS 'Datos personales off-chain controlados por el titular/holder para UI y contacto.';
COMMENT ON COLUMN holder_profiles.full_name IS 'Nombre completo editable por el holder; no se ancla directamente en blockchain.';
COMMENT ON COLUMN holder_profiles.birth_date IS 'Fecha de nacimiento editable por el holder; dato personal off-chain.';
