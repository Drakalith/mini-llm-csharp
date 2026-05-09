namespace MiniLlmDidatico.Modelo;

public sealed partial class MiniTransformador
{
    public EstadoMiniTransformador ExportarEstado()
    {
        return new EstadoMiniTransformador
        {
            EmbeddingsToken = ClonarMatriz(_embeddingsToken),
            EmbeddingsPosicionais = ClonarMatriz(_embeddingsPosicionais),
            Blocos = _blocos.Select(ExportarBloco).ToList(),
            NormalizacaoFinal = ExportarLayerNorm(_normalizacaoFinal),
            PesosSaida = ClonarMatriz(_pesosSaida),
            ViesSaida = ClonarVetor(_viesSaida)
        };
    }

    public void AplicarEstado(EstadoMiniTransformador estado)
    {
        CopiarMatriz(_embeddingsToken, estado.EmbeddingsToken, nameof(estado.EmbeddingsToken));
        CopiarMatriz(_embeddingsPosicionais, estado.EmbeddingsPosicionais, nameof(estado.EmbeddingsPosicionais));

        if (estado.Blocos.Count != _blocos.Length)
        {
            throw new InvalidOperationException($"Quantidade de blocos incompativel. Estado={estado.Blocos.Count}, modelo={_blocos.Length}");
        }

        for (var indice = 0; indice < _blocos.Length; indice++)
        {
            AplicarEstadoBloco(_blocos[indice], estado.Blocos[indice], indice);
        }

        AplicarEstadoLayerNorm(_normalizacaoFinal, estado.NormalizacaoFinal, nameof(estado.NormalizacaoFinal));
        CopiarMatriz(_pesosSaida, estado.PesosSaida, nameof(estado.PesosSaida));
        CopiarVetor(_viesSaida, estado.ViesSaida, nameof(estado.ViesSaida));
    }

    private static EstadoBlocoDecoder ExportarBloco(BlocoDecoder bloco)
    {
        return new EstadoBlocoDecoder
        {
            PesosConsultaPorCabeca = ClonarTensor3(bloco.PesosConsultaPorCabeca),
            PesosChavePorCabeca = ClonarTensor3(bloco.PesosChavePorCabeca),
            PesosValorPorCabeca = ClonarTensor3(bloco.PesosValorPorCabeca),
            PesosProjecaoSaidaAtencao = ClonarMatriz(bloco.PesosProjecaoSaidaAtencao),
            ViesProjecaoSaidaAtencao = ClonarVetor(bloco.ViesProjecaoSaidaAtencao),
            NormalizacaoPreAtencao = ExportarLayerNorm(bloco.NormalizacaoPreAtencao),
            NormalizacaoPreFeedForward = ExportarLayerNorm(bloco.NormalizacaoPreFeedForward),
            PesosFeedForward1 = ClonarMatriz(bloco.PesosFeedForward1),
            ViesFeedForward1 = ClonarVetor(bloco.ViesFeedForward1),
            PesosFeedForward2 = ClonarMatriz(bloco.PesosFeedForward2),
            ViesFeedForward2 = ClonarVetor(bloco.ViesFeedForward2)
        };
    }

    private static EstadoLayerNorm ExportarLayerNorm(ParametrosLayerNorm layerNorm)
    {
        return new EstadoLayerNorm
        {
            Gama = ClonarVetor(layerNorm.Gama),
            Beta = ClonarVetor(layerNorm.Beta)
        };
    }

