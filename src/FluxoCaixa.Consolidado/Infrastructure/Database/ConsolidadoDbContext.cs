using FluxoCaixa.Consolidado.Domain;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Database;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Consolidado> ConsolidadosDiarios { get; set; }
    public DbSet<LancamentoConsolidado> LancamentosProcessados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Consolidado>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Índices otimizados para high-volume processing
            entity.HasIndex(e => new { e.Comerciante, e.Data })
                .IsUnique()
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante_Data");
            
            entity.HasIndex(e => e.Data)
                .HasDatabaseName("IX_ConsolidadoDiario_Data");
            
            entity.HasIndex(e => e.Comerciante)
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante");
            
            entity.HasIndex(e => new { e.Data, e.Comerciante })
                .HasDatabaseName("IX_ConsolidadoDiario_Data_Comerciante");
            
            // Configurações de precisão decimal
            entity.Property(e => e.TotalCreditos).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDebitos).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SaldoLiquido).HasColumnType("decimal(18,2)");
            
            // Otimizações de string
            entity.Property(e => e.Comerciante).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<LancamentoConsolidado>(entity =>
        {
            entity.HasKey(e => e.LancamentoId);
            
            // Índice para performance na busca de lançamentos processados
            entity.HasIndex(e => e.DataProcessamento)
                .HasDatabaseName("IX_LancamentoProcessado_DataProcessamento");
            
            // Configurações da string
            entity.Property(e => e.LancamentoId).HasMaxLength(50).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}