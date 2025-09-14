export const environment = {
  aiApi: {
    uri: 'https://func-lxevwr7a4vipw.azurewebsites.net',
    getKeyCommand:
      'az login --tenant 8e8dee8b-d10c-4894-8d43-cae3f9ed549b && az functionapp function keys list --resource-group rg-skazd --name func-lxevwr7a4vipw --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
