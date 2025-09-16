export const environment = {
  aiApi: {
    uri: 'https://func-cdnkoyn6tkxdw.azurewebsites.net',
    getKeyCommand:
      'az login --tenant 8e8dee8b-d10c-4894-8d43-cae3f9ed549b && az functionapp function keys list --resource-group rg-clone1 --name func-cdnkoyn6tkxdw --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
