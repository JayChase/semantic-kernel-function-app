export const environment = {
  aiApi: {
    uri: 'https://func-movdd4adkhlj6.azurewebsites.net',
    getKeyCommand:
      'az functionapp function keys list --resource-group %NG_RG_NAME% --name %NG_FUNCTION_APP_NAME% --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
