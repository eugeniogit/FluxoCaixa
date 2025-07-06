namespace FluxoCaixa.Lancamento.Configuration;

public static class Constants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Validation
    {
        public const int MaxComercianteLength = 100;
        public const int MaxDescricaoLength = 500;
        public const double MinValor = 0.01;
    }

    public static class Configuration
    {
        public const string MongoDbSettingsSection = "MongoDbSettings";
        public const string RabbitMqSettingsSection = "RabbitMqSettings";
        public const string ApiKeySettingsSection = "ApiKeySettings";
    }

    public static class Authentication
    {
        public const string ApiKeyHeaderName = "X-API-Key";
        public const string ApiKeyScheme = "ApiKey";
    }
}