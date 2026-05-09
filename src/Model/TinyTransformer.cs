using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Numerica;

namespace MiniLlmDidatico.Modelo;

public sealed partial class MiniTransformador
{
    private readonly HiperParametrosTransformador _hiperParametros;
    private readonly IExecutorNumerico _executorNumerico;
    private readonly ContextoBackendNumerico _contextoBackend;
    private readonly Random _geradorAleatorio;

    private readonly double[][] _embeddingsToken;
    private readonly double[][] _embeddingsPosicionais;
    private readonly BlocoDecoder[] _blocos;
    private readonly ParametrosLayerNorm _normalizacaoFinal;
    private readonly double[][] _pesosSaida;
    private readonly double[] _viesSaida;

    public MiniTransformador(HiperParametrosTransformador hiperParametros, IExecutorNumerico? executorNumerico = null, ContextoBackendNumerico? contextoBackend = null)
    {
        if (hiperParametros.NumeroCabecasAtencao <= 0)
        {
            throw new InvalidOperationException("Numero de cabecas de atencao precisa ser maior que zero.");
        }

        if (hiperParametros.NumeroCamadas <= 0)
        {
            throw new InvalidOperationException("Numero de camadas precisa ser maior que zero.");
        }

        if (hiperParametros.DimensaoEmbedding % hiperParametros.NumeroCabecasAtencao != 0)
        {
            throw new InvalidOperationException(
                $"Dimensao embedding ({hiperParametros.DimensaoEmbedding}) precisa ser divisivel pelo numero de cabecas ({hiperParametros.NumeroCabecasAtencao}).");
        }

        _hiperParametros = hiperParametros;
        _executorNumerico = executorNumerico ?? new ExecutorNumericoCpu();
        _contextoBackend = contextoBackend ?? new ContextoBackendNumerico
        {
            NomeBackend = _executorNumerico.NomeBackend,
            DispositivoUsado = _executorNumerico.Dispositivo,
            DispositivoPreferidoPeloHardware = _executorNumerico.Dispositivo,
            BackendGpuDisponivel = false,
            Motivo = "executor numerico padrao em CPU"
        };
        _geradorAleatorio = new Random(_hiperParametros.SementeAleatoria);

        _embeddingsToken = _executorNumerico.EmbeddingsAleatorios(_hiperParametros.TamanhoVocabulario, _hiperParametros.DimensaoEmbedding, _geradorAleatorio);
        _embeddingsPosicionais = _executorNumerico.EmbeddingsAleatorios(_hiperParametros.MaximoComprimentoSequencia, _hiperParametros.DimensaoEmbedding, _geradorAleatorio);
        _blocos = Enumerable.Range(0, _hiperParametros.NumeroCamadas)
            .Select(_ => new BlocoDecoder(_hiperParametros, _executorNumerico, _geradorAleatorio))
            .ToArray();

        _normalizacaoFinal = ParametrosLayerNorm.Criar(_hiperParametros.DimensaoEmbedding);
        _pesosSaida = _executorNumerico.MatrizAleatoria(_hiperParametros.DimensaoEmbedding, _hiperParametros.TamanhoVocabulario, _geradorAleatorio);
        _viesSaida = _executorNumerico.Zeros(_hiperParametros.TamanhoVocabulario);
    }

    public int MaximoComprimentoSequencia => _hiperParametros.MaximoComprimentoSequencia;
    public HiperParametrosTransformador HiperParametros => _hiperParametros;
    public IExecutorNumerico ExecutorNumerico => _executorNumerico;
    public ContextoBackendNumerico ContextoBackend => _contextoBackend;

    public RetratoModelo Descrever()
    {
        var formas = new Dictionary<string, string>
        {
            ["embeddingsToken"] = $"{_hiperParametros.TamanhoVocabulario} x {_hiperParametros.DimensaoEmbedding}",
            ["embeddingsPosicionais"] = $"{_hiperParametros.MaximoComprimentoSequencia} x {_hiperParametros.DimensaoEmbedding}",
            ["normalizacaoFinal"] = $"gama/beta {_hiperParametros.DimensaoEmbedding}",
            ["pesosSaida"] = $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.TamanhoVocabulario}"
        };

