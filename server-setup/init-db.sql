-- Initialize PDS database with proper settings

-- Set timezone to UTC
SET timezone = 'UTC';

-- Create extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE pds TO pds;

-- Log initialization
SELECT 'PDS database initialized successfully' AS status;