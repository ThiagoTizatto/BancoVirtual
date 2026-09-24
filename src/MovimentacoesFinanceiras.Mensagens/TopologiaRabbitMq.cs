namespace MovimentacoesFinanceiras.Mensagens;

public static class TopologiaRabbitMq
{
    public const string Exchange      = "movimentacoes";
    public const string ExchangeDlx   = "movimentacoes.dlx";
    public const string FilaPrincipal = "movimentacoes.registrar";
    public const string FilaDlq       = "movimentacoes.registrar.dlq";
    public const string RoutingKey    = "registrar";
    public const string RoutingKeyDlq = "registrar.dlq";
}
