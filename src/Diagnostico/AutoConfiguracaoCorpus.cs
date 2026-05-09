using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;
using MiniLlmDidatico.Treinamento;

namespace MiniLlmDidatico.Diagnostico;

public sealed class EstatisticasAutoConfiguracaoCorpus
{
    public int TotalEntradas { get; init; }
    public int TotalDominios { get; init; }
    public int EntradasPerguntaResposta { get; init; }
    public int EntradasTextoLivre { get; init; }
    public int TotalTokensBrutos { get; init; }
    public int VocabularioBruto { get; init; }
    public double DiversidadeLexica { get; init; }
    public double TaxaDuplicidade { get; init; }
    public double MediaTokensPorEntrada { get; init; }
    public int Percentil90TokensPorEntrada { get; init; }
    public int Percentil95TokensPorEntrada { get; init; }
    public int MaximoTokensPorEntrada { get; init; }
    public double MediaTokensResposta { get; init; }
    public int Percentil90TokensResposta { get; init; }
}

public sealed class AjusteAutoConfiguracaoCorpus
{
    public string Chave { get; init; } = string.Empty;
    public string ValorSolicitado { get; init; } = string.Empty;
    public string ValorEfetivo { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public sealed class RelatorioAutoConfiguracaoCorpus
{
    public bool AutoConfiguracaoAtiva { get; init; }
    public string PerfilCorpus { get; init; } = string.Empty;
    public string MotivoGeral { get; init; } = string.Empty;
    public EstatisticasAutoConfiguracaoCorpus Estatisticas { get; init; } = new();
    public ConfiguracaoAplicacao ConfiguracaoSolicitada { get; init; } = new();
    public ConfiguracaoAplicacao ConfiguracaoEfetiva { get; init; } = new();
    public List<AjusteAutoConfiguracaoCorpus> Ajustes { get; init; } = new();
}

public static class AutoConfiguradorCorpus
{
    public static RelatorioAutoConfiguracaoCorpus Configurar(
        ConfiguracaoAplicacao configuracaoSolicitada,
        IReadOnlyList<EntradaCorpus> corpus)
    {
        var solicitada = configuracaoSolicitada.Clonar();
        var efetiva = configuracaoSolicitada.Clonar();
        var estatisticas = CalcularEstatisticas(corpus);

        if (!configuracaoSolicitada.AutoConfiguracaoCorpus.Ativar)
        {
            return new RelatorioAutoConfiguracaoCorpus
            {
                AutoConfiguracaoAtiva = false,
                PerfilCorpus = DescreverPerfilCorpus(estatisticas),
                MotivoGeral = "auto-configuracao pelo corpus desativada; mantendo os valores solicitados no arquivo de configuracao",
                Estatisticas = estatisticas,
                ConfiguracaoSolicitada = solicitada,
                ConfiguracaoEfetiva = efetiva
            };
        }

        AplicarHeuristicas(estatisticas, efetiva);
        var ajustes = CriarListaAjustes(solicitada, efetiva, estatisticas);

        return new RelatorioAutoConfiguracaoCorpus
        {
            AutoConfiguracaoAtiva = true,
            PerfilCorpus = DescreverPerfilCorpus(estatisticas),
            MotivoGeral = ajustes.Count == 0
                ? "o corpus ja esta bem atendido pela configuracao solicitada; nenhum ajuste adicional foi necessario"
                : "a configuracao efetiva foi recalculada a partir do tamanho, da repeticao, da estrutura e do comprimento medio do corpus",
            Estatisticas = estatisticas,
            ConfiguracaoSolicitada = solicitada,
            ConfiguracaoEfetiva = efetiva,
            Ajustes = ajustes
        };
    }

    private static EstatisticasAutoConfiguracaoCorpus CalcularEstatisticas(IReadOnlyList<EntradaCorpus> corpus)
    {
        var textosTreino = new List<string>(corpus.Count);
        var tokensPorEntrada = new List<int>(corpus.Count);
        var tokensResposta = new List<int>();
        var vocabulario = new HashSet<string>(StringComparer.Ordinal);
        var entradasNormalizadas = new HashSet<string>(StringComparer.Ordinal);
        var entradasDuplicadas = 0;
        var entradasPerguntaResposta = 0;

        foreach (var entrada in corpus)
        {
            var textoTreino = ConstrutorExemplosTreino.ComporTextoTreino(entrada);
            textosTreino.Add(textoTreino);

            var tokens = TreinadorTokenizadorBpe.PreTokenizar(textoTreino);
            tokensPorEntrada.Add(tokens.Count);

            foreach (var token in tokens)
            {
                vocabulario.Add(token);
            }

            var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(textoTreino);
            if (!entradasNormalizadas.Add(textoNormalizado))
            {
                entradasDuplicadas++;
            }

            if (ConstrutorExemplosTreino.TentarSepararPerguntaResposta(entrada.Texto, out _, out var resposta))
            {
                entradasPerguntaResposta++;
                tokensResposta.Add(TreinadorTokenizadorBpe.PreTokenizar(TreinadorTokenizadorBpe.NormalizarTexto(resposta)).Count);
            }
        }

        var totalTokens = tokensPorEntrada.Sum();
        var totalEntradas = Math.Max(corpus.Count, 1);

        return new EstatisticasAutoConfiguracaoCorpus
        {
            TotalEntradas = corpus.Count,
            TotalDominios = corpus.Select(x => x.Dominio).Distinct(StringComparer.Ordinal).Count(),
            EntradasPerguntaResposta = entradasPerguntaResposta,
            EntradasTextoLivre = Math.Max(0, corpus.Count - entradasPerguntaResposta),
            TotalTokensBrutos = totalTokens,
            VocabularioBruto = vocabulario.Count,
            DiversidadeLexica = totalTokens > 0 ? vocabulario.Count / (double)totalTokens : 0.0,
            TaxaDuplicidade = corpus.Count > 0 ? entradasDuplicadas / (double)totalEntradas : 0.0,
            MediaTokensPorEntrada = corpus.Count > 0 ? tokensPorEntrada.Average() : 0.0,
            Percentil90TokensPorEntrada = CalcularPercentil(tokensPorEntrada, 0.90),
            Percentil95TokensPorEntrada = CalcularPercentil(tokensPorEntrada, 0.95),
            MaximoTokensPorEntrada = tokensPorEntrada.DefaultIfEmpty(0).Max(),
            MediaTokensResposta = tokensResposta.Count > 0 ? tokensResposta.Average() : 0.0,
            Percentil90TokensResposta = CalcularPercentil(tokensResposta, 0.90)
        };
    }

    private static void AplicarHeuristicas(
        EstatisticasAutoConfiguracaoCorpus estatisticas,
        ConfiguracaoAplicacao configuracao)
    {
        configuracao.MaximoMesclasTokenizador = SugerirMaximoMesclas(estatisticas);
        configuracao.MaximoComprimentoSequencia = SugerirComprimentoSequencia(estatisticas);

        var dimensaoEmbedding = SugerirDimensaoEmbedding(estatisticas);
        configuracao.Modelo.DimensaoEmbedding = dimensaoEmbedding;
        configuracao.Modelo.DimensaoFeedForward = dimensaoEmbedding * 3;
        configuracao.Modelo.NumeroCamadas = SugerirNumeroCamadas(estatisticas);
        configuracao.Modelo.NumeroCabecasAtencao = SugerirNumeroCabecas(dimensaoEmbedding);
        configuracao.Modelo.TaxaAprendizado = SugerirTaxaAprendizado(estatisticas, dimensaoEmbedding);
        configuracao.Modelo.Epocas = SugerirEpocas(estatisticas);
        configuracao.Modelo.EmbaralharExemplosPorEpoca = estatisticas.TotalEntradas > 1;
        configuracao.Modelo.UsarParadaAntecipada = true;
        configuracao.Modelo.PacienciaParadaAntecipada = SugerirPacienciaParadaAntecipada(estatisticas);
        configuracao.Modelo.MelhoraMinimaPerda = SugerirMelhoraMinima(estatisticas);
        configuracao.Modelo.MinimoExemplosPorEpocaAdaptado = SugerirMinimoExemplosPorEpoca(estatisticas);
        configuracao.Modelo.MaximoExemplosPorEpoca = SugerirMaximoExemplosPorEpoca(estatisticas);
        configuracao.Modelo.MaximoPassosTreino = 0;

        configuracao.Geracao.MaximoNovosTokens = SugerirMaximoNovosTokens(estatisticas);
        configuracao.Geracao.Temperatura = SugerirTemperatura(estatisticas);
        configuracao.Geracao.TopK = SugerirTopK(estatisticas);
        configuracao.Geracao.TopP = SugerirTopP(estatisticas);
    }

    private static List<AjusteAutoConfiguracaoCorpus> CriarListaAjustes(
        ConfiguracaoAplicacao solicitada,
        ConfiguracaoAplicacao efetiva,
        EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        var ajustes = new List<AjusteAutoConfiguracaoCorpus>();

        AdicionarAjuste(ajustes, "maximoMesclasTokenizador", solicitada.MaximoMesclasTokenizador, efetiva.MaximoMesclasTokenizador,
            $"vocabulario bruto={estatisticas.VocabularioBruto} e total de tokens={estatisticas.TotalTokensBrutos}");
        AdicionarAjuste(ajustes, "maximoComprimentoSequencia", solicitada.MaximoComprimentoSequencia, efetiva.MaximoComprimentoSequencia,
            $"p95 de comprimento do corpus={estatisticas.Percentil95TokensPorEntrada} tokens");
        AdicionarAjuste(ajustes, "modelo.dimensaoEmbedding", solicitada.Modelo.DimensaoEmbedding, efetiva.Modelo.DimensaoEmbedding,
            $"volume efetivo do corpus={estatisticas.TotalTokensBrutos} tokens");
        AdicionarAjuste(ajustes, "modelo.dimensaoFeedForward", solicitada.Modelo.DimensaoFeedForward, efetiva.Modelo.DimensaoFeedForward,
            "mantendo proporcao 3x sobre a dimensao de embedding");
        AdicionarAjuste(ajustes, "modelo.numeroCamadas", solicitada.Modelo.NumeroCamadas, efetiva.Modelo.NumeroCamadas,
            $"profundidade escolhida para {DescreverPerfilCorpus(estatisticas)}");
        AdicionarAjuste(ajustes, "modelo.numeroCabecasAtencao", solicitada.Modelo.NumeroCabecasAtencao, efetiva.Modelo.NumeroCabecasAtencao,
            "garantindo divisibilidade da dimensao de embedding");
        AdicionarAjuste(ajustes, "modelo.taxaAprendizado", solicitada.Modelo.TaxaAprendizado, efetiva.Modelo.TaxaAprendizado,
            "ajustada ao tamanho do modelo e ao volume do corpus");
        AdicionarAjuste(ajustes, "modelo.epocas", solicitada.Modelo.Epocas, efetiva.Modelo.Epocas,
            "ajustada para evitar subtreino em corpus pequeno e excesso em corpus maior");
        AdicionarAjuste(ajustes, "modelo.maximoExemplosPorEpoca", solicitada.Modelo.MaximoExemplosPorEpoca, efetiva.Modelo.MaximoExemplosPorEpoca,
            "limitado apenas quando o corpus fica grande o bastante para exigir subamostragem por epoca");
        AdicionarAjuste(ajustes, "modelo.embaralharExemplosPorEpoca", solicitada.Modelo.EmbaralharExemplosPorEpoca, efetiva.Modelo.EmbaralharExemplosPorEpoca,
            "embaralhamento automatico ativado quando ha mais de um exemplo");
        AdicionarAjuste(ajustes, "modelo.usarParadaAntecipada", solicitada.Modelo.UsarParadaAntecipada, efetiva.Modelo.UsarParadaAntecipada,
            "parada antecipada ligada para reduzir sobreajuste em corpus pequenos e repetitivos");
        AdicionarAjuste(ajustes, "modelo.pacienciaParadaAntecipada", solicitada.Modelo.PacienciaParadaAntecipada, efetiva.Modelo.PacienciaParadaAntecipada,
            "paciencia calibrada pela escala do corpus");
        AdicionarAjuste(ajustes, "modelo.melhoraMinimaPerda", solicitada.Modelo.MelhoraMinimaPerda, efetiva.Modelo.MelhoraMinimaPerda,
            "sensibilidade da parada antecipada ajustada ao regime de treino");
        AdicionarAjuste(ajustes, "modelo.minimoExemplosPorEpocaAdaptado", solicitada.Modelo.MinimoExemplosPorEpocaAdaptado, efetiva.Modelo.MinimoExemplosPorEpocaAdaptado,
            "piso do planejador alinhado ao tamanho real do corpus");
        AdicionarAjuste(ajustes, "geracao.maximoNovosTokens", solicitada.Geracao.MaximoNovosTokens, efetiva.Geracao.MaximoNovosTokens,
            $"p90 de comprimento das respostas={estatisticas.Percentil90TokensResposta} tokens");
        AdicionarAjuste(ajustes, "geracao.temperatura", solicitada.Geracao.Temperatura, efetiva.Geracao.Temperatura,
            "criatividade ajustada a repeticao e estrutura do corpus");
        AdicionarAjuste(ajustes, "geracao.topK", solicitada.Geracao.TopK, efetiva.Geracao.TopK,
            "leque de amostragem ajustado ao risco de repeticao");
        AdicionarAjuste(ajustes, "geracao.topP", solicitada.Geracao.TopP, efetiva.Geracao.TopP,
            "massa de probabilidade calibrada para manter coerencia");

        return ajustes;
    }

    private static void AdicionarAjuste<T>(
        List<AjusteAutoConfiguracaoCorpus> ajustes,
        string chave,
        T valorSolicitado,
        T valorEfetivo,
        string motivo)
    {
        if (EqualityComparer<T>.Default.Equals(valorSolicitado, valorEfetivo))
        {
            return;
        }

        ajustes.Add(new AjusteAutoConfiguracaoCorpus
        {
            Chave = chave,
            ValorSolicitado = FormatarValor(valorSolicitado),
            ValorEfetivo = FormatarValor(valorEfetivo),
            Motivo = motivo
        });
    }

    private static string DescreverPerfilCorpus(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        var proporcaoPerguntaResposta = CalcularProporcaoPerguntaResposta(estatisticas);

        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return "corpus amplo conversacional ruidoso";
        }

        if (estatisticas.TotalTokensBrutos <= 250)
        {
            return proporcaoPerguntaResposta >= 0.5
                ? "micro corpus estruturado"
                : "micro corpus livre";
        }

        if (estatisticas.TotalTokensBrutos <= 1200)
        {
            return proporcaoPerguntaResposta >= 0.5
                ? "corpus pequeno orientado a pergunta-resposta"
                : "corpus pequeno misto";
        }

        if (estatisticas.TotalTokensBrutos <= 5000)
        {
            return proporcaoPerguntaResposta >= 0.5
                ? "corpus medio estruturado"
                : "corpus medio variado";
        }

        return proporcaoPerguntaResposta >= 0.5
            ? "corpus amplo estruturado"
            : "corpus amplo variado";
    }

    private static int SugerirMaximoMesclas(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return estatisticas.TotalTokensBrutos >= 350_000 ? 96 : 80;
        }

        var baseCalculada = estatisticas.TotalTokensBrutos switch
        {
            <= 120 => 16,
            <= 300 => 24,
            <= 700 => 32,
            <= 1500 => 48,
            <= 4000 => 64,
            <= 8000 => 96,
            _ => 128
        };

        if (estatisticas.VocabularioBruto >= 180)
        {
            baseCalculada += 16;
        }

        var limitePeloVocabulario = Math.Max(16, estatisticas.VocabularioBruto * 2);
        return ArredondarParaMultiplo(Math.Clamp(baseCalculada, 16, Math.Min(160, limitePeloVocabulario)), 8);
    }