    private static void AplicarEstadoBloco(BlocoDecoder bloco, EstadoBlocoDecoder estado, int indiceBloco)
    {
        CopiarTensor3(bloco.PesosConsultaPorCabeca, estado.PesosConsultaPorCabeca, $"Blocos[{indiceBloco}].PesosConsultaPorCabeca");
        CopiarTensor3(bloco.PesosChavePorCabeca, estado.PesosChavePorCabeca, $"Blocos[{indiceBloco}].PesosChavePorCabeca");
        CopiarTensor3(bloco.PesosValorPorCabeca, estado.PesosValorPorCabeca, $"Blocos[{indiceBloco}].PesosValorPorCabeca");
        CopiarMatriz(bloco.PesosProjecaoSaidaAtencao, estado.PesosProjecaoSaidaAtencao, $"Blocos[{indiceBloco}].PesosProjecaoSaidaAtencao");
        CopiarVetor(bloco.ViesProjecaoSaidaAtencao, estado.ViesProjecaoSaidaAtencao, $"Blocos[{indiceBloco}].ViesProjecaoSaidaAtencao");
        AplicarEstadoLayerNorm(bloco.NormalizacaoPreAtencao, estado.NormalizacaoPreAtencao, $"Blocos[{indiceBloco}].NormalizacaoPreAtencao");
        AplicarEstadoLayerNorm(bloco.NormalizacaoPreFeedForward, estado.NormalizacaoPreFeedForward, $"Blocos[{indiceBloco}].NormalizacaoPreFeedForward");
        CopiarMatriz(bloco.PesosFeedForward1, estado.PesosFeedForward1, $"Blocos[{indiceBloco}].PesosFeedForward1");
        CopiarVetor(bloco.ViesFeedForward1, estado.ViesFeedForward1, $"Blocos[{indiceBloco}].ViesFeedForward1");
        CopiarMatriz(bloco.PesosFeedForward2, estado.PesosFeedForward2, $"Blocos[{indiceBloco}].PesosFeedForward2");
        CopiarVetor(bloco.ViesFeedForward2, estado.ViesFeedForward2, $"Blocos[{indiceBloco}].ViesFeedForward2");
    }

    private static void AplicarEstadoLayerNorm(ParametrosLayerNorm destino, EstadoLayerNorm origem, string nomeCampo)
    {
        CopiarVetor(destino.Gama, origem.Gama, nomeCampo + ".Gama");
        CopiarVetor(destino.Beta, origem.Beta, nomeCampo + ".Beta");
    }

    private static double[] ClonarVetor(IReadOnlyList<double> origem)
    {
        var clone = new double[origem.Count];
        for (var indice = 0; indice < origem.Count; indice++)
        {
            clone[indice] = origem[indice];
        }

        return clone;
    }

    private static double[][] ClonarMatriz(IReadOnlyList<IReadOnlyList<double>> origem)
    {
        var clone = new double[origem.Count][];
        for (var linha = 0; linha < origem.Count; linha++)
        {
            clone[linha] = ClonarVetor(origem[linha]);
        }

        return clone;
    }

    private static double[][][] ClonarTensor3(IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> origem)
    {
        var clone = new double[origem.Count][][];
        for (var indice = 0; indice < origem.Count; indice++)
        {
            clone[indice] = ClonarMatriz(origem[indice]);
        }

        return clone;
    }

    private static void CopiarVetor(double[] destino, IReadOnlyList<double> origem, string nomeCampo)
    {
        if (destino.Length != origem.Count)
        {
            throw new InvalidOperationException($"Tamanho incompativel em {nomeCampo}. Destino={destino.Length}, origem={origem.Count}");
        }

        for (var indice = 0; indice < destino.Length; indice++)
        {
            destino[indice] = origem[indice];
        }
    }

    private static void CopiarMatriz(double[][] destino, IReadOnlyList<IReadOnlyList<double>> origem, string nomeCampo)
    {
        if (destino.Length != origem.Count)
        {
            throw new InvalidOperationException($"Numero de linhas incompativel em {nomeCampo}. Destino={destino.Length}, origem={origem.Count}");
        }

        for (var linha = 0; linha < destino.Length; linha++)
        {
            CopiarVetor(destino[linha], origem[linha], $"{nomeCampo}[{linha}]");
        }
    }

    private static void CopiarTensor3(double[][][] destino, IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> origem, string nomeCampo)
    {
        if (destino.Length != origem.Count)
        {
            throw new InvalidOperationException($"Numero de blocos incompativel em {nomeCampo}. Destino={destino.Length}, origem={origem.Count}");
        }

        for (var indice = 0; indice < destino.Length; indice++)
        {
            CopiarMatriz(destino[indice], origem[indice], $"{nomeCampo}[{indice}]");
        }
    }
}
