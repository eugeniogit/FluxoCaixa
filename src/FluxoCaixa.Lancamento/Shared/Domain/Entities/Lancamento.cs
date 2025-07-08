using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FluxoCaixa.Lancamento.Shared.Domain.Entities;

public class Lancamento
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = string.Empty;

    [BsonElement("comerciante")]
    public string Comerciante { get; private set; } = string.Empty;

    [BsonElement("valor")]
    public decimal Valor { get; private set; }

    [BsonElement("tipo")]
    public TipoLancamento Tipo { get; private set; }

    [BsonElement("data")]
    public DateTime Data { get; private set; }

    [BsonElement("descricao")]
    public string Descricao { get; private set; } = string.Empty;

    [BsonElement("dataLancamento")]
    public DateTime DataLancamento { get; private set; }

    [BsonElement("consolidado")]
    public bool Consolidado { get; private set; }

    private Lancamento() { }

    public Lancamento(string comerciante, decimal valor, TipoLancamento tipo, DateTime data, string descricao)
    {
        ValidateInputs(comerciante, valor, data);
        
        Id = ObjectId.GenerateNewId().ToString();
        Comerciante = comerciante;
        Valor = valor;
        Tipo = tipo;
        Data = data;
        Descricao = descricao;
        DataLancamento = DateTime.UtcNow;
        Consolidado = false;
    }

    public bool IsCredito() => Tipo == TipoLancamento.Credito;
    public bool IsDebito() => Tipo == TipoLancamento.Debito;

    public void MarcarComoConsolidado()
    {
        Consolidado = true;
    }

    private static void ValidateInputs(string comerciante, decimal valor, DateTime data)
    {
        if (string.IsNullOrWhiteSpace(comerciante))
            throw new ArgumentException("Comerciante é obrigatório", nameof(comerciante));
        
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));
        
        if (data == default)
            throw new ArgumentException("Data é obrigatória", nameof(data));
    }
}

public enum TipoLancamento
{
    Debito = 0,
    Credito = 1
}