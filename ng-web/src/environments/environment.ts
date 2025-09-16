export const environment = {
  aiApi: {
    uri: 'https://func-token.azurewebsites.net',
    getKeyCommand:
      'az login --tenant tenant-id && az functionapp function keys list --resource-group rg-env-or-token --name func-token --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
