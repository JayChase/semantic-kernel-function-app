<#
.SYNOPSIS
Merge azd environment variables into sk-chat/SkChat/local.settings.json under Values.

.DESCRIPTION
Reads current azd environment values and replaces the Values object in local.settings.json
with a dictionary of KEY: VALUE from azd env get-values. Preserves other properties.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "Getting Azure environment name..."
$envName = ''
try { $envName = (azd env get-value AZURE_ENV_NAME 2>$null).Trim() } catch { $envName = '' }

if ([string]::IsNullOrWhiteSpace($envName)) {
    Write-Warning "No default Azure environment found. Ensure azd environment is initialized."
    Write-Host  "Set a default environment with: azd env set-default <env-name>"
    throw "Missing AZURE_ENV_NAME"
}

Write-Host "Using Azure environment: $envName"

$localSettingsPath = Join-Path -Path $PSScriptRoot -ChildPath '../sk-chat/SkChat/local.settings.json' | Resolve-Path | Select-Object -ExpandProperty Path

Write-Host "Loading environment variables from azd..."
$envVarsRaw = ''
try { $envVarsRaw = azd env get-values --environment $envName 2>$null } catch { $envVarsRaw = '' }

if ([string]::IsNullOrWhiteSpace($envVarsRaw)) {
    Write-Warning "No environment variables found for environment '$envName'"
}

# Parse KEY=VALUE lines into a hashtable
$values = [ordered]@{}
$lines = $envVarsRaw -split "`r?`n" | Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*=' }
foreach ($line in $lines) {
    $eq = $line.IndexOf('=')
    if ($eq -lt 1) { continue }
    $key = $line.Substring(0, $eq).Trim()
    $val = $line.Substring($eq + 1)
    if ([string]::IsNullOrWhiteSpace($key)) { continue }
    # Strip balanced quotes
    $val = $val.Trim()
    if (($val.StartsWith('"') -and $val.EndsWith('"')) -and $val.Length -ge 2) { $val = $val.Substring(1, $val.Length - 2) }
    elseif (($val.StartsWith("'") -and $val.EndsWith("'")) -and $val.Length -ge 2) { $val = $val.Substring(1, $val.Length - 2) }
    else {
        if ($val.EndsWith('"') -and -not $val.StartsWith('"')) { $val = $val.Substring(0, $val.Length - 1) }
        if ($val.EndsWith("'") -and -not $val.StartsWith("'")) { $val = $val.Substring(0, $val.Length - 1) }
    }
    $values[$key] = $val
}

# Load existing local.settings.json
if (-not (Test-Path -LiteralPath $localSettingsPath)) {
    Write-Host "Creating new local.settings.json"
    $obj = [ordered]@{ IsEncrypted = $false; Values = @{} }
}
else {
    $obj = Get-Content -LiteralPath $localSettingsPath -Raw | ConvertFrom-Json -AsHashtable
    if (-not $obj.ContainsKey('Values')) { $obj['Values'] = @{} }
}

# Replace Values with env vars
$obj['Values'] = $values

# Write back (UTF8 without BOM)
$json = $obj | ConvertTo-Json -Depth 10
Set-Content -LiteralPath $localSettingsPath -Value $json -Encoding utf8

Write-Host "Updated: $localSettingsPath" -ForegroundColor Green
