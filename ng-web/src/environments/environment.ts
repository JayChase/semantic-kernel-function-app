export const environment = {
  aiApi: {
    uri: 'https://func-lxevwr7a4vipw.azurewebsites.net',
    getKeyCommand:
      'az functionapp function keys list --resource-group rg-skazd --name func-lxevwr7a4vipw --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
