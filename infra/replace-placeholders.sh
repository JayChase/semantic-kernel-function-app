#!/bin/bash

set -e  # Exit on any error

# Function to display usage
usage() {
    echo "Usage: $0 <template_file> <output_file> [env_prefix]"
    echo "  template_file: Path to the template file with placeholders"
    echo "  output_file: Path where the processed file will be saved"
    echo "  env_prefix: Environment variable prefix to filter (default: NG_)"
    exit 1
}

# Check if required arguments are provided
if [ $# -lt 2 ]; then
    usage
fi

TEMPLATE_FILE="$1"
OUTPUT_FILE="$2"
ENV_PREFIX="${3:-NG_}"

# Check if template file exists
if [ ! -f "$TEMPLATE_FILE" ]; then
    echo "Error: Template file '$TEMPLATE_FILE' not found."
    exit 1
fi

# Get the default Azure environment name
echo "Getting Azure environment name..."
AZURE_ENV_NAME=$( azd env get-value AZURE_ENV_NAME 2>/dev/null || echo "")

if [ -z "$AZURE_ENV_NAME" ]; then
    echo "Warning: No default Azure environment found. Make sure you're in an azd project and have initialized an environment."
    echo "You can set a default environment with: azd env set-default <env-name>"
    exit 1
fi

echo "Using Azure environment: $AZURE_ENV_NAME"

# Get environment variables from azd
echo "Loading environment variables from azd..."
ENV_VARS=$(azd env get-values --environment "$AZURE_ENV_NAME" 2>/dev/null || echo "")

if [ -z "$ENV_VARS" ]; then
    echo "Warning: No environment variables found for environment '$AZURE_ENV_NAME'"
    echo "Make sure the environment has been provisioned with: azd provision"
fi

# Create a temporary file to store filtered environment variables
TEMP_ENV_FILE=$(mktemp)
echo "$ENV_VARS" | grep "^${ENV_PREFIX}" > "$TEMP_ENV_FILE" || true

# Read the template file
echo "Processing template file: $TEMPLATE_FILE"
CONTENT=$(cat "$TEMPLATE_FILE")

# Process each environment variable that starts with the prefix
while IFS='=' read -r key value; do
    if [ -n "$key" ] && [ -n "$value" ]; then
        # Remove quotes from value if present
        clean_value=$(echo "$value" | sed 's/^["'\'']\|["'\'']$//g')
        
        echo "Replacing %${key}% with: $clean_value"
        
        # Replace all occurrences of %KEY% with the value
        CONTENT=$(echo "$CONTENT" | sed "s|%${key}%|${clean_value}|g")
    fi
done < "$TEMP_ENV_FILE"

# Write the processed content to the output file
echo "$CONTENT" > "$OUTPUT_FILE"

# Clean up temporary file
rm -f "$TEMP_ENV_FILE"

echo "Successfully processed template and saved to: $OUTPUT_FILE"