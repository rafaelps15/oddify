# Oddify

Backend do Oddify: um sistema quantitativo de apostas esportivas, estruturado como um **modular
monolith** em .NET. Módulos de negócio independentes (`Fixtures`, `Analise`, `Apostas`, `Users`)
compartilham um único banco Postgres (um schema por módulo) e são hospedados em uma única
aplicação (`src/Api`).

Veja `CLAUDE.md` para as convenções de arquitetura, camadas e como estender cada módulo.

## Rodando localmente

Duas formas — escolha uma, não misture (as duas usam as mesmas portas de infra):

```bash
# Opção A — API nativa, infra em Docker (iteração mais rápida, debugger conecta direto)
./run.ps1          # docker compose up -d (Postgres+Seq+Redis) e depois dotnet run
./stop.ps1         # docker compose down

# Opção B — tudo em Docker, incluindo a própria API (http://localhost:8080)
./run-docker.ps1   # docker compose up -d --build (infra + container da API)
docker compose down
```

```bash
# Build / restore
dotnet restore Oddify.sln
dotnet build Oddify.sln

# Testes
dotnet test Oddify.sln
```

## Integrações externas

- **API-Football** e **The Odds API** — sincronização diária de ligas/equipes/partidas/resultados
  e cotações (módulo `Fixtures`). Requer `APIFOOTBALL_API_KEY` e `THEODDSAPI_API_KEY` como
  variáveis de ambiente.
- **Anthropic (Claude)** — camada crítica que avalia as oportunidades identificadas pelo modelo
  Poisson-Dixon-Coles (módulo `Analise`). Requer `ANTHROPIC_API_KEY`.
