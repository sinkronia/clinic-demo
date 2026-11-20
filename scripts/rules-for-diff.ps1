#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Prints the rule files whose applies_to globs match the changed files.
.DESCRIPTION
  Usage: rules-for-diff.ps1 [<path> ...] [-RulesPath <dir>] [-BaseRef origin/main] [-IncludeProposed]
  Without paths, uses the files changed against -BaseRef (falls back to the working tree diff).
  Output: one rule file path per line, sorted by rule id. Only `active` rules are listed
  unless -IncludeProposed is given.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
  [Parameter(ValueFromRemainingArguments = $true)][string[]]$Paths,
  [string]$RulesPath,
  [string]$BaseRef = 'origin/main',
  [switch]$IncludeProposed
)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if (-not $RulesPath) {
  $RulesPath = if (Test-Path (Join-Path $root 'docs/rules')) { Join-Path $root 'docs/rules' } else { Join-Path $root 'rules' }
}

function ConvertTo-GlobRegex([string]$Glob) {
  $g = $Glob -replace '\\', '/'
  $sb = [System.Text.StringBuilder]::new()
  $i = 0
  while ($i -lt $g.Length) {
    $c = $g[$i]
    if ($c -eq '*') {
      if ($i + 1 -lt $g.Length -and $g[$i + 1] -eq '*') {
        if ($i + 2 -lt $g.Length -and $g[$i + 2] -eq '/') { [void]$sb.Append('(?:.*/)?'); $i += 3; continue }
        [void]$sb.Append('.*'); $i += 2; continue
      }
      [void]$sb.Append('[^/]*'); $i++; continue
    }
    if ($c -eq '?') { [void]$sb.Append('[^/]') } else { [void]$sb.Append([regex]::Escape([string]$c)) }
    $i++
  }
  return "^$($sb.ToString())$"
}

function Read-RuleFrontmatter([string]$File) {
  $lines = Get-Content -LiteralPath $File
  if ($lines.Count -lt 3 -or $lines[0].Trim() -ne '---') { return $null }
  $meta = @{}
  for ($i = 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq '---') { return $meta }
    if ($lines[$i] -match '^([A-Za-z_]+):\s*(.*)$') {
      $key = $Matches[1]; $val = $Matches[2].Trim()
      if ($val.StartsWith('[')) { $val = ($val.Substring(0, $val.LastIndexOf(']') + 1) | ConvertFrom-Json -NoEnumerate) }
      else { $val = ($val -replace '\s+#.*$', '').Trim().Trim('"') }
      $meta[$key] = $val
    }
  }
  return $null
}

if (-not $Paths -or $Paths.Count -eq 0) {
  $Paths = @(git diff --name-only "$BaseRef...HEAD" 2>$null)
  if ($LASTEXITCODE -ne 0 -or $Paths.Count -eq 0) { $Paths = @(git diff --name-only HEAD 2>$null) }
}
$Paths = @($Paths | Where-Object { $_ } | ForEach-Object { ($_ -replace '\\', '/').TrimStart('./') })

$matched = foreach ($file in Get-ChildItem -LiteralPath $RulesPath -Recurse -Filter '*.md' | Where-Object Name -NE 'README.md') {
  $meta = Read-RuleFrontmatter $file.FullName
  if (-not $meta -or -not $meta.id) { continue }
  if ($meta.status -ne 'active' -and -not ($IncludeProposed -and $meta.status -eq 'proposed')) { continue }
  $regexes = @($meta.applies_to | ForEach-Object { ConvertTo-GlobRegex $_ })
  $hit = $Paths | Where-Object { $p = $_; $regexes | Where-Object { $p -match $_ } } | Select-Object -First 1
  if ($hit) {
    [pscustomobject]@{ Id = $meta.id; Path = (Resolve-Path -LiteralPath $file.FullName -Relative) -replace '\\', '/' }
  }
}
$matched | Sort-Object Id | ForEach-Object { $_.Path }
