using MiniLlmDidatico.Numerica;

namespace MiniLlmDidatico.Modelo;

public sealed partial class MiniTransformador
{
    public ResultadoPassoTreino TreinarPasso(IReadOnlyList<int> idsEntrada, IReadOnlyList<int> idsAlvo, IReadOnlyList<double>? pesosPerda = null)
    {
        var cache = PassagemDireta(idsEntrada);
        var comprimento = idsEntrada.Count;
        var perda = 0.0;
        var somaPesosPerda = 0.0;

        var gradienteEmbeddingsToken = _executorNumerico.Zeros(_hiperParametros.TamanhoVocabulario, _hiperParametros.DimensaoEmbedding);
        var gradienteEmbeddingsPosicionais = _executorNumerico.Zeros(_hiperParametros.MaximoComprimentoSequencia, _hiperParametros.DimensaoEmbedding);
        var gradientesBlocos = _blocos.Select(_ => new GradientesBlocoDecoder(_hiperParametros, _executorNumerico)).ToArray();
        var gradienteNormalizacaoFinal = new GradientesLayerNorm(_hiperParametros.DimensaoEmbedding);
        var gradientePesosSaida = _executorNumerico.Zeros(_hiperParametros.DimensaoEmbedding, _hiperParametros.TamanhoVocabulario);
        var gradienteViesSaida = _executorNumerico.Zeros(_hiperParametros.TamanhoVocabulario);
        var gradienteEstadoFinal = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var t = 0; t < comprimento; t++)
        {
            var pesoPerda = pesosPerda?[t] ?? 1.0;
            if (pesoPerda <= 0.0)
            {
                continue;
            }

            var probabilidades = cache.Probabilidades[t];
            perda += _executorNumerico.PerdaEntropiaCruzada(probabilidades, idsAlvo[t]) * pesoPerda;
            somaPesosPerda += pesoPerda;

            var gradienteLogits = _executorNumerico.ClonarVetor(probabilidades);
            gradienteLogits[idsAlvo[t]] -= 1.0;
            for (var indice = 0; indice < gradienteLogits.Length; indice++)
            {
                gradienteLogits[indice] *= pesoPerda;
            }

            _executorNumerico.SomarProdutoExternoEm(gradientePesosSaida, cache.EstadoFinal[t], gradienteLogits);
            _executorNumerico.SomarEm(gradienteViesSaida, gradienteLogits);
            _executorNumerico.SomarEm(gradienteEstadoFinal[t], _executorNumerico.RetropropagarParaEntrada(gradienteLogits, _pesosSaida));
        }

        var gradienteEstadoAtual = RetropropagarLayerNorm(
            cache.NormalizacaoFinal,
            gradienteEstadoFinal,
            _normalizacaoFinal.Gama,
            gradienteNormalizacaoFinal);

        for (var indiceBloco = _blocos.Length - 1; indiceBloco >= 0; indiceBloco--)
        {
            gradienteEstadoAtual = RetropropagarBloco(
                _blocos[indiceBloco],
                cache.Blocos[indiceBloco],
                gradienteEstadoAtual,
                gradientesBlocos[indiceBloco]);
        }

