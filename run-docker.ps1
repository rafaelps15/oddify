<#
Sobe TUDO em Docker — infra (Postgres, Seq, Redis) + a própria API containerizada
(Oddify.Api, http://localhost:8080). Não precisa do SDK do .NET instalado.
Alternativa ao run.ps1 (que roda a API nativa via dotnet run).
Uso: .\run-docker.ps1
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host "Subindo infra + Oddify.Api em container (http://localhost:8080)..." -ForegroundColor Cyan
docker compose -f "$root\docker-compose.yml" up -d --build

Write-Host "Pronto. Logs da API: docker logs -f Oddify.Api" -ForegroundColor Green
