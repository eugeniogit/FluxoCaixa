namespace FluxoCaixa.Lancamento.Domain;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}