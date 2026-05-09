using System.Collections.Concurrent;
using System.Text.Json;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Dados;

namespace MiniLlmDidatico.Canal;

public sealed class SessaoUsuario
{
    public string Id { get; init; } = string.Empty;
    public DateTime CriadoEm { get; init; }
    public DateTime UltimoAtivo { get; set; }
    public GestorMemoriaConversacional? Memoria { get; set; }
    public CortexConversacional? Cortex { get; set; }
    public GrafoConhecimento? Grafo { get; set; }
    public EstadoVocabularioDinamico? Vocabulario { get; set; }
    public List<VetorConceitual>? MatrizConceitual { get; set; }
    public List<string>? CategoriasEmergentes { get; set; }
}

public sealed class GerenciadorSessoes
{
    private readonly ConcurrentDictionary<string, SessaoUsuario> _sessoes = new();
    private readonly ConfiguracaoAplicacao _config;
    private readonly IReadOnlyList<EntradaCorpus> _corpus;
    private readonly string _diretorioArtefatos;

    public GerenciadorSessoes(
        ConfiguracaoAplicacao config,
        IReadOnlyList<EntradaCorpus> corpus,
        string diretorioArtefatos)
    {
        _config = config;
        _corpus = corpus;
        _diretorioArtefatos = diretorioArtefatos;
    }

    public SessaoUsuario ObterOuCriarSessao(string numeroTelefone)
    {
        var sessionId = numeroTelefone.Contains("@") ? numeroTelefone : $"{numeroTelefone}@s.whatsapp.net";

        if (_sessoes.TryGetValue(sessionId, out var sessao))
        {
            sessao.UltimoAtivo = DateTime.UtcNow;
            return sessao;
        }

        var novaSessao = CriarSessao(sessionId);
        _sessoes[sessionId] = novaSessao;
        return novaSessao;
    }

    private SessaoUsuario CriarSessao(string sessionId)
    {
        var dirSessao = Path.Combine(_diretorioArtefatos, "sessoes", sessionId.Replace("@s.whatsapp.net", ""));
        Directory.CreateDirectory(dirSessao);

        var memoria = GestorMemoriaConversacional.CarregarOuCriar(dirSessao, null, null);
        var cortex = CortexConversacional.CarregarOuCriar(dirSessao, _config.Cognicao);
        var grafo = RepositorioGrafoConhecimento.CarregarOuReconstruir(dirSessao, _corpus, memoria?.Estado);
        var vocabulario = VocabularioDinamico.Carregar(dirSessao);
        var matrizVetores = MatrizConceitual.CarregarOuCriar(dirSessao);
        var matrizPath = Path.Combine(dirSessao, "17_matriz_conceitual.json");
        List<string> categoriasEmergentes;
        if (File.Exists(matrizPath))
        {
            var json = File.ReadAllText(matrizPath, PadroesSerializacao.Utf8SemBom);
            var estado = JsonSerializer.Deserialize<EstadoMatrizConceitual>(json, PadroesSerializacao.CriarOpcoesJson());
            categoriasEmergentes = estado?.CategoriasEmergentes ?? new List<string>();
        }
        else
        {
            categoriasEmergentes = new List<string>();
        }

        return new SessaoUsuario
        {
            Id = sessionId,
            CriadoEm = DateTime.UtcNow,
            UltimoAtivo = DateTime.UtcNow,
            Memoria = memoria,
            Cortex = cortex,
            Grafo = grafo,
            Vocabulario = vocabulario,
            MatrizConceitual = matrizVetores,
            CategoriasEmergentes = categoriasEmergentes
        };
    }

    public bool ExisteSessao(string numeroTelefone)
    {
        var sessionId = numeroTelefone.Contains("@") ? numeroTelefone : $"{numeroTelefone}@s.whatsapp.net";
        return _sessoes.ContainsKey(sessionId);
    }

    public void RemoverSessao(string numeroTelefone)
    {
        var sessionId = numeroTelefone.Contains("@") ? numeroTelefone : $"{numeroTelefone}@s.whatsapp.net";
        _sessoes.TryRemove(sessionId, out _);
    }

    public IEnumerable<SessaoUsuario> SessoesAtivas(TimeSpan? inatividadeMaxima = null)
    {
        var limite = inatividadeMaxima ?? TimeSpan.FromHours(24);
        var agora = DateTime.UtcNow;

        return _sessoes.Values.Where(s => agora - s.UltimoAtivo < limite);
    }
}
