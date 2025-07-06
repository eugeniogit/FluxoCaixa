namespace FluxoCaixa.Lancamento.Configuration;

public class ApiKeySettings
{
    public List<ApiKeyInfo> ValidApiKeys { get; set; } = new();
}

public class ApiKeyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}