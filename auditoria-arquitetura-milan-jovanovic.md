# Auditoria de Arquitetura — Padrão Milan Jovanović
### Referência: Pragmatic Clean Architecture + Modular Monolith Architecture

> Fontes consultadas: blog oficial (milanjovanovic.tech/blog), template gratuito "Clean Architecture"
> (milanjovanovic.tech/templates/clean-architecture), repositórios públicos de alunos do curso
> "Pragmatic Clean Architecture" (projeto de referência: **Bookify**) e o curso "Modular Monolith
> Architecture" (projeto de referência: **Evently**).
>
> ⚠️ O código-fonte completo desses cursos é pago/autoral. Este documento **não reproduz** o código
> original linha a linha — ele descreve os padrões e traz exemplos equivalentes, escritos do zero,
> para você usar como gabarito de auditoria no seu projeto (Oddify / Tickest).

---

## 1. Estrutura de solução (5 projetos)

```
NomeDoProjeto/
├─ src/
│  ├─ Domain/              # não depende de nada
│  ├─ Application/         # depende só de Domain
│  ├─ Infrastructure/      # implementa interfaces do Application/Domain
│  ├─ Web.Api/              # composition root + endpoints
│  └─ SharedKernel/         # Result, Error, Entity base, primitivos
├─ tests/
│  ├─ ArchitectureTests/    # NetArchTest garantindo a regra de dependência
│  ├─ Application.UnitTests/
│  └─ IntegrationTests/     # Testcontainers (Postgres/SqlServer real)
```

**Regra de dependência**: as setas sempre apontam para dentro.
`Web.Api → Infrastructure → Application → Domain`. Domain não referencia nada.
Isso é validado por um teste de arquitetura (NetArchTest), não só por convenção — se alguém
importar EF Core dentro do Domain, o build quebra.

**Checklist de auditoria:**
- [ ] Existe um projeto `ArchitectureTests` que falha o build se a regra de dependência for violada?
- [ ] O projeto `Domain` não tem NENHUMA referência a pacotes externos (nem EF Core, nem MediatR)?
- [ ] `SharedKernel` contém só primitivos (Result, Error, Entity, ValueObject) — zero lógica de negócio?

---

## 2. Camada Domain

### 2.1 Entity base (SharedKernel)

```csharp
namespace SharedKernel;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent : INotification; // INotification vem do MediatR, se você usar
```

### 2.2 Entidade rica (exemplo: Aposta / Bet, adaptado ao seu domínio)

```csharp
namespace Domain.Apostas;

public sealed class Aposta : Entity
{
    private Aposta() { } // EF Core

    public Guid Id { get; private set; }
    public Guid AnaliseId { get; private set; }
    public decimal Stake { get; private set; }
    public decimal Odd { get; private set; }
    public StatusAposta Status { get; private set; }

    public static Aposta Criar(Guid analiseId, decimal stake, decimal odd)
    {
        var aposta = new Aposta
        {
            Id = Guid.NewGuid(),
            AnaliseId = analiseId,
            Stake = stake,
            Odd = odd,
            Status = StatusAposta.Pendente
        };

        aposta.Raise(new ApostaCriadaDomainEvent(aposta.Id));

        return aposta;
    }

    public Result Confirmar(DateTime agora)
    {
        if (Status != StatusAposta.Pendente)
            return Result.Failure(ApostaErrors.StatusInvalido);

        Status = StatusAposta.Confirmada;
        Raise(new ApostaConfirmadaDomainEvent(Id, agora));

        return Result.Success();
    }
}
```

**Pontos-chave do estilo Milan que costumam ser violados:**
- Construtor **privado** + **factory method estático** (`Criar`, não `new Aposta(...)` público).
- Setters **privados** — estado só muda por método de domínio com nome de intenção de negócio
  (`Confirmar`, não `SetStatus`).
- Toda mudança de estado relevante levanta um **Domain Event**.
- Regras de negócio retornam `Result`/`Result<T>`, nunca lançam exceção para fluxo de controle.
- Erros são objetos estáticos tipados (`ApostaErrors.StatusInvalido`), não strings soltas.

