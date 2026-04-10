-- Create devbrain user with limited permissions
-- Cannot create or modify users, but can create/modify database structures

CREATE USER devbrain WITH PASSWORD 'devbrain_local_pass';

-- Create devbrain_local database
CREATE DATABASE devbrain_local OWNER postgres;

-- Grant connection permission
GRANT CONNECT ON DATABASE devbrain_local TO devbrain;

-- Set default privileges so devbrain can create tables, indexes, etc.
ALTER DATABASE devbrain_local OWNER TO devbrain;

-- Within the database, grant full schema permissions but NOT user management
\c devbrain_local

-- Create schema if needed
CREATE SCHEMA IF NOT EXISTS public;

-- Grant schema permissions (CREATE but not CREATE ROLE)
ALTER SCHEMA public OWNER TO devbrain;

-- Grant table/index/sequence creation
GRANT CREATE ON SCHEMA public TO devbrain;
GRANT USAGE ON SCHEMA public TO devbrain;

-- Allow devbrain to manage all objects in this database
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO devbrain;

-- Verify user was created
\du devbrain
