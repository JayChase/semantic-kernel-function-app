@description('Specifies the name of the virtual network.')
param name string

@description('Specifies the location.')
param location string = resourceGroup().location

@description('Specifies the name of the subnet for the Service Bus private endpoint.')
param peSubnetName string = 'private-endpoints-subnet'

@description('Specifies the name of the subnet for Function App virtual network integration.')
param appSubnetName string = 'app'

param tags object = {}

module vnet 'br/public:avm/res/network/virtual-network:0.5.2' = {
  name: name
  params: {
    name: name
    location: location
    tags: tags
    addressPrefixes: ['10.0.0.0/16']
    subnets: [
      {
        name: appSubnetName
        addressPrefix: '10.0.2.0/24'
        delegation: 'Microsoft.App/environments'
        serviceEndpoints: ['Microsoft.Storage']
        privateEndpointNetworkPolicies: 'Disabled'
        privateLinkServiceNetworkPolicies: 'Enabled'
      }
      {
        name: peSubnetName
        addressPrefix: '10.0.1.0/24'
        privateEndpointNetworkPolicies: 'Disabled'
        privateLinkServiceNetworkPolicies: 'Enabled'
      }
    ]
  }
}

output name string = vnet.name
output peSubnetName string = peSubnetName
output peSubnetID string = '${vnet.outputs.resourceId}/subnets/${peSubnetName}'
output appSubnetName string = appSubnetName
output appSubnetID string = '${vnet.outputs.resourceId}/subnets/${appSubnetName}'
