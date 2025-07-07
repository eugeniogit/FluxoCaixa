namespace FluxoCaixa.Consolidado.Infrastructure.Messaging.Abstractions;

public interface IMessageBrokerFactory
{
    IMessagePublisher CreatePublisher();
    IMessageConsumer CreateConsumer();
}