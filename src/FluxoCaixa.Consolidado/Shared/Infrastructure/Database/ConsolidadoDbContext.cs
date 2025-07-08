using FluxoCaixa.Consolidado.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Database;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Consolidado> Consolidados { get; set; }
    public DbSet<Lancamento> Lancamentos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Entities.Consolidado>(entity =>
        {
            // Configuração da chave primária
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            // Configuração de propriedades obrigatórias
            entity.Property(e => e.Comerciante)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Data)
                .IsRequired();
            
            entity.Property(e => e.UltimaAtualizacao)
                .IsRequired();
            
            // Configuração de tipos decimais com precisão
            entity.Property(e => e.TotalCreditos)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.TotalDebitos)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.SaldoLiquido)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            // Configuração de propriedades inteiras
            entity.Property(e => e.QuantidadeCreditos)
                .IsRequired();
            
            entity.Property(e => e.QuantidadeDebitos)
                .IsRequired();
            
            // Configuração de índices
            entity.HasIndex(e => new { e.Comerciante, e.Data })
                .IsUnique()
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante_Data");
            
            entity.HasIndex(e => e.Data)
                .HasDatabaseName("IX_ConsolidadoDiario_Data");
            
            entity.HasIndex(e => e.Comerciante)
                .HasDatabaseName("IX_ConsolidadoDiario_Comerciante");
            
            entity.HasIndex(e => new { e.Data, e.Comerciante })
                .HasDatabaseName("IX_ConsolidadoDiario_Data_Comerciante");
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