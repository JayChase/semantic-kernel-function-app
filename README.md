# Semantic Kernel Chat Function App + Angular (App Service) 🚀

End‑to‑end sample showing a streaming chat experience powered by Azure OpenAI (via Microsoft.SemanticKernel + Microsoft.Extensions.AI) exposed through a .NET 9 isolated Azure Function and consumed by an Angular Universal (SSR) front end hosted on Azure App Service. Provisioning, configuration, networking, identity and model deployment are fully automated with Azure Developer CLI (azd) + Bicep (AVM modules + custom modules for VNet & private endpoints).

> ⚡ **Fastest way to get started: Open in the Dev Container (or GitHub Codespaces) – everything (Node 22, .NET 9, Azure CLI, azd, Functions Core Tools, Angular CLI) is pre-installed.** Local setup is optional and described later.

## Dev Container / Codespaces

[![Open in Dev Container](https://img.shields.io/static/v1?style=for-the-badge&label=Dev%20Container&message=Open&color=2266ee&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/JayChase/semantic-kernel-function-app)

Or create a new Codespace (GitHub UI: Code ▶ Create codespace on main). Once the container builds, follow the [Dev Container Quick Start](#dev-container-quick-start).

## Table of Contents

-   [Architecture](#architecture)
-   [Features](#features)
-   [Prerequisites](#prerequisites)
-   [Dev Container Quick Start](#dev-container-quick-start)
-   [Quick Start (Local Alternative)](#quick-start-local-alternative)
-   [Deploy to Azure](#deploy-to-azure)
-   [Services & Resources](#services--resources)
-   [Configuration & Environment Variables](#configuration--environment-variables)
-   [Local Development](#local-development)
-   [Chat API Contract](#chat-api-contract)
-   [Streaming Implementation Details](#streaming-implementation-details)
-   [Security & Networking](#security--networking)
-   [Cost & Scaling](#cost--scaling)
-   [Troubleshooting](#troubleshooting)
-   [Next Steps](#next-steps)

## Architecture

![infra](./docs/ai-messaging-deployment.svg)

High‑level flow:

1. Browser loads Angular app (SSR entrypoint `server/server.mjs`) from App Service (Linux B1).
2. Angular front end retrieves the Function key (CLI helper) and calls the `chat` function endpoint (`/api/chat`).
3. Azure Function (Flex Consumption FC1) uses injected `IChatClient` (Azure OpenAI) via Semantic Kernel wiring in `Program.cs`.
4. Messages stream back over Server‑Sent Events (SSE) as incremental JSON lines of `ChatMessage` objects.
5. Private networking + Managed Identity secure access to Storage + Azure OpenAI (public network access disabled) through private endpoints and RBAC.

## Features

-   Modern .NET 9 isolated Azure Functions with middleware & DI.
-   Semantic Kernel + Microsoft.Extensions.AI abstraction for chat.
-   Streaming responses (SSE style) for low‑latency token display.
-   Angular 19 SSR (Node 22 LTS) hosted on App Service.
-   Automated infra: AVM Bicep modules (App Service, Function, Storage, VNet, Private Endpoints, Azure OpenAI, Monitoring, Managed Identity).
-   User Assigned Managed Identity for Function deployment & data plane auth.
-   Strict Storage access (shared key disabled + network ACL + VNet rules).
-   Private endpoints for Azure OpenAI & Storage.
-   Environment materialization scripts (`replace-placeholders.sh`, `update-local-settings.sh`).
-   Application Insights + Log Analytics dashboard.

## Prerequisites

Core (all scenarios):

-   Azure subscription with access to Azure OpenAI (model: `gpt-4o-mini`).
-   Azure CLI (latest) & Azure Developer CLI (azd).
-   Git.

If you use the Dev Container / Codespaces: everything else is preinstalled (skip to [Dev Container Quick Start](#dev-container-quick-start)).

Local machine (when NOT using the dev container):

-   Node.js 22 LTS + npm (Angular 19 + SSR build).
-   .NET 9 SDK (isolated Functions runtime).
-   Azure Functions Core Tools v4 (for `func start`).
-   (Optional but recommended) Azurite (Storage emulator) – not strictly required for the current HTTP-only function, but useful and already integrated (scripts & local artifacts). Future storage triggers will need it.

Install helpers (choose one per tool):

Azure Functions Core Tools:

```bash
# Windows (winget)
winget install Microsoft.AzureFunctionsCoreTools --source winget

# Windows (chocolatey)
choco install azure-functions-core-tools --version=4 -y

# macOS (brew)
brew tap azure/functions
brew install azure-functions-core-tools@4

# npm (all platforms; requires Node 18+)
npm i -g azure-functions-core-tools@4 --unsafe-perm true
```

Verify:

```bash
func --version
```

Azurite (pick one):

```bash
# Global npm install
npm i -g azurite

# Or run locally without install
npx azurite --location ./.azurite --silent &
```

Or install the VS Code "Azurite" extension and press the play button to start the emulator.

Optional:

-   Docker / Dev Container or Codespaces (for the zero‑setup path).

## Dev Container Quick Start

1. Use the badge above (VS Code Dev Containers) OR create a Codespace.
2. Wait for the container build (installs Node, azd, Azure CLI, Functions Core Tools, Angular CLI, .NET 9).
3. Sign in & provision:

    ```bash
    azd auth login
    azd up
    ```

4. After deployment finishes, open the printed `WEBAPP_URL`.
5. For local function + Angular development inside the container run (in split terminals):

    ```bash
    func start --csharp
    npm run start --prefix ng-web
    ```

## Quick Start (Local Alternative)

```bash
git clone https://github.com/JayChase/semantic-kernel-function-app.git
cd semantic-kernel-function-app
azd auth login
azd up
```

Answers required during `azd up`:

-   Environment name (used to derive unique resource names)
-   Subscription
-   Primary location (Function supported region)
-   OpenAI location (can differ; defaults via parameter)

On success you will see output values (e.g. `WEBAPP_URL`, function host URL). Open the web app URL in your browser.

## Deploy to Azure

All infra & app deploy with a single command:

```bash
azd up
```

Subsequent code changes (Front end / Function):

```bash
azd deploy --all
```

Infra only:

```bash
azd provision
```

Package without deploy:

```bash
azd package --all
```

## Services & Resources

| Logical           | Azure Resource                           | Notes                                                                         |
| ----------------- | ---------------------------------------- | ----------------------------------------------------------------------------- |
| Angular Front End | App Service (Linux, B1)                  | Runs Node 22 SSR command line `node /home/site/wwwroot/server/server.mjs`     |
| Chat API          | Azure Functions (Flex Consumption FC1)   | .NET 9 isolated, streaming chat endpoint `/api/chat`                          |
| AI                | Azure OpenAI (S0)                        | Deployment of `gpt-4o-mini` (capacity 15) with public network access disabled |
| Identity          | User Assigned Managed Identity           | Attached to Function for Storage + OpenAI RBAC                                |
| Storage           | Storage Account (LRS)                    | Blob container for Function deployment, network restricted                    |
| Networking        | VNet + subnets + Private Endpoints       | For Storage + Azure OpenAI private ingress                                    |
| Monitoring        | App Insights + Log Analytics + Dashboard | Provided via AVM monitoring pattern module                                    |

## Configuration & Environment Variables

Primary environment variables are surfaced as azd outputs (see `main.bicep`). Notable keys:

-   `AZURE_OPENAI_API__ENDPOINT`, `AZURE_OPENAI_API__DEPLOYMENT_NAME`, `OPENAI_API_VERSION` – consumed by Function in `Program.cs`.
-   `NG_API_URL`, `NG_FUNCTION_APP_NAME`, `NG_RG_NAME` – used to hydrate Angular environment template.
-   `APPLICATIONINSIGHTS_CONNECTION_STRING` – telemetry.

Scripts:

-   `infra/replace-placeholders.sh <template> <output>` – Rewrites Angular environment file by substituting `%KEY%` with matching `NG_` prefixed azd env values.
-   `infra/update-local-settings.sh` – Merges azd env values into `local.settings.json` for the Function.

To inspect environment values:

```bash
azd env get-values
```

## Local Development

1. Initialize env (first time): `azd init` (already done in repo) then `azd up` (provision remote resources).
2. Sync environment into local settings & Angular env file:

    ```bash
    ./infra/replace-placeholders.sh ng-web/src/environments/environment-template.ts ng-web/src/environments/environment.ts
    ./infra/update-local-settings.sh
    ```

3. Start Function locally (from repo root):

    ```bash
    cd sk-chat/SkChat
    func start
    ```

4. Start Angular dev server:

    ```bash
    cd ../../ng-web
    npm install
    npm run start  # or ng serve
    ```

5. Browse `http://localhost:4200` (adjust API base if not using deployed Function).

### Accessing Azure OpenAI from your local machine

The Azure OpenAI resource provisioned by `azd up` has **public network access disabled** and is reachable only through its private endpoint inside the VNet. That means:

-   If you run the Angular front end locally but keep calling the **deployed Function** in Azure, you do NOT need to open the OpenAI resource (the Function still runs in Azure inside the network and can reach it).
-   If you run the **Function locally** (so the call to Azure OpenAI originates from your machine) you must temporarily allow your local traffic.

Options (choose one):

1. Temporarily enable full public network access (quickest; broadest exposure).
2. Enable public access but restrict to your current public IP.
3. Use a secure network path (VPN/ExpressRoute/Dev Tunnel into the VNet) – advanced, not documented here.

#### 1. Enable full public network access (temporary)

```bash
az cognitiveservices account update \
    --name <OPENAI_NAME> \
    --resource-group <RESOURCE_GROUP> \
    --set properties.publicNetworkAccess=Enabled
```

Revert when done:

```bash
az cognitiveservices account update \
    --name <OPENAI_NAME> \
    --resource-group <RESOURCE_GROUP> \
    --set properties.publicNetworkAccess=Disabled
```

#### 2. Allow only your IP

Get your IP:

```bash
curl -s https://ifconfig.me
```

Update (keep defaultAction=Deny while adding an ipRules entry and enabling public network access so the rule applies):

```bash
IP=$(curl -s https://ifconfig.me)
az cognitiveservices account update \
    --name <OPENAI_NAME> \
    --resource-group <RESOURCE_GROUP> \
    --set properties.publicNetworkAccess=Enabled \
                properties.networkAcls.defaultAction=Deny \
                properties.networkAcls.ipRules="[ { \"value\": \"$IP\" } ]"
```

To clear the rule:

```bash
az cognitiveservices account update \
    --name <OPENAI_NAME> \
    --resource-group <RESOURCE_GROUP> \
    --set properties.networkAcls.ipRules='[]' properties.publicNetworkAccess=Disabled
```

#### Where do I get the names?

Environment outputs after `azd up`:

-   Resource group: `AZURE_RESOURCE_GROUP`
-   OpenAI account name: you can parse from `AZURE_OPENAI_API__ENDPOINT` (host prefix) or use output `AZURE_OPENAI_API_INSTANCE_` if present.

Example (extract name from endpoint):

```bash
OPENAI_NAME=$(azd env get-value AZURE_OPENAI_API__ENDPOINT | sed -E 's#https://([^\.]+).*#\1#')
RG_NAME=$(azd env get-value AZURE_RESOURCE_GROUP)
echo "$OPENAI_NAME / $RG_NAME"
```

> Security note: Always revert to the most restrictive posture (private only) once you finish local debugging.

### Using Azurite (optional offline storage)

`local.settings.json` includes Azurite artifacts; ensure Azurite extension / emulator is running if you rely on local blob triggers in future enhancements.

## Chat API Contract

Endpoint: `POST /api/chat`

Headers: `x-functions-key: <function key>` if authorization level requires.

Request (JSON):

```json
{
    "utterance": { "role": "user", "content": "Hello" },
    "history": [{ "role": "assistant", "content": "Hi!" }]
}
```

Streaming Response: New line delimited JSON `ChatMessage` objects with cumulative assistant content. Each line resembles:

```json
{ "role": "assistant", "content": "Partial or full tokens...", "messageId": "..." }
```

## Streaming Implementation Details

-   Function sets `Content-Type: text/event-stream` but emits newline‑delimited JSON (NDJSON). Front end should treat each line as a delta/cumulative update.
-   Uses `IChatClient.GetStreamingResponseAsync` from Microsoft.Extensions.AI (Semantic Kernel integration) with optional future hook for tool/function calling (`FunctionChoiceBehavior.Auto()`).
-   SSE reconnection/backpressure handled client side (not implemented here yet—consider exponential backoff & abort controller).

## Security & Networking

-   Azure OpenAI `publicNetworkAccess: Disabled`; access only via private endpoint + RBAC.
-   Storage account shared key disabled; Function deploy uses user-assigned identity + blob container deployment method.
-   VNet integration applied to Function (subnet injection) + private endpoints for OpenAI & Storage.
-   CORS on Function allows portal + web app hostname only (see `siteConfig.cors`).
-   Secrets avoided in source; identity & RBAC preferred. (OpenAI key parameter exists but not used in runtime binding—future fallback.)

## Cost & Scaling

-   App Service: B1 (baseline dev/test). Scale out by upgrading plan or migrating to container apps.
-   Function: Flex Consumption FC1 auto-scales up to 100 instances (configured `maximumInstanceCount`).
-   OpenAI deployment capacity set to 15; adjust `chatDeploymentCapacity` parameter for throughput.
-   Monitor token usage & consider model alternatives (e.g., `gpt-4o-mini` vs. `gpt-4o`) for cost optimization.

## Troubleshooting

| Issue                       | Action                                                                                                  |
| --------------------------- | ------------------------------------------------------------------------------------------------------- |
| `azd up` fails on region    | Verify chosen location is in allowed list in `main.bicep` (Flex Consumption supported + OpenAI region). |
| Function 500 / empty stream | Check Application Insights traces. Ensure `AZURE_OPENAI_API__*` env values are present.                 |
| CORS errors from front end  | Confirm Function CORS list includes web app hostname output; redeploy if changed.                       |
| OpenAI auth errors          | Confirm role assignment `Cognitive Services User` for managed identity succeeded. Re-run provision.     |
| No streaming updates        | Inspect network tab; ensure incremental lines (may need text decoder in client).                        |

## Next Steps

-   Add structured output / JSON schema validation.
-   Introduce tool/function calling examples with SK.
-   Add retry & cancellation on client side.
-   Add unit tests for chat pipeline and message shaping.
-   Implement proper SSE event framing (event: data) vs. raw NDJSON.

## Contributing

PRs welcome. Validate formatting & run `azd package --all` before submitting.

## License

See [LICENSE](./LICENSE).

---

Generated with reference to project source, infrastructure templates & azd best practices.
