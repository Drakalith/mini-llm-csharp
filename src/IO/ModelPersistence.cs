using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Tokenizacao;
using MiniLlmDidatico.Treinamento;

namespace MiniLlmDidatico.Arquivos;

public sealed class DiagnosticoPersistenciaCerebro
{
    public bool ArquivoEncontrado { get; init; }
    public bool TextoTreinoCompativel { get; init; }
    public bool ContextoCognitivoCompativel { get; init; }
    public bool ConfiguracaoTreinoCompativel { get; init; }
    public bool MudancaSomenteContextoCognitivo { get; init; }
    public bool CorpusCompativel { get; init; }
    public bool ConfiguracaoCompativel { get; init; }
    public bool DeveCarregarCerebroSalvo { get; init; }
    public string CaminhoArquivo { get; init; } = string.Empty;
    public string HashTextoTreinoAtual { get; init; } = string.Empty;
    public string HashContextoCognitivoAtual { get; init; } = string.Empty;
    public string HashConfiguracaoTreinoAtual { get; init; } = string.Empty;
    public string HashCorpusAtual { get; init; } = string.Empty;
    public string HashConfiguracaoAtual { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
    public EstadoCerebroTreinado? EstadoEncontrado { get; init; }
    public EstadoCerebroTreinado? EstadoCompativel { get; init; }
}

public static class PersistenciaCerebroTreinado
{
    private const string NomeArquivoCerebro = "10_cerebro_treinado.json";
    private const int VersaoCompatibilidadeTreino = 3;
    private static readonly JsonSerializerOptions OpcoesJson = PadroesSerializacao.CriarOpcoesJson(
        writeIndented: true,
        propertyNameCaseInsensitive: true);

    public static DiagnosticoPersistenciaCerebro Diagnosticar(
        string raizArtefatos,
        IReadOnlyList<EntradaCorpus> corpus,
        ConfiguracaoAplicacao configuracao)
    {
        var caminhoArquivo = Path.Combine(raizArtefatos, NomeArquivoCerebro);
        var hashTextoTreinoAtual = CalcularHashTextoTreino(corpus);
        var hashContextoCognitivoAtual = CalcularHashContextoCognitivo(corpus);
        var hashConfiguracaoTreinoAtual = CalcularHashConfiguracaoTreino(configuracao);

        if (!File.Exists(caminhoArquivo))
        {
            return new DiagnosticoPersistenciaCerebro
            {
                ArquivoEncontrado = false,
                CaminhoArquivo = caminhoArquivo,
                HashTextoTreinoAtual = hashTextoTreinoAtual,
                HashContextoCognitivoAtual = hashContextoCognitivoAtual,
                HashConfiguracaoTreinoAtual = hashConfiguracaoTreinoAtual,
                HashCorpusAtual = hashContextoCognitivoAtual,
                HashConfiguracaoAtual = hashConfiguracaoTreinoAtual,
                Motivo = "nenhum cerebro salvo foi encontrado; sera necessario treinar"
            };
        }

        var estado = Carregar(caminhoArquivo);
        var hashTextoTreinoEstado = ObterHashTextoTreinoPersistido(estado);
        var hashContextoCognitivoEstado = ObterHashContextoCognitivoPersistido(estado);
        var hashConfiguracaoTreinoEstado = ObterHashConfiguracaoTreinoPersistido(estado);

        var textoTreinoCompativel =
            !string.IsNullOrWhiteSpace(hashTextoTreinoEstado) &&
            string.Equals(hashTextoTreinoEstado, hashTextoTreinoAtual, StringComparison.Ordinal);
        var contextoCognitivoCompativel =
            !string.IsNullOrWhiteSpace(hashContextoCognitivoEstado) &&
            string.Equals(hashContextoCognitivoEstado, hashContextoCognitivoAtual, StringComparison.Ordinal);
        var configuracaoTreinoCompativel =
            !string.IsNullOrWhiteSpace(hashConfiguracaoTreinoEstado) &&
            string.Equals(hashConfiguracaoTreinoEstado, hashConfiguracaoTreinoAtual, StringComparison.Ordinal);
        var mudancaSomenteContextoCognitivo =
            textoTreinoCompativel &&
            configuracaoTreinoCompativel &&
            !contextoCognitivoCompativel;
        var deveCarregar = textoTreinoCompativel && configuracaoTreinoCompativel;

        var motivo = deveCarregar
            ? mudancaSomenteContextoCognitivo
                ? "pesos de treino ainda sao compativeis, mas o contexto cognitivo mudou; o cerebro salvo pode ser carregado com aviso"
                : "cerebro salvo compativel com o texto treinado e a configuracao atual; o treino pode ser pulado"
            : !textoTreinoCompativel && !configuracaoTreinoCompativel
                ? "texto treinado e configuracao de treino mudaram; sera necessario treinar de novo"
                : !textoTreinoCompativel
                    ? "o texto efetivamente treinado mudou desde o ultimo cerebro salvo; sera necessario treinar de novo"
                    : "a configuracao de treino mudou desde o ultimo cerebro salvo; sera necessario treinar de novo";

        return new DiagnosticoPersistenciaCerebro
        {
            ArquivoEncontrado = true,
            TextoTreinoCompativel = textoTreinoCompativel,
            ContextoCognitivoCompativel = contextoCognitivoCompativel,
            ConfiguracaoTreinoCompativel = configuracaoTreinoCompativel,
            MudancaSomenteContextoCognitivo = mudancaSomenteContextoCognitivo,
            CorpusCompativel = textoTreinoCompativel && contextoCognitivoCompativel,
            ConfiguracaoCompativel = configuracaoTreinoCompativel,
            DeveCarregarCerebroSalvo = deveCarregar,
            CaminhoArquivo = caminhoArquivo,
            HashTextoTreinoAtual = hashTextoTreinoAtual,
            HashContextoCognitivoAtual = hashContextoCognitivoAtual,
            HashConfiguracaoTreinoAtual = hashConfiguracaoTreinoAtual,
            HashCorpusAtual = hashContextoCognitivoAtual,
            HashConfiguracaoAtual = hashConfiguracaoTreinoAtual,
            Motivo = motivo,
            EstadoEncontrado = estado,
            EstadoCompativel = deveCarregar ? estado : null
        };
    }

