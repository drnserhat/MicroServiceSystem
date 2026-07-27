#!/bin/bash
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
  CREATE DATABASE identity;
  CREATE DATABASE "user";
  CREATE DATABASE coordinator;
  CREATE DATABASE location;
  CREATE DATABASE notification;
  CREATE DATABASE audit;
  CREATE DATABASE settings;
  CREATE DATABASE file;
EOSQL