### 2.3 Errors por entidade

```csharp
namespace Domain.Apostas;

public static class ApostaErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Apostas.NotFound", "A aposta não foi encontrada.");

    public static readonly Error StatusInvalido = Error.Problem(
        "Apostas.StatusInvalido", "A aposta não pode ser confirmada neste status.");
}
```

**Checklist de auditoria:**
- [ ] Toda entidade tem construtor privado + factory method?
- [ ] Existe alguma entidade com setter público (`{ get; set; }`) exposto para fora do assembly?
- [ ] Toda mudança de estado relevante dispara um `DomainEvent`?
- [ ] Existe uma classe `XxxErrors` estática por agregado, ou os erros estão espalhados em strings?
- [ ] Nenhuma exceção de negócio é lançada (`throw new BusinessException(...)`) — tudo é `Result`?

---

## 3. SharedKernel: Result Pattern

```csharp
namespace SharedKernel;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
            throw new ArgumentException("Estado de erro inválido.", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado com falha.");
}

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Problem(string code, string description) => new(code, description, ErrorType.Problem);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}

public enum ErrorType { Failure, Validation, NotFound, Conflict, Problem }
```

O `ErrorType` é o que permite mapear automaticamente `Result` → `ProblemDetails` HTTP
(NotFound → 404, Validation → 400, Conflict → 409) num único middleware, sem `if/else` espalhado
pelos controllers.

**Checklist de auditoria:**
- [ ] Existe um `Result`/`Result<T>` único e reutilizado em todo o Application, ou cada feature
  inventa o seu?
- [ ] Existe mapeamento automático de `ErrorType` → status HTTP, ou cada endpoint decide manualmente?

---

## 4. Camada Application (CQRS)

### 4.1 Abstrações de Command/Query

```csharp
namespace Application.Abstractions.Messaging;

public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
```

> Observação importante para o seu caso: Milan usa MediatR no material mais antigo, mas no
> **template gratuito atual (.NET 10)** ele **abandonou o MediatR** e implementa o próprio
> dispatcher (`ICommand`/`IQuery` + `ISender` caseiro, registrado por assembly scanning), justamente
> para reduzir dependência de terceiros e ter decorators de log/validação explícitos e legíveis.
> Se no seu projeto você só viu "uma feature com CQRS", vale decidir explicitamente: **ou você
> generaliza CQRS pra tudo (ele faz isso), ou você documenta por que só usa em pontos específicos.**
> Meio-termo inconsistente é o que geralmente gera "bagunça" numa auditoria.

### 4.2 Command + Handler (write side, com EF Core e domínio rico)

```csharp
namespace Application.Apostas.Confirmar;

public sealed record ConfirmarApostaCommand(Guid ApostaId) : ICommand;

internal sealed class ConfirmarApostaCommandHandler(
    IApostaRepository apostaRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmarApostaCommand>
{
    public async Task<Result> Handle(ConfirmarApostaCommand request, CancellationToken cancellationToken)
    {
        Aposta? aposta = await apostaRepository.GetByIdAsync(request.ApostaId, cancellationToken);

        if (aposta is null)
            return Result.Failure(ApostaErrors.NotFound);

        Result resultado = aposta.Confirmar(dateTimeProvider.UtcNow);

        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 4.3 Query + Handler (read side, SQL direto — Dapper)

```csharp
namespace Application.Apostas.ListarPorAnalise;

public sealed record ListarApostasPorAnaliseQuery(Guid AnaliseId) : IQuery<IReadOnlyList<ApostaResponse>>;

