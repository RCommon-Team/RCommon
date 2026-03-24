#!/bin/bash

# Set defaults
DEFAULT_P="my-project"
DEFAULT_CONNECTOR_NAME="my_pg"

# Parse dem arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --P=*) P="${1#*=}" ;; 
    --P) P="$2"; shift ;;
    --CONNECTOR_NAME=*) CONNECTOR_NAME="${1#*=}" ;;
    --CONNECTOR_NAME) CONNECTOR_NAME="$2"; shift ;;
    --) shift; break ;;
    *) echo "Unknown argument: $1" ;;
  esac
  shift
done
CONNECTOR_NAME="${2:-$DEFAULT_CONNECTOR_NAME}"

DIRECTORY="$HOME/Desktop/testing/$P"

# Scaffold a new project
echo "Creating a new Hasura DDN project: $P"
ddn supergraph init "$DIRECTORY" && cd "$DIRECTORY"

# Initialize the PostgreSQL connector — we'll still have to manually hit RETURN twice... 🤷
echo "Initializing PostgreSQL connector: $CONNECTOR_NAME"
ddn connector init "$CONNECTOR_NAME" -i

# Start the PostgreSQL container and Adminer
echo "Starting PostgreSQL container and Adminer..."
docker compose -f "app/connector/$CONNECTOR_NAME/compose.postgres-adminer.yaml" up -d

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to initialize..."
sleep 7

# Create tables and insert data
echo "Creating 'users' and 'posts' tables and inserting data..."
docker exec -i "${CONNECTOR_NAME}-postgres-1" psql -U user -d dev <<EOF
CREATE TABLE users (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL,
  age INT NOT NULL
);

CREATE TABLE posts (
  id SERIAL PRIMARY KEY,
  user_id INT REFERENCES users(id) ON DELETE CASCADE,
  title TEXT NOT NULL,
  content TEXT NOT NULL
);

INSERT INTO users (name, age) VALUES
  ('Alice', 25),                                                                                      
  ('Bob', 30),
  ('Charlie', 35);

INSERT INTO posts (user_id, title, content) VALUES
  (1, 'My First Post', 'This is Alice''s first post.'),
  (1, 'Another Post', 'Alice writes again!'),
  (2, 'Bob''s Post', 'Bob shares his thoughts.'),
  (3, 'Hello World', 'Charlie joins the conversation.');
EOF

# Introspect the PostgreSQL database
echo "Introspecting PostgreSQL database..."
ddn connector introspect "$CONNECTOR_NAME"

# Link the connector and add resources
echo "Creating the ConnectorLink object and adding resources..."
ddn connector-link add-resources "$CONNECTOR_NAME"

# Build the supergraph
echo "Building the supergraph..."
ddn supergraph build local

# Add the `detached` flag to our docker command...becuase logs are a pain for This
yq -i '.definition.scripts.docker-start.bash += " -d"' .hasura/context.yaml

# Start the Hasura DDN Engine and PostgreSQL connector
echo "Starting Hasura DDN Engine and PostgreSQL connector..."
ddn run docker-start

# Open the Hasura console
echo "Opening the Hasura console..."
ddn console --local

