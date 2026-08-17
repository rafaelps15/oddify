using System.Reflection;

namespace Oddify.Common.Infrastructure.Outbox;

// Um módulo que usa a outbox contribui uma entrada aqui (schema + assembly de IntegrationEvents,
// pro job resolver o Type na hora de desserializar) — ver Program.cs/AddInfrastructure.
public sealed record OutboxModule(string Schema, Assembly MessageAssembly);
