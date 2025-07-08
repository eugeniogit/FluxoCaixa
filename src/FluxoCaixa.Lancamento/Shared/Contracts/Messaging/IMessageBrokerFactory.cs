namespace FluxoCaixa.Lancamento.Shared.Contracts.Messaging;

public interface IMessageBrokerFactory
{
    IMessagePublisher CreatePublisher();
    IMessageConsumer CreateConsumer();
}