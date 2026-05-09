using System.Text;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Canal;
using MiniLlmDidatico.Cli;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Web;

Console.InputEncoding = PadroesSerializacao.Utf8SemBom;
Console.OutputEncoding = PadroesSerializacao.Utf8SemBom;

var modoChatCerebroSalvo = args.Any(arg =>
    string.Equals(arg, "chat", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--chat", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "/chat", StringComparison.OrdinalIgnoreCase));

var modoWhatsApp = args.Any(arg =>
    string.Equals(arg, "whatsapp", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "--whatsapp", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(arg, "/whatsapp", StringComparison.OrdinalIgnoreCase));

var raizProjeto = AppContext.BaseDirectory;
var raizBusca = raizProjeto;

for (var tentativa = 0; tentativa < 6; tentativa++)
{
    var caminhoCorpusTxt = Path.Combine(raizBusca, "corpus", "corpus.txt");
    var caminhoCorpusJson = Path.Combine(raizBusca, "corpus", "corpus.json");
    var caminhoConfiguracao = Path.Combine(raizBusca, "appsettings.json");
    var caminhoCorpus = File.Exists(caminhoCorpusTxt) ? caminhoCorpusTxt : caminhoCorpusJson;

    if (File.Exists(caminhoCorpus) && File.Exists(caminhoConfiguracao))
    {
        var configuracaoSolicitada = CarregadorConfiguracao.Carregar(caminhoConfiguracao);
        var corpus = CarregadorCorpus.Carregar(caminhoCorpus);
        var gravadorArtefatos = new GravadorArtefatos(Path.Combine(raizBusca, "artefatos"));
        var relatorioAutoConfiguracao = AutoConfiguradorCorpus.Configurar(configuracaoSolicitada, corpus);
        gravadorArtefatos.GravarJson("00_autoconfiguracao_corpus.json", relatorioAutoConfiguracao);
        var configuracaoEfetiva = relatorioAutoConfiguracao.ConfiguracaoEfetiva;

        if (modoWhatsApp)
        {
            var whatsAppConfig = CarregarConfigWhatsApp(caminhoConfiguracao);
            WhatsAppStartup.ExecutarServidor(
                configuracaoEfetiva,
                corpus,
                gravadorArtefatos.Raiz,
                relatorioAutoConfiguracao,
                whatsAppConfig);
            return;
        }

        if (modoChatCerebroSalvo)
        {
            var chat = new ChatCerebroSalvo(configuracaoEfetiva, corpus, gravadorArtefatos, relatorioAutoConfiguracao);
            chat.Executar();
            return;
        }

        var inspetor = new InspetorPipeline(configuracaoEfetiva, corpus, gravadorArtefatos, relatorioAutoConfiguracao);
        inspetor.Executar();
        return;
    }

    var diretorioPai = Directory.GetParent(raizBusca);
    if (diretorioPai is null)
    {
        break;
    }

    raizBusca = diretorioPai.FullName;
}

throw new InvalidOperationException(
    "Nao foi possivel localizar corpus/corpus.txt ou corpus/corpus.json e appsettings.json a partir do diretorio atual.");

static WhatsAppConfig CarregarConfigWhatsApp(string caminhoConfig)
{
    var json = File.ReadAllText(caminhoConfig, PadroesSerializacao.Utf8SemBom);
    using var doc = System.Text.Json.JsonDocument.Parse(json);
    var root = doc.RootElement;

    var config = new WhatsAppConfig();

    if (root.TryGetProperty("whatsapp", out var whatsAppProp))
    {
        if (whatsAppProp.TryGetProperty("evolutionApiUrl", out var urlProp))
            config.EvolutionApiUrl = urlProp.GetString() ?? config.EvolutionApiUrl;
        if (whatsAppProp.TryGetProperty("apiKey", out var keyProp))
            config.ApiKey = keyProp.GetString() ?? config.ApiKey;
        if (whatsAppProp.TryGetProperty("instanceName", out var nameProp))
            config.InstanceName = nameProp.GetString() ?? config.InstanceName;
        if (whatsAppProp.TryGetProperty("portaWebhook", out var portProp))
            config.PortaWebhook = portProp.GetInt32();
    }

    return config;
}
