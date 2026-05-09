using MiniLlmDidatico.Numerica;

namespace MiniLlmDidatico.Modelo;

internal sealed class BlocoDecoder
{
    public BlocoDecoder(HiperParametrosTransformador hiperParametros, IExecutorNumerico executorNumerico, Random geradorAleatorio)
    {
        PesosConsultaPorCabeca = CriarPesosPorCabeca(hiperParametros, executorNumerico, geradorAleatorio);
        PesosChavePorCabeca = CriarPesosPorCabeca(hiperParametros, executorNumerico, geradorAleatorio);
        PesosValorPorCabeca = CriarPesosPorCabeca(hiperParametros, executorNumerico, geradorAleatorio);

        PesosProjecaoSaidaAtencao = executorNumerico.MatrizAleatoria(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoEmbedding, geradorAleatorio);
        ViesProjecaoSaidaAtencao = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding);

        NormalizacaoPreAtencao = ParametrosLayerNorm.Criar(hiperParametros.DimensaoEmbedding);
        NormalizacaoPreFeedForward = ParametrosLayerNorm.Criar(hiperParametros.DimensaoEmbedding);

        PesosFeedForward1 = executorNumerico.MatrizAleatoria(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoFeedForward, geradorAleatorio);
        ViesFeedForward1 = executorNumerico.Zeros(hiperParametros.DimensaoFeedForward);
        PesosFeedForward2 = executorNumerico.MatrizAleatoria(hiperParametros.DimensaoFeedForward, hiperParametros.DimensaoEmbedding, geradorAleatorio);
        ViesFeedForward2 = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding);
    }

    public double[][][] PesosConsultaPorCabeca { get; }
    public double[][][] PesosChavePorCabeca { get; }
    public double[][][] PesosValorPorCabeca { get; }
    public double[][] PesosProjecaoSaidaAtencao { get; }
    public double[] ViesProjecaoSaidaAtencao { get; }
    public ParametrosLayerNorm NormalizacaoPreAtencao { get; }
    public ParametrosLayerNorm NormalizacaoPreFeedForward { get; }
    public double[][] PesosFeedForward1 { get; }
    public double[] ViesFeedForward1 { get; }
    public double[][] PesosFeedForward2 { get; }
    public double[] ViesFeedForward2 { get; }

    private static double[][][] CriarPesosPorCabeca(HiperParametrosTransformador hiperParametros, IExecutorNumerico executorNumerico, Random geradorAleatorio)
    {
        var pesos = new double[hiperParametros.NumeroCabecasAtencao][][];
        for (var cabeca = 0; cabeca < hiperParametros.NumeroCabecasAtencao; cabeca++)
        {
            pesos[cabeca] = executorNumerico.MatrizAleatoria(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoCabeca, geradorAleatorio);
        }

        return pesos;
    }
}

internal sealed class ParametrosLayerNorm
{
    public required double[] Gama { get; init; }
    public required double[] Beta { get; init; }

    public static ParametrosLayerNorm Criar(int dimensao)
    {
        return new ParametrosLayerNorm
        {
            Gama = Enumerable.Repeat(1.0, dimensao).ToArray(),
            Beta = new double[dimensao]
        };
    }
}

internal sealed class GradientesBlocoDecoder
{
    public GradientesBlocoDecoder(HiperParametrosTransformador hiperParametros, IExecutorNumerico executorNumerico)
    {
        PesosConsultaPorCabeca = CriarGradientesPorCabeca(hiperParametros, executorNumerico);
        PesosChavePorCabeca = CriarGradientesPorCabeca(hiperParametros, executorNumerico);
        PesosValorPorCabeca = CriarGradientesPorCabeca(hiperParametros, executorNumerico);

        PesosProjecaoSaidaAtencao = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoEmbedding);
        ViesProjecaoSaidaAtencao = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding);

        NormalizacaoPreAtencao = new GradientesLayerNorm(hiperParametros.DimensaoEmbedding);
        NormalizacaoPreFeedForward = new GradientesLayerNorm(hiperParametros.DimensaoEmbedding);

        PesosFeedForward1 = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoFeedForward);
        ViesFeedForward1 = executorNumerico.Zeros(hiperParametros.DimensaoFeedForward);
        PesosFeedForward2 = executorNumerico.Zeros(hiperParametros.DimensaoFeedForward, hiperParametros.DimensaoEmbedding);
        ViesFeedForward2 = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding);
    }

    public double[][][] PesosConsultaPorCabeca { get; }
    public double[][][] PesosChavePorCabeca { get; }
    public double[][][] PesosValorPorCabeca { get; }
    public double[][] PesosProjecaoSaidaAtencao { get; }
    public double[] ViesProjecaoSaidaAtencao { get; }
    public GradientesLayerNorm NormalizacaoPreAtencao { get; }
    public GradientesLayerNorm NormalizacaoPreFeedForward { get; }
    public double[][] PesosFeedForward1 { get; }
    public double[] ViesFeedForward1 { get; }
    public double[][] PesosFeedForward2 { get; }
    public double[] ViesFeedForward2 { get; }

    private static double[][][] CriarGradientesPorCabeca(HiperParametrosTransformador hiperParametros, IExecutorNumerico executorNumerico)
    {
        var gradientes = new double[hiperParametros.NumeroCabecasAtencao][][];
        for (var cabeca = 0; cabeca < hiperParametros.NumeroCabecasAtencao; cabeca++)
        {
            gradientes[cabeca] = executorNumerico.Zeros(hiperParametros.DimensaoEmbedding, hiperParametros.DimensaoCabeca);
        }

        return gradientes;
    }
}

internal sealed class GradientesLayerNorm
{
    public GradientesLayerNorm(int dimensao)
    {
        Gama = new double[dimensao];
        Beta = new double[dimensao];
    }

    public double[] Gama { get; }
    public double[] Beta { get; }
}
