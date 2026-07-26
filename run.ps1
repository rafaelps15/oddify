<#
Sobe a infra local (Postgres, Seq, Redis) via Docker Compose e roda a API nativa (dotnet run) em
Development. A API NÃO sobe em container aqui — para isso use run-docker.ps1.
Uso: .\run.ps1  (a partir da raiz do repo, ou de qualquer lugar)
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host "Subindo infra (Postgres, Seq, Redis)..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" up -d oddify.database oddify.seq oddify.redis

Write-Host "Subindo Oddify.Api (https://localhost:54457)..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project "$root\src\API\Oddify.Api"
