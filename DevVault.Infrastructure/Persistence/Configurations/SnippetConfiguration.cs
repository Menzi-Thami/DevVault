using DevVault.Domain.Entities;
using DevVault.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevVault.Infrastructure.Persistence.Configurations;

public class SnippetConfiguration : IEntityTypeConfiguration<Snippet>
{
    public void Configure(EntityTypeBuilder<Snippet> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(Snippet.TitleMaxLength);

        builder.Property(s => s.Content)
            .IsRequired();

        // Store the Language value object as its underlying string.
        builder.Property(s => s.Language)
            .HasConversion(language => language.Value, value => Language.From(value))
            .IsRequired()
            .HasMaxLength(Language.MaxLength);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.CreatedByUserId)
            .IsRequired();

        // Serves the list query exactly: owner filter, then the keyset order.
        builder.HasIndex(s => new { s.CreatedByUserId, s.CreatedAt, s.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_Snippets_CreatedByUserId_CreatedAt_Id");
    }
}