    public static void Salvar(
        string raizArtefatos,
        IReadOnlyList<EntradaCorpus> corpus,
        ConfiguracaoAplicacao configuracao,
        ModeloTokenizador modeloTokenizador,
        MiniTransformador modelo,
        ResultadoTreino? resultadoTreino)
    {
        Directory.CreateDirectory(raizArtefatos);

        var estado = new EstadoCerebroTreinado
        {
            CriadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp(),
            HashTextoTreino = CalcularHashTextoTreino(corpus),
            HashContextoCognitivo = CalcularHashContextoCognitivo(corpus),
            HashConfiguracaoTreino = CalcularHashConfiguracaoTreino(configuracao),
            HashCorpus = CalcularHashContextoCognitivo(corpus),
            HashConfiguracaoModelo = CalcularHashConfiguracaoTreino(configuracao),
            QuantidadeEntradasCorpus = corpus.Count,
            QuantidadeDominiosCorpus = corpus.Select(x => x.Dominio).Distinct(StringComparer.Ordinal).Count(),
            PerdaMediaFinal = resultadoTreino?.PerdasEpoca.LastOrDefault()?.PerdaMedia ?? 0.0,
            EpocasTreinadas = resultadoTreino?.PerdasEpoca.LastOrDefault()?.Epoca ?? 0,
            ModeloTokenizador = modeloTokenizador,
            HiperParametros = new HiperParametrosTransformador
            {
                TamanhoVocabulario = modeloTokenizador.TamanhoVocabulario,
                MaximoComprimentoSequencia = configuracao.MaximoComprimentoSequencia,
                DimensaoEmbedding = configuracao.Modelo.DimensaoEmbedding,
                DimensaoFeedForward = configuracao.Modelo.DimensaoFeedForward,
                NumeroCamadas = configuracao.Modelo.NumeroCamadas,
                NumeroCabecasAtencao = configuracao.Modelo.NumeroCabecasAtencao,
                EpsilonLayerNorm = configuracao.Modelo.EpsilonLayerNorm,
                TaxaAprendizado = configuracao.Modelo.TaxaAprendizado,
                SementeAleatoria = configuracao.SementeAleatoria
            },
            EstadoModelo = modelo.ExportarEstado()
        };

        var json = JsonSerializer.Serialize(estado, OpcoesJson);
        File.WriteAllText(
            Path.Combine(raizArtefatos, NomeArquivoCerebro),
            json,
            PadroesSerializacao.Utf8SemBom);
    }

