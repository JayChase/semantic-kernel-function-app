#!/usr/bin/env bash

set -euo pipefail

# Resolve repo root relative to this script so it works from any cwd
SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(cd -- "$SCRIPT_DIR/.." && pwd)

echo "Getting Azure environment name..."
AZURE_ENV_NAME=$( azd env get-value AZURE_ENV_NAME 2>/dev/null || echo "" )

if [ -z "$AZURE_ENV_NAME" ]; then
  echo "Warning: No default Azure environment found. Make sure you're in an azd project and have initialized an environment."
  echo "You can set a default environment with: azd env set-default <env-name>"
  exit 1
fi

echo "Using Azure environment: $AZURE_ENV_NAME"

LOCAL_SETTINGS="$REPO_ROOT/sk-chat/SkChat/local.settings.json"
TMP_FILE="${LOCAL_SETTINGS}.tmp"

# Get environment variables from azd
echo "Loading environment variables from azd..."
AZURE_ENV_NAME=$( azd env get-value AZURE_ENV_NAME 2>/dev/null || echo "" )

if [ -z "$AZURE_ENV_NAME" ]; then
  echo "Warning: No default Azure environment found. Make sure you're in an azd project and have initialized an environment."
  echo "You can set a default environment with: azd env set-default <env-name>"
  exit 1
fi

echo "Using Azure environment: $AZURE_ENV_NAME"

ENV_VARS=$(azd env get-values --environment "$AZURE_ENV_NAME" 2>/dev/null || echo "")

if [ -z "$ENV_VARS" ]; then
  echo "Warning: No environment variables found for environment '$AZURE_ENV_NAME'"
  echo "Make sure the environment has been provisioned with: azd provision"
fi

# Ensure target directory exists
LOCAL_DIR=$(dirname "$LOCAL_SETTINGS")
mkdir -p "$LOCAL_DIR"

# Build Values JSON from env vars
VALUES_TMP=$(mktemp)
echo '  "Values": {' > "$VALUES_TMP"
first=1
while IFS='=' read -r key value; do
  [ -z "${key:-}" ] && continue
  key=$(echo "$key" | xargs)
  # Trim surrounding quotes if any
  value=$(echo "$value" | sed 's/^"//;s/"$//')
  [[ -z "$key" || "$key" =~ ^# ]] && continue
  if [ $first -eq 0 ]; then echo "," >> "$VALUES_TMP"; fi
  printf '    "%s": "%s"' "$key" "$value" >> "$VALUES_TMP"
  first=0
done <<< "$ENV_VARS"
echo -e "\n  }" >> "$VALUES_TMP"

# Generate full local.settings.json
cat > "$TMP_FILE" <<JSON
{
  "IsEncrypted": false,
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*"
  },
$(cat "$VALUES_TMP")
}
JSON

mv "$TMP_FILE" "$LOCAL_SETTINGS"
rm -f "$VALUES_TMP"
