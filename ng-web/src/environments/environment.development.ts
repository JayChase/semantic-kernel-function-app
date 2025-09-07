export const environment = {
  aiApi: {
    uri: 'http://localhost:7071',
    getKeyCommand:
      'az functionapp function keys list --resource-group %NG_RG_NAME% --name %NG_FUNCTION_APP_NAME% --function-name chat',
    endpoints: {
      chat: 'api/chat'
    }
  }
};
