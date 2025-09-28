#!/usr/bin/env pwsh

# Get the current user's object ID
$USER_OBJECT_ID = az ad signed-in-user show --query id --output tsv

# Get environment variables from azd, with fallback to empty string
try {
    $RESOURCE_GROUP = azd env get-value AZURE_RESOURCE_GROUP 2>$null
    if ($LASTEXITCODE -ne 0) { $RESOURCE_GROUP = "" }
}
catch {
    $RESOURCE_GROUP = ""
}

try {
    $COG_SERVICE_NAME = azd env get-value AZURE_OPENAI_API__INSTANCE 2>$null
    if ($LASTEXITCODE -ne 0) { $COG_SERVICE_NAME = "" }
}
catch {
    $COG_SERVICE_NAME = ""
}

# Get current subscription ID
$SUBSCRIPTION_ID = az account show --query id --output tsv

# Assign the Cognitive Services User role
az role assignment create `
    --assignee-object-id "$USER_OBJECT_ID" `
    --assignee-principal-type User `
    --role "Cognitive Services User" `
    --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$COG_SERVICE_NAME"
