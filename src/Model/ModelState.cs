using MiniLlmDidatico.Tokenizacao;
using System.Text.Json.Serialization;

namespace MiniLlmDidatico.Modelo;

public sealed class EstadoCerebroTreinado
{
    public string VersaoFormato { get; init; } = "3";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CriadoEmUtc { get; init; }
    public DateTimeOffset CriadoEmBrasilSp { get; init; }
    public string HashCorpus { get; init; } = string.Empty;
    public string HashConfiguracaoModelo { get; init; } = string.Empty;
    public string HashTextoTreino { get; init; } = string.Empty;
    public string HashContextoCognitivo { get; init; } = string.Empty;
    public string HashConfiguracaoTreino { get; init; } = string.Empty;
    public int QuantidadeEntradasCorpus { get; init; }
    public int QuantidadeDominiosCorpus { get; init; }
    public double PerdaMediaFinal { get; init; }
    public int EpocasTreinadas { get; init; }
    public ModeloTokenizador ModeloTokenizador { get; init; } = new();
    public HiperParametrosTransformador HiperParametros { get; init; } = new();
    public EstadoMiniTransformador EstadoModelo { get; init; } = new();
}

public sealed class EstadoMiniTransformador
{
    public double[][] EmbeddingsToken { get; init; } = Array.Empty<double[]>();
    public double[][] EmbeddingsPosicionais { get; init; } = Array.Empty<double[]>();
    public List<EstadoBlocoDecoder> Blocos { get; init; } = new();
    public EstadoLayerNorm NormalizacaoFinal { get; init; } = new();
    public double[][] PesosSaida { get; init; } = Array.Empty<double[]>();
    public double[] ViesSaida { get; init; } = Array.Empty<double>();
}

public sealed class EstadoBlocoDecoder
{
    public double[][][] PesosConsultaPorCabeca { get; init; } = Array.Empty<double[][]>();
    public double[][][] PesosChavePorCabeca { get; init; } = Array.Empty<double[][]>();
    public double[][][] PesosValorPorCabeca { get; init; } = Array.Empty<double[][]>();
    public double[][] PesosProjecaoSaidaAtencao { get; init; } = Array.Empty<double[]>();
    public double[] ViesProjecaoSaidaAtencao { get; init; } = Array.Empty<double>();
    public EstadoLayerNorm NormalizacaoPreAtencao { get; init; } = new();
    public EstadoLayerNorm NormalizacaoPreFeedForward { get; init; } = new();
    public double[][] PesosFeedForward1 { get; init; } = Array.Empty<double[]>();
    public double[] ViesFeedForward1 { get; init; } = Array.Empty<double>();
    public double[][] PesosFeedForward2 { get; init; } = Array.Empty<double[]>();
    public double[] ViesFeedForward2 { get; init; } = Array.Empty<double>();
}

public sealed class EstadoLayerNorm
{
    public double[] Gama { get; init; } = Array.Empty<double>();
    public double[] Beta { get; init; } = Array.Empty<double>();
}
