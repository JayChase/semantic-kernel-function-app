<#
.SYNOPSIS
Replaces %KEY% placeholders in a template file with values from the current azd environment.

.DESCRIPTION
Reads all environment values from `azd env get-values` for the active environment and replaces
matching %KEY% placeholders in the provided template. Writes the processed content to the output path.

.PARAMETER TemplateFile
Path to the template file containing %KEY% placeholders.

.PARAMETER OutputFile
Path to write the processed output file.

.EXAMPLE
./infra/replace-placeholders.ps1 ng-web/src/environments/environment-template.ts ng-web/src/environments/environment.ts
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TemplateFile,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Usage {
    Write-Host "Usage: .\replace-placeholders.ps1 <template_file> <output_file>" -ForegroundColor Yellow
    Write-Host "  template_file: Path with %KEY% placeholders" -ForegroundColor Yellow
    Write-Host "  output_file: Destination path" -ForegroundColor Yellow
}

if (-not (Test-Path -LiteralPath $TemplateFile)) {
    Write-Error "Template file not found: $TemplateFile"
}

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    Write-Error "Azure Developer CLI (azd) not found on PATH. Install azd and try again."
}

Write-Host "Getting Azure environment name..."
$envName = ''
try {
    $envName = (azd env get-value AZURE_ENV_NAME 2>$null).Trim()
}
catch {
    $envName = ''
}

if ([string]::IsNullOrWhiteSpace($envName)) {
    Write-Warning "No default Azure environment found. Ensure this is an azd project and an environment is initialized."
    Write-Host  "Set a default environment with: azd env set-default <env-name>"
    throw "Missing AZURE_ENV_NAME"
}

Write-Host "Using Azure environment: $envName"

Write-Host "Loading environment variables from azd..."
$envVars = ''
try {
    $envVars = azd env get-values --environment $envName 2>$null
}
catch {
    $envVars = ''
}

if ([string]::IsNullOrWhiteSpace($envVars)) {
    Write-Warning "No environment variables returned for '$envName'. Did you run: azd provision?"
}

$content = Get-Content -LiteralPath $TemplateFile -Raw

# Parse KEY=VALUE lines
$lines = $envVars -split "`r?`n" | Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*=' }

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }

    $eq = $line.IndexOf('=')
    if ($eq -lt 1) { continue }

    $key = $line.Substring(0, $eq)
    $value = $line.Substring($eq + 1)

    if ([string]::IsNullOrWhiteSpace($key)) { continue }

    # Replace only if %KEY% exists in content
    if ($content -notlike "*%$key%*") { continue }

    $clean = $value.Trim()
    # Strip matching surrounding quotes symmetrically
    if (($clean.StartsWith('"') -and $clean.EndsWith('"')) -and $clean.Length -ge 2) {
        $clean = $clean.Substring(1, $clean.Length - 2)
    }
    elseif (($clean.StartsWith("'") -and $clean.EndsWith("'")) -and $clean.Length -ge 2) {
        $clean = $clean.Substring(1, $clean.Length - 2)
    }
    else {
        # Handle stray trailing quote without leading
        if ($clean.EndsWith('"') -and -not $clean.StartsWith('"')) { $clean = $clean.Substring(0, $clean.Length - 1) }
        if ($clean.EndsWith("'") -and -not $clean.StartsWith("'")) { $clean = $clean.Substring(0, $clean.Length - 1) }
    }

    # Build regex-safe pattern and replacement-safe value
    $pattern = [System.Text.RegularExpressions.Regex]::Escape("%$key%")
    $replacement = $clean -replace '\$', '$$'

    Write-Host "Replacing %$key%"
    $content = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        $pattern,
        { param($m) $replacement }
    )
}

# Write output (UTF8)
Set-Content -LiteralPath $OutputFile -Value $content -Encoding UTF8

Write-Host "Successfully processed template and saved to: $OutputFile" -ForegroundColor Green
