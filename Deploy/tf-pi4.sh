#!/bin/bash
# Helper script to run terraform commands for pi4 cluster

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Get the module name from current directory
MODULE=$(basename "$PWD")

# Get the terraform command (plan, apply, destroy, etc.)
COMMAND="${1:-plan}"

# Shift to get remaining arguments
shift

# Initialize with pi4 backend
echo "Initializing terraform for pi4-${MODULE}..."
terraform init -backend-config="${SCRIPT_DIR}/backend.hcl" -backend-config="key=pi4-${MODULE}.tfstate"

# Run the terraform command with pi4 vars
echo "Running terraform ${COMMAND} for pi4-${MODULE}..."
terraform "${COMMAND}" -var-file="${SCRIPT_DIR}/pi4.tfvars" "$@"