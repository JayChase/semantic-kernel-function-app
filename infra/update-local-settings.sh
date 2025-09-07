#!/usr/bin/env bash

echo "Getting Azure environment name..."
AZURE_ENV_NAME=$( azd env get-value AZURE_ENV_NAME 2>/dev/null || echo "")

if [ -z "$AZURE_ENV_NAME" ]; then
    echo "Warning: No default Azure environment found. Make sure you're in an azd project and have initialized an environment."
    echo "You can set a default environment with: azd env set-default <env-name>"
    exit 1
fi

echo "Using Azure environment: $AZURE_ENV_NAME"


LOCAL_SETTINGS="sk-chat/SkChat/local.settings.json"
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

# Add all azd env variables to a temp file as JSON lines
echo '  "Values": {' > values.tmp
first=1
echo "$ENV_VARS" | while IFS='=' read -r key value; do
  key=$(echo "$key" | xargs)
  value=$(echo "$value" | sed 's/^"//;s/"$//')
  [[ -z "$key" || "$key" =~ ^# ]] && continue
  if [ $first -eq 0 ]; then echo "," >> values.tmp; fi
  echo -n "    \"$key\": \"$value\"" >> values.tmp
  first=0
done
echo -e "\n  }" >> values.tmp

# Extract the Values object from local.settings.json and merge
awk '
  BEGIN {in_values=0}
  /"Values": *{/ {in_values=1; system("cat values.tmp"); next}
  in_values && /}/ {in_values=0; next}
  !in_values {print}
' "$LOCAL_SETTINGS" > "$TMP_FILE"

mv "$TMP_FILE" "$LOCAL_SETTINGS"
rm values.tmp
