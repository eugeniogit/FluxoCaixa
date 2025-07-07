using FluxoCaixa.Consolidado.Domain;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Database;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Consolidado> Consolidados { get; set; }
    public DbSet<Lancamento> Lancamentos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Consolidado>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.Comerciante, e.Data })
                .IsUnique()
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante_Data");
            
            entity.HasIndex(e => e.Data)
                .HasDatabaseName("IX_ConsolidadoDiario_Data");
            
            entity.HasIndex(e => e.Comerciante)
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante");
            
            entity.HasIndex(e => new { e.Data, e.Comerciante })
                .HasDatabaseName("IX_ConsolidadoDiario_Data_Comerciante");
            
            entity.Property(e => e.TotalCreditos).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDebitos).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SaldoLiquido).HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.Comerciante).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Lancamento>(entity =>
        {
            entity.HasKey(e => e.LancamentoId);
            
            entity.HasIndex(e => e.DataProcessamento)
                .HasDatabaseName("IX_LancamentoProcessado_DataProcessamento");
            
            entity.Property(e => e.LancamentoId).HasMaxLength(50).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}