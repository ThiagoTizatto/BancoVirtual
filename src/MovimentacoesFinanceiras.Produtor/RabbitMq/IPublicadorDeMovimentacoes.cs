using MovimentacoesFinanceiras.Mensagens;

namespace MovimentacoesFinanceiras.Produtor.RabbitMq;

public interface IPublicadorDeMovimentacoes
{
    Task PublicarAsync(ComandoMovimentacaoMessage mensagem, CancellationToken ct = default);
}
