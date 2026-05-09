namespace MiniLlmDidatico.Modelo;

public sealed class RetratoModelo
{
    public string NomeBackendNumerico { get; set; } = string.Empty;
    public string DispositivoExecucao { get; set; } = string.Empty;
    public string DispositivoPreferidoPeloHardware { get; set; } = string.Empty;
    public int TamanhoVocabulario { get; set; }
    public int MaximoComprimentoSequencia { get; set; }
    public int DimensaoEmbedding { get; set; }
    public int DimensaoFeedForward { get; set; }
    public int NumeroCamadas { get; set; }
    public int NumeroCabecasAtencao { get; set; }
    public int DimensaoCabeca { get; set; }
    public double EpsilonLayerNorm { get; set; }
    public double TaxaAprendizado { get; set; }
    public Dictionary<string, string> Formas { get; set; } = new();
}
