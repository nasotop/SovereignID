FROM postgres:16-alpine

# Bake canonical schema into the image so Portainer/git deploys do not depend
# on a host bind mount (Docker creates an empty directory if the file is missing).
COPY BBDD_SovereignID.sql /docker-entrypoint-initdb.d/01-schema.sql
