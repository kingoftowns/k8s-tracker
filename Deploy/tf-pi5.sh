#!/bin/bash
# Helper script to run terraform commands for pi5 cluster

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Get the module name from current directory
MODULE=$(basename "$PWD")

# Get the terraform command (plan, apply, destroy, etc.)
COMMAND="${1:-plan}"

# Shift to get remaining arguments
shift

# Initialize with pi5 backend (reconfigure since we might be switching)
echo "Initializing terraform for pi5-${MODULE}..."
terraform init -reconfigure -backend-config="${SCRIPT_DIR}/backend.hcl" -backend-config="key=pi5-${MODULE}.tfstate"

# Run the terraform command with pi5 vars
echo "Running terraform ${COMMAND} for pi5-${MODULE}..."
terraform "${COMMAND}" -var-file="${SCRIPT_DIR}/pi5.tfvars" "$@"