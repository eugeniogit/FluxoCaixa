namespace FluxoCaixa.Consolidado.Configuration;

public class ConsolidationSettings
{
    public const string SectionName = "ConsolidationSettings";
    
    public int BatchSize { get; set; } = 10000;
    public int BatchCommitSize { get; set; } = 1000;
    public int MaxConcurrency { get; set; } = 4;
    public int TimeoutMinutes { get; set; } = 60;
    public bool EnableProgressLogging { get; set; } = true;
}