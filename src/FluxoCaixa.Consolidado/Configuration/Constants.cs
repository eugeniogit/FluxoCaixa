namespace FluxoCaixa.Consolidado.Configuration;

public static class Constants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 1000;
        public const int MaxPageSize = 5000;
    }

    public static class Scheduling
    {
        public const string DailyConsolidationCron = "0 0 1 * * ?";
    }

    public static class Configuration
    {
        public const string LancamentoApiSettingsSection = "LancamentoApiSettings";
        public const string RabbitMqSettingsSection = "RabbitMqSettings";
        public const string ConnectionStringKey = "DefaultConnection";
    }
}