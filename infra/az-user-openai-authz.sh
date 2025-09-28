#!/usr/bin/env bash

USER_OBJECT_ID=$(az ad signed-in-user show --query id --output tsv)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP 2>/dev/null || echo "")
COG_SERVICE_NAME=$(azd env get-value AZURE_OPENAI_API__INSTANCE 2>/dev/null || echo "")

# Assign the Cognitive Services User role
az role assignment create \
  --assignee-object-id "$USER_OBJECT_ID" \
  --assignee-principal-type User \
  --role "Cognitive Services User" \
  --scope "/subscriptions/$(az account show --query id --output tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$COG_SERVICE_NAME"
