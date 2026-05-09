namespace MiniLlmDidatico.Configuracao;

public sealed class ConfiguracaoAplicacao
{
    public int SementeAleatoria { get; set; } = 42;
    public int MaximoMesclasTokenizador { get; set; } = 60;
    public int MaximoComprimentoSequencia { get; set; } = 64;
    public ConfiguracaoModelo Modelo { get; set; } = new();
    public ConfiguracaoGeracao Geracao { get; set; } = new();
    public ConfiguracaoExecucao Execucao { get; set; } = new();
    public ConfiguracaoAutoCorpus AutoConfiguracaoCorpus { get; set; } = new();
    public ConfiguracaoCognicao Cognicao { get; set; } = new();

    public ConfiguracaoAplicacao Clonar()
    {
        return new ConfiguracaoAplicacao
        {
            SementeAleatoria = SementeAleatoria,
            MaximoMesclasTokenizador = MaximoMesclasTokenizador,
            MaximoComprimentoSequencia = MaximoComprimentoSequencia,
            Modelo = Modelo.Clonar(),
            Geracao = Geracao.Clonar(),
            Execucao = Execucao.Clonar(),
            AutoConfiguracaoCorpus = AutoConfiguracaoCorpus.Clonar(),
            Cognicao = Cognicao.Clonar()
        };
    }
}

public sealed class ConfiguracaoModelo
{
    private const int PacienciaParadaAntecipadaPadrao = 20;

    public int DimensaoEmbedding { get; set; } = 24;
    public int DimensaoFeedForward { get; set; } = 48;
    public int NumeroCamadas { get; set; } = 1;
    public int NumeroCabecasAtencao { get; set; } = 4;
    public double EpsilonLayerNorm { get; set; } = 1e-5;
    public double TaxaAprendizado { get; set; } = 0.015;
    public int Epocas { get; set; } = 650;
    public int MaximoExemplosPorEpoca { get; set; }
    public int MaximoPassosTreino { get; set; }
    public bool EmbaralharExemplosPorEpoca { get; set; } = true;
    public bool? UsarParadaAntecipada { get; set; }
    public int PacienciaParadaAntecipada { get; set; }
    public double MelhoraMinimaPerda { get; set; } = 0.0005;
    public bool TreinoAdaptativoAutomatico { get; set; } = true;
    public int TempoMaximoTreinoMinutos { get; set; } = 20;
    public int PassosAmostraPlanejamento { get; set; } = 12;
    public int MinimoExemplosPorEpocaAdaptado { get; set; } = 256;

    public ConfiguracaoModelo Clonar()
    {
        return new ConfiguracaoModelo
        {
            DimensaoEmbedding = DimensaoEmbedding,
            DimensaoFeedForward = DimensaoFeedForward,
            NumeroCamadas = NumeroCamadas,
            NumeroCabecasAtencao = NumeroCabecasAtencao,
            EpsilonLayerNorm = EpsilonLayerNorm,
            TaxaAprendizado = TaxaAprendizado,
            Epocas = Epocas,
            MaximoExemplosPorEpoca = MaximoExemplosPorEpoca,
            MaximoPassosTreino = MaximoPassosTreino,
            EmbaralharExemplosPorEpoca = EmbaralharExemplosPorEpoca,
            UsarParadaAntecipada = UsarParadaAntecipada,
            PacienciaParadaAntecipada = PacienciaParadaAntecipada,
            MelhoraMinimaPerda = MelhoraMinimaPerda,
            TreinoAdaptativoAutomatico = TreinoAdaptativoAutomatico,
            TempoMaximoTreinoMinutos = TempoMaximoTreinoMinutos,
            PassosAmostraPlanejamento = PassosAmostraPlanejamento,
            MinimoExemplosPorEpocaAdaptado = MinimoExemplosPorEpocaAdaptado
        };
    }

    public bool EstaParadaAntecipadaAtivada()
    {
        return UsarParadaAntecipada ?? PacienciaParadaAntecipada > 0;
    }

    public int ObterPacienciaParadaAntecipadaEfetiva()
    {
        if (!EstaParadaAntecipadaAtivada())
        {
            return 0;
        }

        return PacienciaParadaAntecipada > 0
            ? PacienciaParadaAntecipada
            : PacienciaParadaAntecipadaPadrao;
    }
}

public sealed class ConfiguracaoGeracao
{
    public int MaximoNovosTokens { get; set; } = 24;
    public int AtrasoFluxoMs { get; set; } = 350;
    public int MaximoAlternativasExibidas { get; set; } = 2;
    public double Temperatura { get; set; }
    public int TopK { get; set; }
    public double TopP { get; set; } = 1.0;

    public ConfiguracaoGeracao Clonar()
    {
        return new ConfiguracaoGeracao
        {
            MaximoNovosTokens = MaximoNovosTokens,
            AtrasoFluxoMs = AtrasoFluxoMs,
            MaximoAlternativasExibidas = MaximoAlternativasExibidas,
            Temperatura = Temperatura,
            TopK = TopK,
            TopP = TopP
        };
    }
}

public sealed class ConfiguracaoExecucao
{
    public string Treino { get; set; } = "auto";
    public string Inferencia { get; set; } = "auto";

    public ConfiguracaoExecucao Clonar()
    {
        return new ConfiguracaoExecucao
        {
            Treino = Treino,
            Inferencia = Inferencia
        };
    }
}

public sealed class ConfiguracaoAutoCorpus
{
    public bool Ativar { get; set; } = true;

    public ConfiguracaoAutoCorpus Clonar()
    {
        return new ConfiguracaoAutoCorpus
        {
            Ativar = Ativar
        };
    }
}

public sealed class ConfiguracaoCognicao
{
    public bool Ativar { get; set; } = true;
    public string ModoThink { get; set; } = "resumoSeguro";
    public bool PersistirExperiencia { get; set; } = true;

    public ConfiguracaoCognicao Clonar()
    {
        return new ConfiguracaoCognicao
        {
            Ativar = Ativar,
            ModoThink = ModoThink,
            PersistirExperiencia = PersistirExperiencia
        };
    }
}
