export const environment = {
  aiApi: {
    uri: 'https://func-movdd4adkhlj6.azurewebsites.net',
    getKeyCommand:
      'az functionapp function keys list --resource-group rg-demodos --name func-movdd4adkhlj6 --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