        for (var t = 0; t < comprimento; t++)
        {
            var idToken = idsEntrada[t];
            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                gradienteEmbeddingsToken[idToken][dim] += gradienteEstadoAtual[t][dim];
                gradienteEmbeddingsPosicionais[t][dim] += gradienteEstadoAtual[t][dim];
            }
        }

        AplicarGradiente(_embeddingsToken, gradienteEmbeddingsToken, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(_embeddingsPosicionais, gradienteEmbeddingsPosicionais, _hiperParametros.TaxaAprendizado);

        for (var indiceBloco = 0; indiceBloco < _blocos.Length; indiceBloco++)
        {
            AplicarGradientesBloco(_blocos[indiceBloco], gradientesBlocos[indiceBloco]);
        }

        AplicarGradiente(_normalizacaoFinal.Gama, gradienteNormalizacaoFinal.Gama, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(_normalizacaoFinal.Beta, gradienteNormalizacaoFinal.Beta, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(_pesosSaida, gradientePesosSaida, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(_viesSaida, gradienteViesSaida, _hiperParametros.TaxaAprendizado);

        return new ResultadoPassoTreino
        {
            Perda = perda / System.Math.Max(somaPesosPerda, 1.0),
            Cache = cache
        };
    }

    private double[][] RetropropagarBloco(
        BlocoDecoder bloco,
        CacheBlocoTransformador cacheBloco,
        double[][] gradienteSaida,
        GradientesBlocoDecoder gradientes)
    {
        var comprimento = gradienteSaida.Length;
        var gradienteEntrada = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
        var gradienteEstadoPosAtencao = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);
        var gradienteSaidaNormalizacaoFeedForward = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var t = 0; t < comprimento; t++)
        {
            _executorNumerico.SomarEm(gradienteEstadoPosAtencao[t], gradienteSaida[t]);

            _executorNumerico.SomarProdutoExternoEm(gradientes.PesosFeedForward2, cacheBloco.OcultaFeedForward[t], gradienteSaida[t]);
            _executorNumerico.SomarEm(gradientes.ViesFeedForward2, gradienteSaida[t]);

            var gradienteOcultaFeedForward = _executorNumerico.RetropropagarParaEntrada(gradienteSaida[t], bloco.PesosFeedForward2);
            var gradientePreAtivacaoFeedForward = _executorNumerico.ReluReversa(cacheBloco.PreAtivacaoFeedForward[t], gradienteOcultaFeedForward);

            _executorNumerico.SomarProdutoExternoEm(gradientes.PesosFeedForward1, cacheBloco.NormalizacaoPreFeedForward.Saida[t], gradientePreAtivacaoFeedForward);
            _executorNumerico.SomarEm(gradientes.ViesFeedForward1, gradientePreAtivacaoFeedForward);
            _executorNumerico.SomarEm(
                gradienteSaidaNormalizacaoFeedForward[t],
                _executorNumerico.RetropropagarParaEntrada(gradientePreAtivacaoFeedForward, bloco.PesosFeedForward1));
        }

        var gradienteEstadoPosAtencaoViaNormalizacao = RetropropagarLayerNorm(
            cacheBloco.NormalizacaoPreFeedForward,
            gradienteSaidaNormalizacaoFeedForward,
            bloco.NormalizacaoPreFeedForward.Gama,
            gradientes.NormalizacaoPreFeedForward);

        for (var t = 0; t < comprimento; t++)
        {
            _executorNumerico.SomarEm(gradienteEstadoPosAtencao[t], gradienteEstadoPosAtencaoViaNormalizacao[t]);
            _executorNumerico.SomarEm(gradienteEntrada[t], gradienteEstadoPosAtencao[t]);
        }

        var gradienteContextoConcatenado = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var t = 0; t < comprimento; t++)
        {
            _executorNumerico.SomarProdutoExternoEm(gradientes.PesosProjecaoSaidaAtencao, cacheBloco.ContextoConcatenado[t], gradienteEstadoPosAtencao[t]);
            _executorNumerico.SomarEm(gradientes.ViesProjecaoSaidaAtencao, gradienteEstadoPosAtencao[t]);
            _executorNumerico.SomarEm(
                gradienteContextoConcatenado[t],
                _executorNumerico.RetropropagarParaEntrada(gradienteEstadoPosAtencao[t], bloco.PesosProjecaoSaidaAtencao));
        }

        var gradienteSaidaNormalizacaoAtencao = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var cabeca = 0; cabeca < _hiperParametros.NumeroCabecasAtencao; cabeca++)
        {
            var cacheCabeca = cacheBloco.CabecasAtencao[cabeca];
            var gradienteContextoCabeca = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
            var gradienteConsultas = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
            var gradienteChaves = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
            var gradienteValores = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoCabeca);
            var escala = System.Math.Sqrt(_hiperParametros.DimensaoCabeca);
            var deslocamentoCabeca = cabeca * _hiperParametros.DimensaoCabeca;

            for (var t = 0; t < comprimento; t++)
            {
                for (var dim = 0; dim < _hiperParametros.DimensaoCabeca; dim++)
                {
                    gradienteContextoCabeca[t][dim] = gradienteContextoConcatenado[t][deslocamentoCabeca + dim];
                }
            }

            for (var t = 0; t < comprimento; t++)
            {
                var gradienteProbabilidadesAtencao = new double[comprimento];

                for (var j = 0; j <= t; j++)
                {
                    gradienteProbabilidadesAtencao[j] = _executorNumerico.ProdutoEscalar(gradienteContextoCabeca[t], cacheCabeca.Valores[j]);

                    for (var dim = 0; dim < _hiperParametros.DimensaoCabeca; dim++)
                    {
                        gradienteValores[j][dim] += cacheCabeca.Probabilidades[t][j] * gradienteContextoCabeca[t][dim];
                    }
                }

                var somaPonderada = 0.0;
                for (var j = 0; j <= t; j++)
                {
                    somaPonderada += gradienteProbabilidadesAtencao[j] * cacheCabeca.Probabilidades[t][j];
                }

                for (var j = 0; j <= t; j++)
                {
                    var gradienteEscore = cacheCabeca.Probabilidades[t][j] * (gradienteProbabilidadesAtencao[j] - somaPonderada) / escala;

                    for (var dim = 0; dim < _hiperParametros.DimensaoCabeca; dim++)
                    {
                        gradienteConsultas[t][dim] += gradienteEscore * cacheCabeca.Chaves[j][dim];
                        gradienteChaves[j][dim] += gradienteEscore * cacheCabeca.Consultas[t][dim];
                    }
                }
            }

            for (var t = 0; t < comprimento; t++)
            {
                _executorNumerico.SomarProdutoExternoEm(gradientes.PesosConsultaPorCabeca[cabeca], cacheBloco.NormalizacaoPreAtencao.Saida[t], gradienteConsultas[t]);
                _executorNumerico.SomarProdutoExternoEm(gradientes.PesosChavePorCabeca[cabeca], cacheBloco.NormalizacaoPreAtencao.Saida[t], gradienteChaves[t]);
                _executorNumerico.SomarProdutoExternoEm(gradientes.PesosValorPorCabeca[cabeca], cacheBloco.NormalizacaoPreAtencao.Saida[t], gradienteValores[t]);

                _executorNumerico.SomarEm(gradienteSaidaNormalizacaoAtencao[t], _executorNumerico.RetropropagarParaEntrada(gradienteConsultas[t], bloco.PesosConsultaPorCabeca[cabeca]));
                _executorNumerico.SomarEm(gradienteSaidaNormalizacaoAtencao[t], _executorNumerico.RetropropagarParaEntrada(gradienteChaves[t], bloco.PesosChavePorCabeca[cabeca]));
                _executorNumerico.SomarEm(gradienteSaidaNormalizacaoAtencao[t], _executorNumerico.RetropropagarParaEntrada(gradienteValores[t], bloco.PesosValorPorCabeca[cabeca]));
            }
        }

        var gradienteEntradaAtencao = RetropropagarLayerNorm(
            cacheBloco.NormalizacaoPreAtencao,
            gradienteSaidaNormalizacaoAtencao,
            bloco.NormalizacaoPreAtencao.Gama,
            gradientes.NormalizacaoPreAtencao);

        for (var t = 0; t < comprimento; t++)
        {
            _executorNumerico.SomarEm(gradienteEntrada[t], gradienteEntradaAtencao[t]);
        }

        return gradienteEntrada;
    }

    private double[][] RetropropagarLayerNorm(
        CacheLayerNorm cache,
        double[][] gradienteSaida,
        double[] gama,
        GradientesLayerNorm gradientes)
    {
        var comprimento = gradienteSaida.Length;
        var gradienteEntrada = _executorNumerico.Zeros(comprimento, _hiperParametros.DimensaoEmbedding);

        for (var t = 0; t < comprimento; t++)
        {
            var gradienteNormalizado = new double[_hiperParametros.DimensaoEmbedding];
            var mediaGradienteNormalizado = 0.0;
            var mediaGradienteNormalizadoVezesNormalizado = 0.0;

            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                gradientes.Gama[dim] += gradienteSaida[t][dim] * cache.Normalizado[t][dim];
                gradientes.Beta[dim] += gradienteSaida[t][dim];

                gradienteNormalizado[dim] = gradienteSaida[t][dim] * gama[dim];
                mediaGradienteNormalizado += gradienteNormalizado[dim];
                mediaGradienteNormalizadoVezesNormalizado += gradienteNormalizado[dim] * cache.Normalizado[t][dim];
            }

            mediaGradienteNormalizado /= _hiperParametros.DimensaoEmbedding;
            mediaGradienteNormalizadoVezesNormalizado /= _hiperParametros.DimensaoEmbedding;

            for (var dim = 0; dim < _hiperParametros.DimensaoEmbedding; dim++)
            {
                gradienteEntrada[t][dim] =
                    cache.InversosDesvioPadrao[t] *
                    (gradienteNormalizado[dim]
                     - mediaGradienteNormalizado
                     - cache.Normalizado[t][dim] * mediaGradienteNormalizadoVezesNormalizado);
            }
        }

        return gradienteEntrada;
    }

    private void AplicarGradientesBloco(BlocoDecoder bloco, GradientesBlocoDecoder gradientes)
    {
        for (var cabeca = 0; cabeca < _hiperParametros.NumeroCabecasAtencao; cabeca++)
        {
            AplicarGradiente(bloco.PesosConsultaPorCabeca[cabeca], gradientes.PesosConsultaPorCabeca[cabeca], _hiperParametros.TaxaAprendizado);
            AplicarGradiente(bloco.PesosChavePorCabeca[cabeca], gradientes.PesosChavePorCabeca[cabeca], _hiperParametros.TaxaAprendizado);
            AplicarGradiente(bloco.PesosValorPorCabeca[cabeca], gradientes.PesosValorPorCabeca[cabeca], _hiperParametros.TaxaAprendizado);
        }

        AplicarGradiente(bloco.NormalizacaoPreAtencao.Gama, gradientes.NormalizacaoPreAtencao.Gama, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.NormalizacaoPreAtencao.Beta, gradientes.NormalizacaoPreAtencao.Beta, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.PesosProjecaoSaidaAtencao, gradientes.PesosProjecaoSaidaAtencao, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.ViesProjecaoSaidaAtencao, gradientes.ViesProjecaoSaidaAtencao, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.NormalizacaoPreFeedForward.Gama, gradientes.NormalizacaoPreFeedForward.Gama, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.NormalizacaoPreFeedForward.Beta, gradientes.NormalizacaoPreFeedForward.Beta, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.PesosFeedForward1, gradientes.PesosFeedForward1, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.ViesFeedForward1, gradientes.ViesFeedForward1, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.PesosFeedForward2, gradientes.PesosFeedForward2, _hiperParametros.TaxaAprendizado);
        AplicarGradiente(bloco.ViesFeedForward2, gradientes.ViesFeedForward2, _hiperParametros.TaxaAprendizado);
    }
}
