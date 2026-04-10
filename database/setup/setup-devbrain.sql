-- DevBrain PostgreSQL Setup Script
-- Run as: psql -U postgres -h localhost -p 5432 -f setup-devbrain.sql

-- Step 1: Create user with password
CREATE USER devbrain WITH PASSWORD 'devbrain_secure_password';

-- Step 2: Create database owned by devbrain
CREATE DATABASE devbrain_local OWNER devbrain;

-- Step 3: Grant connection
GRANT CONNECT ON DATABASE devbrain_local TO devbrain;

-- Step 4: Switch to new database
\c devbrain_local

-- Step 5: Ensure schema exists
CREATE SCHEMA IF NOT EXISTS public;

-- Step 6: Grant schema permissions
GRANT USAGE ON SCHEMA public TO devbrain;
GRANT CREATE ON SCHEMA public TO devbrain;

-- Step 7: Set default privileges so devbrain can manage tables/sequences/functions
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO devbrain;

-- Step 8: Verify
SELECT 'Setup complete!' as status;
SELECT usename FROM pg_user WHERE usename = 'devbrain';
SELECT datname FROM pg_database WHERE datname = 'devbrain_local';
