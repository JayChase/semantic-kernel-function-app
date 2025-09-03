param virtualNetworkName string
param subnetName string
@description('Specifies the OpenAI resource name')
param resourceName string
param location string = resourceGroup().location
param tags object = {}

resource vnet 'Microsoft.Network/virtualNetworks@2021-08-01' existing = {
  name: virtualNetworkName
}

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: resourceName
}

// DNS zone name for OpenAI private endpoint
var openAiPrivateDNSZoneName = 'privatelink.openai.azure.com'

// AVM module for OpenAI Private DNS Zone
module privateDnsZoneOpenAiDeployment 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'openai-private-dns-zone-deployment'
  params: {
    name: openAiPrivateDNSZoneName
    location: 'global'
    tags: tags
    virtualNetworkLinks: [
      {
        name: '${resourceName}-openai-link-${take(toLower(uniqueString(resourceName, virtualNetworkName)), 4)}'
        virtualNetworkResourceId: vnet.id
        registrationEnabled: false
        location: 'global'
        tags: tags
      }
    ]
  }
}

// AVM module for OpenAI Private Endpoint with private DNS zone
module openAiPrivateEndpoint 'br/public:avm/res/network/private-endpoint:0.11.0' = {
  name: 'openai-private-endpoint-deployment'
  params: {
    name: 'openai-private-endpoint'
    location: location
    tags: tags
    subnetResourceId: '${vnet.id}/subnets/${subnetName}'
    privateLinkServiceConnections: [
      {
        name: 'openAiPrivateLinkConnection'
        properties: {
          privateLinkServiceId: openAiAccount.id
          groupIds: [
            'account'
          ]
        }
      }
    ]
    customDnsConfigs: []
    // Creates private DNS zone and links
    privateDnsZoneGroup: {
      name: 'openAiPrivateDnsZoneGroup'
      privateDnsZoneGroupConfigs: [
        {
          name: 'openAiARecord'
          privateDnsZoneResourceId: privateDnsZoneOpenAiDeployment.outputs.resourceId
        }
      ]
    }
  }
}
