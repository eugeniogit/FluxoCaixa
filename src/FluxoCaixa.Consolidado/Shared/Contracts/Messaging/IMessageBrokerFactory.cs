namespace FluxoCaixa.Consolidado.Shared.Contracts.Messaging;

public interface IMessageBrokerFactory
{
    IMessagePublisher CreatePublisher();
    IMessageConsumer CreateConsumer();
}