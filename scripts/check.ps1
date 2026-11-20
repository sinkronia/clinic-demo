#!/usr/bin/env pwsh
# Runs the same checks as CI (ci-dotnet): restore, build with warnings as errors, format, tests.
# Integration tests need Docker running (Testcontainers).
$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot '../backend/Clinic.sln'

function Invoke-Step([string]$Name, [scriptblock]$Command) {
  Write-Host "==> $Name" -ForegroundColor Cyan
  & $Command
  if ($LASTEXITCODE -ne 0) { Write-Host "FAILED: $Name" -ForegroundColor Red; exit $LASTEXITCODE }
}

Invoke-Step 'restore' { dotnet restore $solution }
Invoke-Step 'build (warnings are errors)' { dotnet build $solution --no-restore -warnaserror }
Invoke-Step 'format (BE-001)' { dotnet format $solution --verify-no-changes --no-restore }
Invoke-Step 'test' { dotnet test $solution --no-build }

Write-Host 'check: all green' -ForegroundColor Green
