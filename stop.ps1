<#
Para a infra local (Postgres, Seq, Redis) subida pelo run.ps1.
A API em si é parada com Ctrl+C na janela onde ela está rodando.
Uso: .\stop.ps1
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host "Parando infra (Postgres, Seq, Redis)..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" down