    public static EstadoCerebroTreinado Carregar(string caminhoArquivo)
    {
        var json = File.ReadAllText(caminhoArquivo, PadroesSerializacao.Utf8SemBom);
        return JsonSerializer.Deserialize<EstadoCerebroTreinado>(json, OpcoesJson)
               ?? throw new InvalidOperationException("Falha ao carregar o cerebro treinado.");
    }

    public static string CalcularHashTextoTreino(IReadOnlyList<EntradaCorpus> corpus)
    {
        var conteudo = string.Join(
            "\n",
            corpus.Select(ConstrutorExemplosTreino.ComporTextoTreino));

        return CalcularHashSha256(conteudo);
    }

    public static string CalcularHashContextoCognitivo(IReadOnlyList<EntradaCorpus> corpus)
    {
        var conteudo = string.Join(
            "\n",
            corpus.Select(x => $"{x.Dominio}|{TreinadorTokenizadorBpe.NormalizarTexto(x.Texto)}"));

        return CalcularHashSha256(conteudo);
    }

    public static string CalcularHashConfiguracaoTreino(ConfiguracaoAplicacao configuracao)
    {
        var assinatura = JsonSerializer.Serialize(new
        {
            VersaoCompatibilidadeTreino,
            configuracao.MaximoMesclasTokenizador,
            configuracao.MaximoComprimentoSequencia,
            configuracao.SementeAleatoria,
            configuracao.Modelo.DimensaoEmbedding,
            configuracao.Modelo.DimensaoFeedForward,
            configuracao.Modelo.NumeroCamadas,
            configuracao.Modelo.NumeroCabecasAtencao,
            configuracao.Modelo.EpsilonLayerNorm,
            configuracao.Modelo.TaxaAprendizado,
            configuracao.Modelo.Epocas,
            configuracao.Modelo.MaximoExemplosPorEpoca,
            configuracao.Modelo.MaximoPassosTreino,
            configuracao.Modelo.EmbaralharExemplosPorEpoca,
            ParadaAntecipadaAtivada = configuracao.Modelo.EstaParadaAntecipadaAtivada(),
            PacienciaParadaAntecipadaEfetiva = configuracao.Modelo.ObterPacienciaParadaAntecipadaEfetiva(),
            configuracao.Modelo.MelhoraMinimaPerda,
            configuracao.Modelo.TreinoAdaptativoAutomatico,
            configuracao.Modelo.TempoMaximoTreinoMinutos,
            configuracao.Modelo.PassosAmostraPlanejamento,
            configuracao.Modelo.MinimoExemplosPorEpocaAdaptado
        });

        return CalcularHashSha256(assinatura);
    }

    public static string CalcularHashCorpus(IReadOnlyList<EntradaCorpus> corpus)
    {
        return CalcularHashContextoCognitivo(corpus);
    }

    public static string CalcularHashConfiguracao(ConfiguracaoAplicacao configuracao)
    {
        return CalcularHashConfiguracaoTreino(configuracao);
    }

    private static string CalcularHashSha256(string conteudo)
    {
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ObterHashTextoTreinoPersistido(EstadoCerebroTreinado estado)
    {
        return estado.VersaoFormato switch
        {
            "3" => estado.HashTextoTreino,
            _ => string.Empty
        };
    }

    private static string ObterHashContextoCognitivoPersistido(EstadoCerebroTreinado estado)
    {
        if (!string.IsNullOrWhiteSpace(estado.HashContextoCognitivo))
        {
            return estado.HashContextoCognitivo;
        }

        return estado.HashCorpus;
    }

    private static string ObterHashConfiguracaoTreinoPersistido(EstadoCerebroTreinado estado)
    {
        if (!string.IsNullOrWhiteSpace(estado.HashConfiguracaoTreino))
        {
            return estado.HashConfiguracaoTreino;
        }

        return estado.HashConfiguracaoModelo;
    }
}
