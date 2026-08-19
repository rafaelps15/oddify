namespace Oddify.Common.Application.Messaging
{
    // Alternativa a ISender.Send(command) pra quando o efeito não precisa (ou não deve) acontecer
    // dentro da mesma execução/transação de quem está enfileirando — ex.: reagir a um evento em lote
    // enfileirando N Commands sem esperar N execuções síncronas do pipeline completo de cada um.
    // EnqueueAsync grava o Command serializado numa fila durável, atômica com o SaveChanges de quem
    // chamou (ver EfCommandsScheduler); um job periódico (InternalCommandProcessorJob) desserializa e
    // reenvia via ISender.Send depois, no seu próprio ritmo. Mesmo conceito do ICommandsScheduler do
    // projeto de referência (Modular Monolith with DDD), sem a sobrecarga EnqueueAsync<T>(ICommand<T>)
    // dele — lá ela nunca foi implementada (só um NotImplementedException), porque não faz sentido: um
    // Command agendado pra depois não tem quem espere o retorno síncrono.
    public interface ICommandsScheduler
    {
        Task EnqueueAsync(ICommand command);
    }
}