internal sealed class ListarApostasPorAnaliseQueryHandler(ISqlConnectionFactory connectionFactory)
    : IQueryHandler<ListarApostasPorAnaliseQuery, IReadOnlyList<ApostaResponse>>
{
    public async Task<Result<IReadOnlyList<ApostaResponse>>> Handle(
        ListarApostasPorAnaliseQuery request, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT id AS Id, stake AS Stake, odd AS Odd, status AS Status
            FROM apostas
            WHERE analise_id = @AnaliseId
            """;

        var apostas = await connection.QueryAsync<ApostaResponse>(sql, new { request.AnaliseId });

        return apostas.ToList();
    }
}
```

**Este é o ponto mais importante da metodologia dele e o que mais costuma divergir em projetos
que "meio que seguem" Clean Architecture:**

| Lado | Ferramenta | Motivo |
|---|---|---|
| **Write** (Command) | EF Core + modelo de domínio rico + Repository | precisa carregar o agregado, rodar regra de negócio, persistir |
| **Read** (Query) | Dapper + SQL cru (ou EF com `.AsNoTracking()`/projeção) | performance, sem indireção, sem tracking de EF |

Se no seu projeto TODAS as queries também passam por EF Core com entidades de domínio
completas, isso é uma divergência clara do padrão dele — não é "certo ou errado" no absoluto,
mas não é o que ele usa e prega.

**Checklist de auditoria:**
- [ ] Cada feature tem `Command`/`Query` + `Handler` dedicado (um arquivo, uma responsabilidade),
  ou existem "services" genéricos fazendo várias coisas?
- [ ] Commands usam EF Core + repositório + entidade rica?
- [ ] Queries usam Dapper/SQL projetado, evitando carregar entidades completas de domínio?
- [ ] O handler segue esse esqueleto: carregar → validar/regra de domínio → persistir → retornar
  `Result`? Ou tem lógica de negócio vazando pro handler (`if` de regra de negócio dentro do
  handler em vez de dentro da entidade)?

### 4.4 Pipeline Behaviors (decorators) — validação e logging

```csharp
namespace Application.Abstractions.Behaviors;

internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var errors = validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (errors.Count == 0)
            return await next();

        // converte erros de FluentValidation em Result.Failure com ErrorType.Validation
        throw new ValidationException(errors);
    }
}
```

Validação, logging e (se usar) cache **nunca** ficam dentro do handler — são decorators/pipeline
behaviors que "envolvem" o handler. Isso é o que ele chama de manter o handler "burro e legível".

**Checklist de auditoria:**
- [ ] Existe `ValidationPipelineBehavior` (ou equivalente) centralizando FluentValidation, ou cada
  handler valida manualmente no início (`if (string.IsNullOrEmpty(x)) return ...`)?
- [ ] Existe logging cross-cutting (behavior) em vez de `_logger.LogInformation` espalhado dentro
  de cada handler?

---

## 5. Camada Infrastructure

### 5.1 Repositório (NÃO genérico)

```csharp
namespace Application.Apostas; // a interface mora no Application (ou Domain), não no Infrastructure

public interface IApostaRepository
{
    Task<Aposta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Aposta aposta);
}
```

```csharp
namespace Infrastructure.Apostas;

internal sealed class ApostaRepository(ApplicationDbContext context) : IApostaRepository
{
    public async Task<Aposta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Apostas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Add(Aposta aposta) => context.Apostas.Add(aposta);
}
```

**Importante**: ele **não** usa `IGenericRepository<T>` com `Get/Add/Update/Delete` genéricos.
Cada agregado tem seu repositório específico, com só os métodos que aquele agregado realmente
precisa (YAGNI aplicado ao repositório também — isso conversa direto com o que você já pratica).
`SaveChanges` fica fora do repositório, num `IUnitOfWork` (ou direto no `DbContext` exposto via
interface `IApplicationDbContext`, no template mais novo).

### 5.2 Unit of Work

```csharp
namespace Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 5.3 EF Core Entity Configuration

```csharp
namespace Infrastructure.Apostas;

internal sealed class ApostaConfiguration : IEntityTypeConfiguration<Aposta>
{
    public void Configure(EntityTypeBuilder<Aposta> builder)
    {
        builder.ToTable("apostas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Stake).HasPrecision(18, 2);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
```

Configuração fica em classe separada por entidade (`IEntityTypeConfiguration<T>`), nunca com
Fluent API solta dentro de `OnModelCreating`.

### 5.4 Interceptors — domain events → outbox

Dois interceptors, cada um com sua responsabilidade e seu momento do ciclo de vida do EF:

```csharp
// 1) Antes de salvar: converte alterações de auditoria (CreatedOnUtc, ModifiedOnUtc)
internal sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }
    // ... lógica de CreatedOnUtc/ModifiedOnUtc
}

// 2) Depois de salvar: pega os domain events acumulados nas entidades e grava como mensagens
//    de Outbox (na MESMA transação lógica, pois roda no mesmo SaveChanges)
internal sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        DbContext? context = eventData.Context;
        if (context is null) return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var outboxMessages = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.DomainEvents;
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow,
                Type = domainEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(domainEvent, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                })
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
        await context.SaveChangesAsync(cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
```

Sua nota de memória já registra que no Tickest você tem
`AuditableEntityInterceptor` (`SavingChanges`) + `PublishDomainEventsInterceptor` (`SavedChanges`)
— isso está **alinhado** com o padrão dele. O ponto de auditoria aqui é: o segundo interceptor
está **gravando na tabela Outbox** ou está **disparando o `INotification` direto via MediatR
`Publish`**? As duas abordagens existem no material dele (a segunda é mais simples, a primeira
[Outbox] é a que garante entrega mesmo se o processo cair). Vale decidir qual das duas você quer
formalmente e documentar o motivo, em vez de ter as duas coexistindo por acidente.

### 5.5 Outbox Processor (job em background)

```csharp
[DisallowConcurrentExecution]
internal sealed class ProcessOutboxMessagesJob(
    ApplicationDbContext dbContext,
    IPublisher publisher) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync(context.CancellationToken);

        foreach (var message in messages)
        {
            IDomainEvent? domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                message.Content, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            if (domainEvent is null) continue;

            try
            {
                await publisher.Publish(domainEvent, context.CancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.ToString();
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
```

Ele usa **Quartz.NET** para agendar esse job recorrente (não Hangfire, não `BackgroundService`
manual com `Task.Delay` em loop).

**Checklist de auditoria (Outbox):**
- [ ] Existe uma tabela `outbox_messages` com `Id`, `Type`, `Content`, `OccurredOnUtc`,
  `ProcessedOnUtc`, `Error`?
- [ ] O outbox é populado dentro do MESMO `SaveChanges` que persiste a entidade (garantia
  transacional), ou é um passo separado que pode falhar independentemente?
- [ ] Existe um job/worker separado consumindo o outbox, com retry e limite de mensagens por
  execução (`Take(20)`), ou tudo é processado de uma vez, sem controle de falha parcial?
- [ ] Existe um segundo interceptor para AUDITORIA (`CreatedOnUtc`/`ModifiedOnUtc`) separado do
  interceptor de outbox, ou está tudo misturado numa classe só?

---

## 6. Camada Web.Api (Minimal APIs, não Controllers, no template mais novo)

```csharp
internal sealed class ConfirmarAposta : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("apostas/{id:guid}/confirmar", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmarApostaCommand(id);
            Result result = await sender.Send(command, cancellationToken);

            return result.IsFailure
                ? CustomResults.Problem(result)
                : Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags(Tags.Apostas);
    }
}
```

Cada endpoint é uma classe própria implementando `IEndpoint`, registrada automaticamente por
assembly scanning — não existe um `ApostasController` gigante com 15 actions. Isso é o mesmo
princípio de "um arquivo, uma responsabilidade" do CQRS aplicado à camada de apresentação.

`CustomResults.Problem(result)` é o mapeamento único e centralizado de `Result.Error.Type` para
`ProblemDetails` (400/404/409/422), citado na seção 3.

**Checklist de auditoria:**
- [ ] Endpoints são classes individuais (`IEndpoint`) ou Controllers tradicionais com múltiplas
  actions? (Nenhuma das duas é "errada", mas escolha uma e seja consistente.)
- [ ] Existe UM lugar só que traduz `Result` → resposta HTTP, ou cada endpoint decide o status
  code manualmente?

---

## 7. Modular Monolith — regras específicas (curso "Modular Monolith Architecture")

> ✅ **Seção revisada e verificada diretamente nos artigos oficiais dele** (não é mais só
> conhecimento geral): "Modular Monolith Communication Patterns", "What Is a Modular Monolith?",
> "Modular Monolith Data Isolation" e "Modular Monolith vs Microservices" — todos em
> milanjovanovic.tech/blog. Isso muda alguns detalhes importantes em relação à primeira versão.

Isso é o que muda quando você sai de "um Clean Architecture só" para "vários módulos dentro do
mesmo monolito" — que é exatamente onde o Oddify está (Vertex, motor estatístico, etc. como
módulos dentro do mesmo processo).

### 7.1 Regra de ouro
Módulos **não podem se referenciar diretamente**, exceto através da **API pública** de cada um.
Um módulo expõe uma `interface` pública em .NET; a implementação interna fica marcada como
`internal`, inacessível de fora do módulo. Outros módulos dependem só da interface em tempo de
compilação; o DI resolve a implementação em tempo de execução.

### 7.2 Duas formas de comunicação entre módulos (e só duas)

| Padrão | Como funciona | Vantagem | Desvantagem |
|---|---|---|---|
| **Síncrona (method calls)** | Módulo A chama um método na interface pública do Módulo B e espera o retorno. É uma chamada em memória. | Rápida, simples, sem indireção | Acoplamento forte — se a lógica do Módulo B falha, a chamada do Módulo A falha junto |
| **Assíncrona (mensageria)** | Módulo A publica uma mensagem num broker (fire-and-forget); Módulo B assina e reage. Os módulos só precisam concordar no **contrato da mensagem**, não um no outro. | Alta disponibilidade, baixo acoplamento | Mais complexidade operacional (precisa de um broker); ele cita explicitamente **MassTransit com transporte in-memory** como forma de começar sem subir um broker de verdade logo de cara |

Ele é explícito: **não existe uma terceira opção** — é uma dessas duas, escolhida por par de
módulos conforme o caso de uso. Para evitar perda de mensagem na comunicação assíncrona, ele
recomenda gravar a mensagem no **Outbox antes de publicar** — mesmo mecanismo de outbox da
seção 5.4/5.5 deste documento, só que agora a mensagem cruza a fronteira do módulo em vez de
ficar só dentro dele.

### 7.3 Isolamento de dados — 4 níveis, começando pelo mais simples

Ele descreve explicitamente 4 abordagens de isolamento de dados, em ordem crescente de rigor
(e de custo operacional):

1. **Nenhum isolamento** (banco único, tabelas misturadas) — o que ele está tentando te tirar daí.
2. **Isolamento lógico por schema** — cada módulo tem seu próprio schema no mesmo banco físico
   (`apostas.*`, `analises.*`). **Esse é o ponto de partida que ele recomenda sempre** ("eu sempre
   começo com isolamento lógico usando schemas").
3. **Bancos de dados separados** (mesmo servidor ou não) — mais rigor, mais complexidade
   operacional (migrations, conexões, backups por módulo).
4. **Persistência diferente por módulo** (ex.: um módulo em Postgres, outro em um banco
   NoSQL) — o nível mais próximo de microsserviço de fato.

A regra que vale para todos os níveis: **um módulo só pode acessar suas próprias tabelas.**
Nunca compartilha tabela com outro módulo, nunca faz query direta na tabela de outro módulo.

**Como implementar isso em EF Core**, segundo o material dele: um `DbContext` por módulo,
cada um com `modelBuilder.HasDefaultSchema("apostas")` (ou schema equivalente) e só os
`DbSet<T>` daquele módulo. Não existe um `ApplicationDbContext` único com todas as entidades de
todos os módulos.

### 7.4 Estrutura de projeto por módulo

Cada módulo tende a ser "quase" um Clean Architecture completo dentro de si — Domain,
Application, Infrastructure próprios — só que sem projeto `Web.Api` próprio: existe **um único**
projeto de entrada (host) que referencia todos os módulos, registra os endpoints de cada um
(auto-scan) e monta o pipeline HTTP compartilhado (auth, logging, health checks — tratados como
"cross-cutting concerns" configurados uma vez, não duplicados por módulo).

**Checklist de auditoria (Modular Monolith) — atualizado:**
- [ ] Cada módulo expõe uma **interface pública** e esconde a implementação com `internal`? Ou
  outros módulos importam classes concretas de dentro de outro módulo?
- [ ] Para cada par de módulos que se comunica, foi uma **decisão consciente** síncrona
  (method call) vs. assíncrona (mensageria), ou aconteceu "que dava" no momento?
- [ ] Se é assíncrono: a mensagem passa pelo Outbox antes de ser publicada, ou pode se perder se
  o processo cair entre salvar o estado e publicar?
- [ ] Que nível de isolamento de dados o Oddify usa hoje entre os módulos (Vertex, motor
  estatístico, apostas)? Nenhum, schema, banco separado? Ele recomenda começar em **schema**, não
  pular direto pra banco separado sem necessidade real.
- [ ] Existe um `DbContext` por módulo (com schema próprio), ou um `ApplicationDbContext` único
  mapeando entidades de todos os módulos juntos?
- [ ] Algum módulo faz `JOIN`/query direto em tabela de outro módulo? (Violação direta da regra
  de ouro — o item mais crítico da auditoria.)

---

## 8. Nomenclatura e convenções observadas no material dele

- Domínio em inglês no código-fonte dele (você já decidiu português sem acento — mantenha
  consistente, isso não é uma regra rígida dele, é escolha sua e está ok).
- Sufixos: `XxxCommand`, `XxxCommandHandler`, `XxxQuery`, `XxxQueryHandler`, `XxxRequest` (input
  de endpoint), `XxxResponse` (DTO de saída), `XxxConfiguration` (EF), `XxxErrors` (estático).
- `internal sealed class` para handlers e implementações de repositório — só as interfaces e os
  Commands/Queries são `public` (o que precisa ser visível fora do assembly).
- Records para Command/Query (`public sealed record XxxCommand(...) : ICommand`), classes para
  entidades (porque entidades têm identidade e comportamento, não são só dados imutáveis).

---

## 9. Roteiro sugerido de auditoria (ordem prática)

1. Rodar o teste de arquitetura (ou criar um, se não existir) para confirmar a regra de
   dependência entre camadas.
2. Escolher UM agregado do Oddify (ex.: `Aposta` ou `Analise`) e revisar linha a linha contra a
   seção 2 (Domain) — construtor privado, factory, Result em vez de exceção, domain events.
3. Revisar o handler correspondente contra a seção 4 — write via EF+repositório rico, read via
   Dapper.
4. Revisar se validação/log estão no handler ou em pipeline behavior (seção 4.4).
5. Revisar a cadeia de interceptors → outbox → job de processamento (seção 5.4/5.5) — esse é
   normalmente o ponto mais frágil em implementações "quase certas".
6. Se o Oddify já tem mais de um módulo (Vertex, motor estatístico, etc.), aplicar a seção 7 —
   provavelmente o maior gap, porque é o padrão mais recente e menos praticado por você até agora.
7. Documentar cada divergência encontrada como um item de backlog técnico, com a decisão
   explícita: "manter diferente por motivo X" ou "migrar para o padrão do Milan".

---

**Como usar isso com o Claude Code no seu projeto:**

Cole este arquivo no repositório (ex.: `docs/auditoria-milan.md`) e peça para o Claude Code
percorrer `src/` comparando cada seção contra os arquivos reais, gerando uma lista de
divergências por módulo/agregado, seção por seção, na ordem do item 9 acima.
