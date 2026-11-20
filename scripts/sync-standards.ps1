#!/usr/bin/env pwsh
# Thin wrapper: runs the sync script from Sinkronia/code-standards at the pinned version.
# Usage: pwsh ./scripts/sync-standards.ps1 [-Tag v0.2] [-Check]
# Without -Tag it uses .code-standards-version; with -Tag it re-pins to that tag.
param(
  [string]$Tag,
  [switch]$Check
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $root '.code-standards-version'
if (-not $Tag) { $Tag = (Get-Content -LiteralPath $versionFile -Raw).Trim() }

$script = Join-Path ([System.IO.Path]::GetTempPath()) "sync-standards-$([guid]::NewGuid().ToString('N')).ps1"
try {
  Invoke-WebRequest -Uri "https://raw.githubusercontent.com/Sinkronia/code-standards/$Tag/scripts/sync-standards.ps1" -OutFile $script
  & $script -Tag $Tag -ProjectRoot $root -Check:$Check
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally { Remove-Item -LiteralPath $script -Force -ErrorAction SilentlyContinue }