    private static int SugerirComprimentoSequencia(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        var baseCalculada = (int)Math.Ceiling(estatisticas.Percentil95TokensPorEntrada * 1.35 + 12.0);
        baseCalculada = Math.Max(baseCalculada, estatisticas.Percentil90TokensPorEntrada + 10);
        baseCalculada = Math.Max(baseCalculada, estatisticas.Percentil90TokensResposta + 18);

        if (estatisticas.TotalTokensBrutos <= 600)
        {
            baseCalculada = Math.Min(baseCalculada, 96);
        }

        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            baseCalculada = Math.Min(baseCalculada, 64);
        }

        return ArredondarParaMultiplo(Math.Clamp(baseCalculada, 32, 256), 8);
    }

    private static int SugerirDimensaoEmbedding(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return estatisticas.TotalTokensBrutos >= 500_000 ? 64 : 48;
        }

        return estatisticas.TotalTokensBrutos switch
        {
            <= 250 => 24,
            <= 1200 => 32,
            <= 4000 => 48,
            <= 12000 => 64,
            _ => 96
        };
    }

    private static int SugerirNumeroCamadas(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 2;
        }

        return estatisticas.TotalTokensBrutos switch
        {
            <= 350 => 1,
            <= 5000 => 2,
            <= 15000 => 3,
            _ => 4
        };
    }

    private static int SugerirNumeroCabecas(int dimensaoEmbedding)
    {
        return dimensaoEmbedding switch
        {
            <= 32 => 4,
            <= 64 => 4,
            _ => 6
        };
    }

    private static double SugerirTaxaAprendizado(EstatisticasAutoConfiguracaoCorpus estatisticas, int dimensaoEmbedding)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 0.0065;
        }

        var baseCalculada = dimensaoEmbedding switch
        {
            <= 24 => 0.0105,
            <= 32 => 0.0085,
            <= 48 => 0.0065,
            <= 64 => 0.0050,
            _ => 0.0035
        };

        if (estatisticas.TotalTokensBrutos <= 300)
        {
            baseCalculada += 0.0008;
        }
        else if (estatisticas.TotalTokensBrutos >= 6000)
        {
            baseCalculada -= 0.0004;
        }

        return Math.Clamp(baseCalculada, 0.0030, 0.0120);
    }

    private static int SugerirEpocas(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 90;
        }

        return estatisticas.TotalEntradas switch
        {
            <= 24 => 900,
            <= 80 => 700,
            <= 200 => 500,
            <= 800 => 250,
            _ => 140
        };
    }

    private static int SugerirPacienciaParadaAntecipada(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 16;
        }

        return estatisticas.TotalEntradas switch
        {
            <= 24 => 28,
            <= 80 => 22,
            <= 200 => 18,
            <= 800 => 14,
            _ => 10
        };
    }

    private static double SugerirMelhoraMinima(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 0.00035;
        }

        return estatisticas.TotalTokensBrutos switch
        {
            <= 300 => 0.00010,
            <= 1500 => 0.00020,
            <= 5000 => 0.00035,
            _ => 0.00050
        };
    }

    private static int SugerirMinimoExemplosPorEpoca(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 256;
        }

        var baseCalculada = Math.Max(16, estatisticas.TotalEntradas / 4);
        return ArredondarParaMultiplo(Math.Clamp(baseCalculada, 16, 512), 8);
    }

    private static int SugerirMaximoExemplosPorEpoca(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 4096;
        }

        if (estatisticas.TotalEntradas <= 1024)
        {
            return 0;
        }

        if (estatisticas.TotalEntradas <= 4096)
        {
            return 1024;
        }

        return 2048;
    }

    private static int SugerirMaximoNovosTokens(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        var baseCalculada = estatisticas.EntradasPerguntaResposta > 0
            ? estatisticas.Percentil90TokensResposta + 4
            : estatisticas.Percentil90TokensPorEntrada / 2 + 6;

        return ArredondarParaMultiplo(Math.Clamp(baseCalculada, 12, 64), 4);
    }

    private static double SugerirTemperatura(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        var proporcaoPerguntaResposta = CalcularProporcaoPerguntaResposta(estatisticas);

        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 0.64;
        }

        if (estatisticas.TotalTokensBrutos <= 600 || estatisticas.TaxaDuplicidade >= 0.10)
        {
            return 0.76;
        }

        if (proporcaoPerguntaResposta >= 0.55)
        {
            return 0.80;
        }

        if (estatisticas.DiversidadeLexica >= 0.32)
        {
            return 0.88;
        }

        return 0.84;
    }

    private static int SugerirTopK(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 4;
        }

        if (estatisticas.TotalTokensBrutos <= 600 || estatisticas.TaxaDuplicidade >= 0.10)
        {
            return 6;
        }

        if (estatisticas.DiversidadeLexica >= 0.32)
        {
            return 12;
        }

        return 8;
    }

    private static double SugerirTopP(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        if (EhCorpusConversacionalRuidoso(estatisticas))
        {
            return 0.74;
        }

        if (estatisticas.TotalTokensBrutos <= 600 || estatisticas.TaxaDuplicidade >= 0.10)
        {
            return 0.82;
        }

        if (estatisticas.DiversidadeLexica >= 0.32)
        {
            return 0.92;
        }

        return 0.88;
    }

    private static bool EhCorpusConversacionalRuidoso(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        return estatisticas.TotalEntradas >= 5_000 &&
               CalcularProporcaoPerguntaResposta(estatisticas) < 0.10 &&
               estatisticas.TaxaDuplicidade >= 0.20 &&
               estatisticas.DiversidadeLexica <= 0.12 &&
               estatisticas.MediaTokensPorEntrada <= 24.0;
    }

    private static double CalcularProporcaoPerguntaResposta(EstatisticasAutoConfiguracaoCorpus estatisticas)
    {
        return estatisticas.TotalEntradas > 0
            ? estatisticas.EntradasPerguntaResposta / (double)estatisticas.TotalEntradas
            : 0.0;
    }

    private static int CalcularPercentil(IReadOnlyList<int> valores, double percentil)
    {
        if (valores.Count == 0)
        {
            return 0;
        }

        var ordenados = valores.OrderBy(x => x).ToList();
        var indice = (int)Math.Ceiling(percentil * ordenados.Count) - 1;
        indice = Math.Clamp(indice, 0, ordenados.Count - 1);
        return ordenados[indice];
    }

    private static int ArredondarParaMultiplo(int valor, int multiplo)
    {
        if (multiplo <= 1)
        {
            return valor;
        }

        return ((valor + multiplo - 1) / multiplo) * multiplo;
    }

    private static string FormatarValor<T>(T valor)
    {
        return valor switch
        {
            null => string.Empty,
            double numeroDouble => numeroDouble.ToString("0.######"),
            float numeroFloat => numeroFloat.ToString("0.######"),
            bool booleano => booleano ? "true" : "false",
            _ => valor.ToString() ?? string.Empty
        };
    }
}
