targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
// Flex Consumption functions are only supported in these regions.
// Run `az functionapp list-flexconsumption-locations --output table` to get the latest list
@allowed([
  'northeurope'
  'southeastasia'
  'eastasia'
  'japaneast'
  'southcentralus'
  'australiaeast'
  'eastus'
  'westus2'
  'uksouth'
  'eastus2euap'
  'westus3'
  'swedencentral'
])
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

param resourceGroupName string = ''

@description('Location for the OpenAI resource group')
@allowed([
  'australiaeast'
  'canadaeast'
  'eastus'
  'eastus2'
  'francecentral'
  'japaneast'
  'northcentralus'
  'swedencentral'
  'switzerlandnorth'
  'uksouth'
  'westeurope'
])
@metadata({
  azd: {
    type: 'location'
  }
})
param openAiLocation string // Set in main.parameters.json
param openAiApiVersion string // Set in main.parameters.json

param chatModelName string // Set in main.parameters.json
param chatDeploymentName string = chatModelName
param chatModelVersion string // Set in main.parameters.json
param chatDeploymentCapacity int = 15

// ---------------------------------------------------------------------------
// Common variables

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }
var openAiUrl = 'https://${openAi.outputs.name}.openai.azure.com'
var functionAppName = '${abbrs.webSitesFunctions}${resourceToken}'
var storageAccountName = '${abbrs.storageStorageAccounts}${resourceToken}'

// ---------------------------------------------------------------------------
// Resources

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// The application webapp
module webApp 'br/public:avm/res/web/site:0.15.1' = {
  name: 'webApp'
  scope: resourceGroup
  params: {
    // Required parameters
    tags: union(tags, { 'azd-service-name': 'ngWeb' })
    kind: 'app,linux'
    name: '${abbrs.webSitesAppService}${resourceToken}'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'NODE|22-lts'
      appCommandLine: 'node /home/site/wwwroot/server/server.mjs'
    }
  }
}

// function app id

module faUserAssignedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.1' = {
  name: 'processorUserAssignedIdentity'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    name: '${abbrs.managedIdentityUserAssignedIdentities}${functionAppName}'
  }
}

// The application backend API
module functionApp 'br/public:avm/res/web/site:0.13.0' = {
  name: 'api'
  scope: resourceGroup
  params: {
    tags: union(tags, { 'azd-service-name': 'skChat' })
    location: location
    kind: 'functionapp,linux'
    name: functionAppName
    serverFarmResourceId: fcAppServicePlan.outputs.resourceId
    appInsightResourceId: monitoring.outputs.applicationInsightsResourceId
    managedIdentities: {
      systemAssigned: false
      userAssignedResourceIds: [
        faUserAssignedIdentity.outputs.resourceId
      ]
    }
    appSettingsKeyValuePairs: {
      AZURE_OPENAI_API__INSTANCE_NAME: openAi.outputs.name
      AZURE_OPENAI_API__ENDPOINT: openAiUrl
      AZURE_OPENAI_API__VERSION: openAiApiVersion
      AZURE_OPENAI_API__DEPLOYMENT_NAME: chatDeploymentName
      AZURE_CLIENT_ID: faUserAssignedIdentity.outputs.clientId // see https://learn.microsoft.com/en-us/answers/questions/1225865/unable-to-get-a-user-assigned-managed-identity-wor
      APPLICATIONINSIGHTS_CONNECTION_STRING: monitoring.outputs.applicationInsightsConnectionString
      AzureWebJobsStorage__clientId: faUserAssignedIdentity.outputs.clientId
    }
    siteConfig: {
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
          'https://ms.portal.azure.com'
          'https://${webApp.outputs.defaultHostname}'
        ]
      }
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.outputs.primaryBlobEndpoint}${functionAppName}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: faUserAssignedIdentity.outputs.resourceId
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '9.0'
      }
    }
    storageAccountResourceId: storageAccount.outputs.resourceId
    storageAccountUseIdentityAuthentication: true
    virtualNetworkSubnetId: vnet.outputs.appSubnetID
  }
}

// Compute plan for the Azure Functions API
module appServicePlan 'br/public:avm/res/web/serverfarm:0.4.1' = {
  name: 'appserviceplan'
  scope: resourceGroup
  params: {
    name: '${abbrs.webServerFarms}${resourceToken}'
    tags: tags
    location: location
    skuName: 'B1'
    reserved: true
  }
}

