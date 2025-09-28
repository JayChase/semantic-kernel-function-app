export const environment = {
  aiApi: {
    uri: 'https://func-sdszrwqhllc2m.azurewebsites.net',
    getKeyCommand:
      'az login --tenant 8e8dee8b-d10c-4894-8d43-cae3f9ed549b && az functionapp function keys list --resource-group rg-issue12fnl --name func-sdszrwqhllc2m --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