        for (var indiceBloco = 0; indiceBloco < _blocos.Length; indiceBloco++)
        {
            var prefixo = $"bloco{indiceBloco + 1}";
            formas[$"{prefixo}.normalizacaoPreAtencao"] = $"gama/beta {_hiperParametros.DimensaoEmbedding}";
            formas[$"{prefixo}.normalizacaoPreFeedForward"] = $"gama/beta {_hiperParametros.DimensaoEmbedding}";
            formas[$"{prefixo}.atencaoPorCabeca"] = $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoCabeca} (x{_hiperParametros.NumeroCabecasAtencao})";
            formas[$"{prefixo}.projecaoSaidaAtencao"] = $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoEmbedding}";
            formas[$"{prefixo}.feedForward1"] = $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoFeedForward}";
            formas[$"{prefixo}.feedForward2"] = $"{_hiperParametros.DimensaoFeedForward} x {_hiperParametros.DimensaoEmbedding}";
        }

        return new RetratoModelo
        {
            NomeBackendNumerico = _contextoBackend.NomeBackend,
            DispositivoExecucao = _contextoBackend.DispositivoUsado,
            DispositivoPreferidoPeloHardware = _contextoBackend.DispositivoPreferidoPeloHardware,
            TamanhoVocabulario = _hiperParametros.TamanhoVocabulario,
            MaximoComprimentoSequencia = _hiperParametros.MaximoComprimentoSequencia,
            DimensaoEmbedding = _hiperParametros.DimensaoEmbedding,
            DimensaoFeedForward = _hiperParametros.DimensaoFeedForward,
            NumeroCamadas = _hiperParametros.NumeroCamadas,
            NumeroCabecasAtencao = _hiperParametros.NumeroCabecasAtencao,
            DimensaoCabeca = _hiperParametros.DimensaoCabeca,
            EpsilonLayerNorm = _hiperParametros.EpsilonLayerNorm,
            TaxaAprendizado = _hiperParametros.TaxaAprendizado,
            Formas = formas
        };
    }

    public RelatorioEscalaModelo GerarRelatorioEscala(int comprimentoSequenciaAtiva)
    {
        const int bytesPorParametro = sizeof(double);
        var componentes = new List<ComponenteParametroModelo>
        {
            CriarComponente("embeddings de token", "embedding", $"{_hiperParametros.TamanhoVocabulario} x {_hiperParametros.DimensaoEmbedding}", _hiperParametros.TamanhoVocabulario * _hiperParametros.DimensaoEmbedding, 0, bytesPorParametro),
            CriarComponente("embeddings posicionais", "embedding", $"{_hiperParametros.MaximoComprimentoSequencia} x {_hiperParametros.DimensaoEmbedding}", _hiperParametros.MaximoComprimentoSequencia * _hiperParametros.DimensaoEmbedding, 0, bytesPorParametro)
        };

        for (var indiceBloco = 0; indiceBloco < _hiperParametros.NumeroCamadas; indiceBloco++)
        {
            var sufixoBloco = $"bloco {indiceBloco + 1}";
            componentes.Add(CriarComponente($"layer norm pre-atencao {sufixoBloco}", "layer-norm", $"gama/beta {_hiperParametros.DimensaoEmbedding}", _hiperParametros.DimensaoEmbedding, _hiperParametros.DimensaoEmbedding, bytesPorParametro));

            for (var cabeca = 0; cabeca < _hiperParametros.NumeroCabecasAtencao; cabeca++)
            {
                var sufixoCabeca = $"{sufixoBloco} cabeca {cabeca + 1}";
                componentes.Add(CriarComponente($"consulta {sufixoCabeca}", "atencao", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoCabeca}", _hiperParametros.DimensaoEmbedding * _hiperParametros.DimensaoCabeca, 0, bytesPorParametro));
                componentes.Add(CriarComponente($"chave {sufixoCabeca}", "atencao", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoCabeca}", _hiperParametros.DimensaoEmbedding * _hiperParametros.DimensaoCabeca, 0, bytesPorParametro));
                componentes.Add(CriarComponente($"valor {sufixoCabeca}", "atencao", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoCabeca}", _hiperParametros.DimensaoEmbedding * _hiperParametros.DimensaoCabeca, 0, bytesPorParametro));
            }

            componentes.Add(CriarComponente($"projecao de saida da atencao {sufixoBloco}", "atencao", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoEmbedding}", _hiperParametros.DimensaoEmbedding * _hiperParametros.DimensaoEmbedding, _hiperParametros.DimensaoEmbedding, bytesPorParametro));
            componentes.Add(CriarComponente($"layer norm pre-feed-forward {sufixoBloco}", "layer-norm", $"gama/beta {_hiperParametros.DimensaoEmbedding}", _hiperParametros.DimensaoEmbedding, _hiperParametros.DimensaoEmbedding, bytesPorParametro));
            componentes.Add(CriarComponente($"feed-forward 1 {sufixoBloco}", "feed-forward", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.DimensaoFeedForward}", _hiperParametros.DimensaoEmbedding * _hiperParametros.DimensaoFeedForward, _hiperParametros.DimensaoFeedForward, bytesPorParametro));
            componentes.Add(CriarComponente($"feed-forward 2 {sufixoBloco}", "feed-forward", $"{_hiperParametros.DimensaoFeedForward} x {_hiperParametros.DimensaoEmbedding}", _hiperParametros.DimensaoFeedForward * _hiperParametros.DimensaoEmbedding, _hiperParametros.DimensaoEmbedding, bytesPorParametro));
        }

        componentes.Add(CriarComponente("layer norm final", "layer-norm", $"gama/beta {_hiperParametros.DimensaoEmbedding}", _hiperParametros.DimensaoEmbedding, _hiperParametros.DimensaoEmbedding, bytesPorParametro));
        componentes.Add(CriarComponente("saida logits", "saida", $"{_hiperParametros.DimensaoEmbedding} x {_hiperParametros.TamanhoVocabulario}", _hiperParametros.DimensaoEmbedding * _hiperParametros.TamanhoVocabulario, _hiperParametros.TamanhoVocabulario, bytesPorParametro));

        var camadasAtivas = new List<CamadaAtivaModelo>
        {
            CriarCamada("embedding de entrada", "embedding", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding)
        };

        for (var indiceBloco = 0; indiceBloco < _hiperParametros.NumeroCamadas; indiceBloco++)
        {
            var sufixoBloco = $"bloco {indiceBloco + 1}";
            camadasAtivas.Add(CriarCamada($"layer norm pre-atencao {sufixoBloco}", "layer-norm", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"consultas multi-head {sufixoBloco}", "atencao", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"chaves multi-head {sufixoBloco}", "atencao", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"valores multi-head {sufixoBloco}", "atencao", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"contexto multi-head {sufixoBloco}", "atencao", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"projecao de saida da atencao {sufixoBloco}", "atencao", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"residual pos-atencao {sufixoBloco}", "residual", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"layer norm pre-feed-forward {sufixoBloco}", "layer-norm", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"pre-ativacao feed-forward {sufixoBloco}", "feed-forward", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoFeedForward}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoFeedForward));
            camadasAtivas.Add(CriarCamada($"oculta feed-forward {sufixoBloco}", "feed-forward", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoFeedForward}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoFeedForward));
            camadasAtivas.Add(CriarCamada($"saida feed-forward {sufixoBloco}", "feed-forward", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
            camadasAtivas.Add(CriarCamada($"saida do bloco {sufixoBloco}", "residual", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
        }

        camadasAtivas.Add(CriarCamada("layer norm final", "layer-norm", $"{comprimentoSequenciaAtiva} x {_hiperParametros.DimensaoEmbedding}", comprimentoSequenciaAtiva * _hiperParametros.DimensaoEmbedding));
        camadasAtivas.Add(CriarCamada("logits", "saida", $"{comprimentoSequenciaAtiva} x {_hiperParametros.TamanhoVocabulario}", comprimentoSequenciaAtiva * _hiperParametros.TamanhoVocabulario));

        return new RelatorioEscalaModelo
        {
            ComprimentoSequenciaAtiva = comprimentoSequenciaAtiva,
            TotalNeuroniosAtivos = camadasAtivas.Sum(x => x.QuantidadeNeuronios),
            TotalPesosTreinaveis = componentes.Sum(x => x.QuantidadePesos),
            TotalVieses = componentes.Sum(x => x.QuantidadeVieses),
            TotalParametrosTreinaveis = componentes.Sum(x => x.QuantidadeParametros),
            MemoriaParametrosBytes = componentes.Sum(x => x.MemoriaBytes),
            ComponentesParametros = componentes,
            CamadasAtivas = camadasAtivas
        };
    }

    private static ComponenteParametroModelo CriarComponente(string nome, string tipo, string forma, long quantidadePesos, long quantidadeVieses, int bytesPorParametro)
    {
        var quantidadeParametros = quantidadePesos + quantidadeVieses;

        return new ComponenteParametroModelo
        {
            Nome = nome,
            Tipo = tipo,
            Forma = forma,
            QuantidadePesos = quantidadePesos,
            QuantidadeVieses = quantidadeVieses,
            QuantidadeParametros = quantidadeParametros,
            MemoriaBytes = quantidadeParametros * bytesPorParametro
        };
    }

    private static CamadaAtivaModelo CriarCamada(string nome, string tipo, string forma, long quantidadeNeuronios)
    {
        return new CamadaAtivaModelo
        {
            Nome = nome,
            Tipo = tipo,
            Forma = forma,
            QuantidadeNeuronios = quantidadeNeuronios
        };
    }

    private static void AplicarGradiente(double[][] pesos, double[][] gradientes, double taxaAprendizado)
    {
        for (var i = 0; i < pesos.Length; i++)
        {
            for (var j = 0; j < pesos[i].Length; j++)
            {
                pesos[i][j] -= taxaAprendizado * gradientes[i][j];
            }
        }
    }

    private static void AplicarGradiente(double[] pesos, double[] gradientes, double taxaAprendizado)
    {
        for (var i = 0; i < pesos.Length; i++)
        {
            pesos[i] -= taxaAprendizado * gradientes[i];
        }
    }
}
