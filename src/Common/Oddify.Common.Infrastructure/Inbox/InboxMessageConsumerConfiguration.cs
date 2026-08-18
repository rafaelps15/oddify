using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Common.Infrastructure.Inbox;

// Só existe pra EF migrar a tabela — o acesso de verdade é sempre via Dapper cru, dentro de
// IdempotentIntegrationEventHandler<T> (nunca via este DbSet).
public sealed class InboxMessageConsumerConfiguration : IEntityTypeConfiguration<InboxMessageConsumer>
{
    public void Configure(EntityTypeBuilder<InboxMessageConsumer> builder)
    {
        builder.ToTable("inbox_message_consumers");

        builder.HasKey(c => new { c.InboxMessageId, c.Name });

        builder.Property(c => c.Name).HasMaxLength(500);
    }
}