// Compute plan for the Azure Functions API
module fcAppServicePlan 'br/public:avm/res/web/serverfarm:0.4.1' = {
  name: 'fcAppserviceplan'
  scope: resourceGroup
  params: {
    name: '${abbrs.webServerFarms}fc-${resourceToken}'
    tags: tags
    location: location
    skuName: 'FC1'
    reserved: true
  }
}

// Virtual network for Azure Functions API

module vnet './vnet.bicep' = {
  name: 'vnet'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    name: '${abbrs.networkVirtualNetworks}${resourceToken}'
  }
}

// Storage for Azure Functions API and Blob storage
module storageAccount 'br/public:avm/res/storage/storage-account:0.15.0' = {
  name: 'storageAccount'
  scope: resourceGroup
  params: {
    name: storageAccountName
    tags: tags
    location: location
    skuName: 'Standard_LRS'
    allowSharedKeyAccess: false
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      virtualNetworkRules: [
        {
          id: vnet.outputs.appSubnetID
          action: 'Allow'
        }
      ]
    }
    blobServices: {
      containers: [
        {
          name: functionAppName
        }
      ]
    }
    roleAssignments: [
      {
        principalId: faUserAssignedIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
    ]
  }
}

module storagePrivateEndpoints './storage-private-endpoints.bicep' = {
  name: 'servicePrivateEndpoint'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    virtualNetworkName: vnet.outputs.name
    subnetName: vnet.outputs.peSubnetName
    resourceName: storageAccount.outputs.name
  }
}

// Monitor application with Azure Monitor
module monitoring 'br/public:avm/ptn/azd/monitoring:0.1.1' = {
  name: 'monitoring'
  scope: resourceGroup
  params: {
    tags: tags
    location: location
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: '${abbrs.portalDashboards}${resourceToken}'
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
  }
}

module openAi 'br/public:avm/res/cognitive-services/account:0.9.2' = {
  name: 'openai'
  scope: resourceGroup
  params: {
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: openAiLocation
    tags: tags
    kind: 'OpenAI'
    customSubDomainName: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    publicNetworkAccess: 'Disabled'
    sku: 'S0'
    deployments: [
      {
        name: chatDeploymentName
        model: {
          format: 'OpenAI'
          name: chatModelName
          version: chatModelVersion
        }
        sku: {
          name: 'GlobalStandard'
          capacity: chatDeploymentCapacity
        }
      }
    ]
    disableLocalAuth: true
    roleAssignments: [
      {
        principalId: faUserAssignedIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'Cognitive Services OpenAI User'
      }
    ]
  }
}

module openaiPrivateEndpoints './openai-private-endpoints.bicep' = {
  name: 'openAiPrivateEndpoints'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    virtualNetworkName: vnet.outputs.name
    subnetName: vnet.outputs.peSubnetName
    resourceName: openAi.outputs.name
  }
}

// ---------------------------------------------------------------------------
// System roles assignation

module openAiRoleApi 'br/public:avm/ptn/authorization/resource-role-assignment:0.1.2' = {
  scope: resourceGroup
  name: 'openai-role-api'
  params: {
    principalId: faUserAssignedIdentity.outputs.principalId
    roleName: 'Cognitive Services User'
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    resourceId: openAi.outputs.resourceId
  }
}

module storageRoleApi 'br/public:avm/ptn/authorization/resource-role-assignment:0.1.2' = {
  scope: resourceGroup
  name: 'storage-role-api'
  params: {
    principalId: faUserAssignedIdentity.outputs.principalId
    roleName: 'Storage Blob Data Contributor'
    roleDefinitionId: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
    resourceId: storageAccount.outputs.resourceId
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = resourceGroup.name

output AZURE_OPENAI_API__ENDPOINT string = openAiUrl
output AZURE_OPENAI_API__INSTANCE_ string = openAi.outputs.name
output AZURE_OPENAI_API__DEPLOYMENT_NAME string = chatDeploymentName
output OPENAI_API_VERSION string = openAiApiVersion
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output NG_API_URL string = 'https://${functionApp.outputs.defaultHostname}'
output NG_FUNCTION_APP_NAME string = functionApp.outputs.name
output NG_RG_NAME string = resourceGroup.name

output WEBAPP_URL string = webApp.outputs.defaultHostname
