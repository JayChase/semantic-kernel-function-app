export const environment = {
  aiApi: {
    uri: 'https://func-og6rnvu26w7ra.azurewebsites.net',
    getKeyCommand:
      'az functionapp function keys list --resource-group rg-skaf --name func-og6rnvu26w7ra --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
