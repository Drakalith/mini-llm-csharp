namespace MiniLlmDidatico.Modelo;

public sealed class CachePassagemDireta
{
    public required int[] IdsEntrada { get; init; }
    public required double[][] EmbeddingEntrada { get; init; }
    public required List<CacheBlocoTransformador> Blocos { get; init; }
    public required CacheLayerNorm NormalizacaoFinal { get; init; }
    public required double[][] EstadoFinal { get; init; }
    public required double[][] Logits { get; init; }
    public required double[][] Probabilidades { get; init; }
}

public sealed class CacheBlocoTransformador
{
    public required double[][] Entrada { get; init; }
    public required CacheLayerNorm NormalizacaoPreAtencao { get; init; }
    public required List<CacheCabecaAtencao> CabecasAtencao { get; init; }
    public required double[][] ContextoConcatenado { get; init; }
    public required double[][] ProjecaoSaidaAtencao { get; init; }
    public required double[][] EstadoPosAtencao { get; init; }
    public required CacheLayerNorm NormalizacaoPreFeedForward { get; init; }
    public required double[][] PreAtivacaoFeedForward { get; init; }
    public required double[][] OcultaFeedForward { get; init; }
    public required double[][] SaidaFeedForward { get; init; }
    public required double[][] Saida { get; init; }
}

public sealed class CacheCabecaAtencao
{
    public required double[][] Consultas { get; init; }
    public required double[][] Chaves { get; init; }
    public required double[][] Valores { get; init; }
    public required double[][] Escores { get; init; }
    public required double[][] Probabilidades { get; init; }
    public required double[][] Contexto { get; init; }
}

public sealed class CacheLayerNorm
{
    public required double[][] Entrada { get; init; }
    public required double[][] Normalizado { get; init; }
    public required double[][] Saida { get; init; }
    public required double[] Medias { get; init; }
    public required double[] Variancias { get; init; }
    public required double[] InversosDesvioPadrao { get; init; }
}
