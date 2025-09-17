#!/bin/bash

set -e  # Exit on any error

# Function to display usage
usage() {
    echo "Usage: $0 <template_file> <output_file>"
    echo "  template_file: Path to the template file with placeholders (e.g., %KEY%)"
    echo "  output_file: Path where the processed file will be saved"
    exit 1
}

# Check if required arguments are provided
if [ $# -lt 2 ]; then
    usage
fi

TEMPLATE_FILE="$1"
OUTPUT_FILE="$2"

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

# Create a temporary file to store environment variables (all keys)
TEMP_ENV_FILE=$(mktemp)
# Keep only KEY=VALUE lines (ignore blanks/comments)
echo "$ENV_VARS" | grep -E '^[A-Za-z_][A-Za-z0-9_]*=' > "$TEMP_ENV_FILE" || true

# Read the template file
echo "Processing template file: $TEMPLATE_FILE"
CONTENT=$(cat "$TEMPLATE_FILE")

# Process each environment variable; replace only if %KEY% exists in the template
while IFS= read -r line; do
    # Skip empty lines just in case
    [ -z "$line" ] && continue

    # Split into key and value at the first '=' only
    key=${line%%=*}
    value=${line#*=}

    if [ -n "$key" ] && [ -n "$value" ]; then
        # Replace only if the placeholder exists
        if printf '%s' "$CONTENT" | grep -q "%${key}%"; then
            clean_value="$value"
            # Strip matching surrounding quotes symmetrically ("..." or '...')
            if [ "${clean_value#\"}" != "$clean_value" ] && [ "${clean_value%\"}" != "$clean_value" ]; then
                clean_value=${clean_value:1:${#clean_value}-2}
            elif [ "${clean_value#\'}" != "$clean_value" ] && [ "${clean_value%\'}" != "$clean_value" ]; then
                clean_value=${clean_value:1:${#clean_value}-2}
            else
                # If there's a stray trailing quote with no leading one, drop it
                if [ "${clean_value%\"}" != "$clean_value" ] && [ "${clean_value#\"}" = "$clean_value" ]; then
                    clean_value=${clean_value%\"}
                fi
                if [ "${clean_value%\'}" != "$clean_value" ] && [ "${clean_value#\'}" = "$clean_value" ]; then
                    clean_value=${clean_value%\'}
                fi
            fi

            # Escape sed replacement specials: /, &, |
            esc_value=$(printf '%s' "$clean_value" | sed -e 's/[\/&|]/\\&/g')

            echo "Replacing %${key}%"
            # Replace all occurrences of %KEY% with the escaped value
            CONTENT=$(printf '%s' "$CONTENT" | sed "s|%${key}%|${esc_value}|g")
        fi
    fi
done < "$TEMP_ENV_FILE"

# Write the processed content to the output file
echo "$CONTENT" > "$OUTPUT_FILE"

# Clean up temporary file
rm -f "$TEMP_ENV_FILE"

echo "Successfully processed template and saved to: $OUTPUT_FILE"
