using DevVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevVault.Infrastructure.Persistence.Configurations;

public class SnippetConfiguration
{
    public void Configure(EntityTypeBuilder<Snippet> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Content)
            .IsRequired();

        builder.Property(s => s.Language)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.CreatedByUserId)
            .IsRequired();
    }
}
