namespace FluxoCaixa.Lancamento.Infrastructure.Messaging.Abstractions;

public interface IMessageBrokerFactory
{
    IMessagePublisher CreatePublisher();
    IMessageConsumer CreateConsumer();
}