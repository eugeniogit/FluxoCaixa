using FluxoCaixa.Lancamento.Domain;

namespace FluxoCaixa.Lancamento.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishLancamentoEventAsync(LancamentoEvent lancamentoEvent);
}