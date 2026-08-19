namespace Oddify.Common.Infrastructure.Processing
{
    // Uma linha da fila de comandos internos: guarda o Command serializado (Type + Content) até o
    // InternalCommandProcessorJob reenviá-lo via ISender.Send. Id é sempre um Guid novo gerado aqui —
    // diferente de OutboxMessage (que reaproveita o Id do próprio evento), a maioria dos Command deste
    // projeto não expõe um Id próprio. Sem RetryCount/Error — mesmo raciocínio de OutboxMessage: uma
    // falha deixa a linha pendente pra ser repescada na próxima rodada do job, sem contagem de
    // tentativas nem exaustão.
    public class InternalCommand
    {
        public Guid Id { get; init; }

        public string Type { get; init; }

        public string Content { get; init; }

        public DateTime EnqueuedOnUtc { get; init; }

        public DateTime? ProcessedOnUtc { get; init; }

        public static InternalCommand Create(string type, string content, DateTime enqueuedOnUtc)
        {
            var internalCommand = new InternalCommand
            {
                Id = Guid.NewGuid(),
                Type = type,
                Content = content,
                EnqueuedOnUtc = enqueuedOnUtc
            };

            return internalCommand;
        }
    }
}
