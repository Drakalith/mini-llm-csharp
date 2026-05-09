namespace MiniLlmDidatico.Modelo;

public sealed class HiperParametrosTransformador
{
    public int TamanhoVocabulario { get; init; }
    public int MaximoComprimentoSequencia { get; init; }
    public int DimensaoEmbedding { get; init; }
    public int DimensaoFeedForward { get; init; }
    public int NumeroCamadas { get; init; }
    public int NumeroCabecasAtencao { get; init; }
    public double EpsilonLayerNorm { get; init; }
    public double TaxaAprendizado { get; init; }
    public int SementeAleatoria { get; init; }

    public int DimensaoCabeca => DimensaoEmbedding / NumeroCabecasAtencao;
}
