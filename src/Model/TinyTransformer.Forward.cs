using MiniLlmDidatico.Numerica;

namespace MiniLlmDidatico.Modelo;

public sealed partial class MiniTransformador
{
    public CachePassagemDireta PassagemDireta(IReadOnlyList<int> idsEntrada)
    {
        if (idsEntrada.Count > _hiperParametros.MaximoComprimentoSequencia)
        {
            throw new InvalidOperationException(
                $"Sequencia maior que o limite do modelo. Tamanho={idsEntrada.Count}, maximo={_hiperParametros.MaximoComprimentoSequencia}");
        }

        var comprimento = idsEntrada.Count;
        var embeddingEntrada = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var t = 0; t < comprimento; t++)
        {
            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                embeddingEntrada[t][dim] = _embeddingsToken[idsEntrada[t]][dim] + _embeddingsPosicionais[t][dim];
            }
        }

        var cachesBlocos = new List<CacheBlocoTransformador>(_blocos.Length);
        var estadoAtual = embeddingEntrada;

        for (var indiceBloco = 0; indiceBloco < _blocos.Length; indiceBloco++)
        {
            var bloco = _blocos[indiceBloco];
            var normalizacaoPreAtencao = AplicarLayerNorm(estadoAtual, bloco.NormalizacaoPreAtencao.Gama, bloco.NormalizacaoPreAtencao.Beta);
            var cachesCabecas = new List<CacheCabecaAtencao>(_hiperParametros.NumeroCabecasAtencao);
            var contextoConcatenado = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

            for (var cabeca = 0; cabeca < _hiperParametros.NumeroCabecasAtencao; cabeca++)
            {
                var cacheCabeca = ExecutarCabecaAtencao(normalizacaoPreAtencao.Saida, bloco, cabeca);
                cachesCabecas.Add(cacheCabeca);
                CopiarContextoCabecaParaContextoConcatenado(cacheCabeca.Contexto, cabeca, contextoConcatenado);
            }

            var projecaoSaidaAtencao = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
            var estadoPosAtencao = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
            var preAtivacaoFeedForward = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoFeedForward);
            var ocultaFeedForward = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoFeedForward);
            var saidaFeedForward = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
            var saidaBloco = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

            for (var t = 0; t < comprimento; t++)
            {
                projecaoSaidaAtencao[t] = _executorNumerico.MatrizVezesVetor(contextoConcatenado[t], bloco.PesosProjecaoSaidaAtencao, bloco.ViesProjecaoSaidaAtencao);
                estadoPosAtencao[t] = _executorNumerico.Somar(estadoAtual[t], projecaoSaidaAtencao[t]);
            }

            var normalizacaoPreFeedForward = AplicarLayerNorm(estadoPosAtencao, bloco.NormalizacaoPreFeedForward.Gama, bloco.NormalizacaoPreFeedForward.Beta);

            for (var t = 0; t < comprimento; t++)
            {
                preAtivacaoFeedForward[t] = _executorNumerico.MatrizVezesVetor(normalizacaoPreFeedForward.Saida[t], bloco.PesosFeedForward1, bloco.ViesFeedForward1);
                ocultaFeedForward[t] = _executorNumerico.Relu(preAtivacaoFeedForward[t]);
                saidaFeedForward[t] = _executorNumerico.MatrizVezesVetor(ocultaFeedForward[t], bloco.PesosFeedForward2, bloco.ViesFeedForward2);
                saidaBloco[t] = _executorNumerico.Somar(estadoPosAtencao[t], saidaFeedForward[t]);
            }

            cachesBlocos.Add(new CacheBlocoTransformador
            {
                Entrada = estadoAtual,
                NormalizacaoPreAtencao = normalizacaoPreAtencao,
                CabecasAtencao = cachesCabecas,
                ContextoConcatenado = contextoConcatenado,
                ProjecaoSaidaAtencao = projecaoSaidaAtencao,
                EstadoPosAtencao = estadoPosAtencao,
                NormalizacaoPreFeedForward = normalizacaoPreFeedForward,
                PreAtivacaoFeedForward = preAtivacaoFeedForward,
                OcultaFeedForward = ocultaFeedForward,
                SaidaFeedForward = saidaFeedForward,
                Saida = saidaBloco
            });

            estadoAtual = saidaBloco;
        }

        var normalizacaoFinal = AplicarLayerNorm(estadoAtual, _normalizacaoFinal.Gama, _normalizacaoFinal.Beta);
        var logits = _executorNumerico.Zeros(comprimento, _hiperParametros.TamanhoVocabulario);
        var probabilidades = _executorNumerico.Zeros(comprimento, _hiperParametros.TamanhoVocabulario);

        for (var t = 0; t < comprimento; t++)
        {
            logits[t] = _executorNumerico.MatrizVezesVetor(normalizacaoFinal.Saida[t], _pesosSaida, _viesSaida);
            probabilidades[t] = _executorNumerico.Softmax(logits[t]);
        }

        return new CachePassagemDireta
        {
            IdsEntrada = idsEntrada.ToArray(),
            EmbeddingEntrada = embeddingEntrada,
            Blocos = cachesBlocos,
            NormalizacaoFinal = normalizacaoFinal,
            EstadoFinal = normalizacaoFinal.Saida,
            Logits = logits,
            Probabilidades = probabilidades
        };
    }

    private CacheCabecaAtencao ExecutarCabecaAtencao(double[][] entradaNormalizada, BlocoDecoder bloco, int indiceCabeca)
    {
        var comprimento = entradaNormalizada.Length;
        var consultas = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
        var chaves = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
        var valores = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
        var escores = _executorNumerico.Zeros(comprimento, comprimento);
        var probabilidades = _executorNumerico.Zeros(comprimento, comprimento);
        var contexto = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
        var escala = System.Math.Sqrt(_hiperParametros.DimensaoCabeca);

        for (var t = 0; t < comprimento; t++)
        {
            consultas[t] = _executorNumerico.MatrizVezesVetor(entradaNormalizada[t], bloco.PesosConsultaPorCabeca[indiceCabeca]);
            chaves[t] = _executorNumerico.MatrizVezesVetor(entradaNormalizada[t], bloco.PesosChavePorCabeca[indiceCabeca]);
            valores[t] = _executorNumerico.MatrizVezesVetor(entradaNormalizada[t], bloco.PesosValorPorCabeca[indiceCabeca]);
        }

        for (var t = 0; t < comprimento; t++)
        {
            var escoresMascarados = new double[comprimento];
            for (var j = 0; j < comprimento; j++)
            {
                if (j <= t)
                {
                    escores[t][j] = _executorNumerico.ProdutoEscalar(consultas[t], chaves[j]) / escala;
                    escoresMascarados[j] = escores[t][j];
                }
                else
                {
                    escores[t][j] = double.NegativeInfinity;
                    escoresMascarados[j] = -1e9;
                }
            }

            probabilidades[t] = _executorNumerico.Softmax(escoresMascarados);

            for (var j = 0; j <= t; j++)
            {
                for (var dim = 0; dim < _hiperParametros.DimensaoCabeca; dim++)
                {
                    contexto[t][dim] += probabilidades[t][j] * valores[j][dim];
                }
            }
        }

        return new CacheCabecaAtencao
        {
            Consultas = consultas,
            Chaves = chaves,
            Valores = valores,
            Escores = escores,
            Probabilidades = probabilidades,
            Contexto = contexto
        };
    }

    private CacheLayerNorm AplicarLayerNorm(double[][] entrada, double[] gama, double[] beta)
    {
        var comprimento = entrada.Length;
        var normalizado = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
        var saida = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
        var medias = new double[comprimento];
        var variancias = new double[comprimento];
        var inversosDesvioPadrao = new double[comprimento];

        for (var t = 0; t < comprimento; t++)
        {
            var soma = 0.0;
            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                soma += entrada[t][dim];
            }

            var media = soma / _hiperParametros.DimensaoEmbedding;
            medias[t] = media;

            var somaQuadratica = 0.0;
            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                var diferenca = entrada[t][dim] - media;
                somaQuadratica += diferenca * diferenca;
            }

            var variancia = somaQuadratica / _hiperParametros.DimensaoEmbedding;
            variancias[t] = variancia;
            var inversoDesvioPadrao = 1.0 / System.Math.Sqrt(variancia + _hiperParametros.EpsilonLayerNorm);
            inversosDesvioPadrao[t] = inversoDesvioPadrao;

            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                normalizado[t][dim] = (entrada[t][dim] - media) * inversoDesvioPadrao;
                saida[t][dim] = normalizado[t][dim] * gama[dim] + beta[dim];
            }
        }

        return new CacheLayerNorm
        {
            Entrada = entrada,
            Normalizado = normalizado,
            Saida = saida,
            Medias = medias,
            Variancias = variancias,
            InversosDesvioPadrao = inversosDesvioPadrao
        };
    }

    private static void CopiarContextoCabecaParaContextoConcatenado(double[][] contextoCabeca, int indiceCabeca, double[][] contextoConcatenado)
    {
        var deslocamento = indiceCabeca * contextoCabeca[0].Length;

        for (var t = 0; t < contextoCabeca.Length; t++)
        {
            for (var dim = 0; dim < contextoCabeca[t].Length; dim++)
            {
                contextoConcatenado[t][deslocamento + dim] = contextoCabeca[t][dim];
            }
        }
    }
}
