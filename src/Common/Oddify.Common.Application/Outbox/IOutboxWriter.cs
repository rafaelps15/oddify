using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Application.Outbox;

// Enfileiramento explícito de integration event a partir de código que NÃO é ele mesmo despachado
// pelo OutboxProcessorJob (ex.: um CommandHandler comum, chamado síncronamente dentro do request
// original, ou qualquer serviço que precise gerar um valor sensível — como um token bruto — que
// nunca deveria virar propriedade de um domain event persistido). Grava na MESMA tabela
// outbox_messages que os domain events; OutboxProcessorJob reconhece o tipo real gravado (via
// AssemblyQualifiedName) e publica direto no bus em vez de tentar achar IDomainEventHandler<T>
// local. Um DomainEventHandler<T> já despachado pelo job NÃO precisa disso — a durabilidade já
// vem da outbox message que disparou o próprio handler; ele chama IEventBus.PublishAsync direto
// (ver §10 do CLAUDE.md).
public interface IOutboxWriter
{
    void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent;
}
