using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Canal;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Ferramentas;
using MiniLlmDidatico.Geracao;
using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Numerica;
using MiniLlmDidatico.Tokenizacao;
using MiniLlmDidatico.Web;

namespace MiniLlmDidatico.Web;

public static class WhatsAppStartup
{
    public static void ExecutarServidor(
        ConfiguracaoAplicacao configuracao,
        IReadOnlyList<EntradaCorpus> corpus,
        string diretorioArtefatos,
        RelatorioAutoConfiguracaoCorpus relatorioAutoConfig,
        WhatsAppConfig whatsAppConfig)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
        builder.Services.AddLogging(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Information));

        var cerebro = CarregarCerebro(configuracao, corpus, diretorioArtefatos);

        builder.Services.AddSingleton(configuracao);
        builder.Services.AddSingleton(corpus);
        builder.Services.AddSingleton(diretorioArtefatos);
        builder.Services.AddSingleton(cerebro.Modelo);
        builder.Services.AddSingleton(cerebro.Gerador);
        builder.Services.AddSingleton(cerebro.Tokenizador);
        builder.Services.AddSingleton(cerebro.ModeloTokenizador);
        builder.Services.AddSingleton(cerebro.MemoriaEpistemica);
        builder.Services.AddSingleton(cerebro.EstadoCerebro);

        builder.Services.AddSingleton(whatsAppConfig);
        builder.Services.AddSingleton<WhatsAppChannel>(sp =>
        {
            var cfg = sp.GetRequiredService<WhatsAppConfig>();
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("evolution");
            var logger = sp.GetRequiredService<ILogger<WhatsAppChannel>>();
            return new WhatsAppChannel(cfg, http, logger);
        });

        builder.Services.AddHttpClient("evolution", client =>
        {
            client.BaseAddress = new Uri(whatsAppConfig.EvolutionApiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSingleton<GerenciadorSessoes>(sp =>
        {
            var cfg = sp.GetRequiredService<ConfiguracaoAplicacao>();
            var c = sp.GetRequiredService<IReadOnlyList<EntradaCorpus>>();
            var dir = sp.GetRequiredService<string>();
            return new GerenciadorSessoes(cfg, c, dir);
        });

        builder.Services.AddSingleton<ProcessadorRespostas>(sp =>
        {
            var cfg = sp.GetRequiredService<ConfiguracaoAplicacao>();
            var modelo = sp.GetRequiredService<MiniTransformador>();
            var gerador = sp.GetRequiredService<Gerador>();
            var tokenizador = sp.GetRequiredService<Tokenizador>();
            var modeloTokenizador = sp.GetRequiredService<ModeloTokenizador>();
            var memoria = sp.GetRequiredService<MemoriaEpistemica>();
            var dir = sp.GetRequiredService<string>();
            return new ProcessadorRespostas(cfg, modelo, gerador, tokenizador, modeloTokenizador, memoria, dir);
        });

        var app = builder.Build();

        app.MapControllers();

        Console.WriteLine($"Servidor WhatsApp iniciado em http://localhost:{whatsAppConfig.PortaWebhook}");
        Console.WriteLine($"Webhook: http://localhost:{whatsAppConfig.PortaWebhook}/webhook/whatsapp");
        Console.WriteLine($"Evolution API: {whatsAppConfig.EvolutionApiUrl}");
        Console.WriteLine($"Instancia: {whatsAppConfig.InstanceName}");

        app.Run($"http://0.0.0.0:{whatsAppConfig.PortaWebhook}");
    }

    private static CerebroCarregado CarregarCerebro(
        ConfiguracaoAplicacao configuracao,
        IReadOnlyList<EntradaCorpus> corpus,
        string diretorioArtefatos)
    {
        var gravador = new GravadorArtefatos(diretorioArtefatos);
        var diagnostico = PersistenciaCerebroTreinado.Diagnosticar(
            gravador.Raiz,
            corpus,
            configuracao);

        if (!diagnostico.ArquivoEncontrado)
        {
            throw new InvalidOperationException(
                $"Nenhum cerebro salvo foi encontrado em {diagnostico.CaminhoArquivo}. Rode o fluxo completo ao menos uma vez antes de usar o chat.");
        }

        var estadoCerebro = diagnostico.EstadoEncontrado
                         ?? PersistenciaCerebroTreinado.Carregar(diagnostico.CaminhoArquivo);

        var relatorioHardware = AnalisadorAdaptacaoHardware.Analisar(
            new PerfilCargaNumerica
            {
                DimensaoEmbedding = estadoCerebro.HiperParametros.DimensaoEmbedding,
                DimensaoFeedForward = estadoCerebro.HiperParametros.DimensaoFeedForward,
                TipoCarga = "inferencia"
            },
            configuracao.Execucao.Treino,
            configuracao.Execucao.Inferencia);

        var (executorNumerico, contextoBackend) = FabricaExecutorNumerico.Criar(relatorioHardware.DecisaoInferencia);
        var modelo = new MiniTransformador(estadoCerebro.HiperParametros, executorNumerico, contextoBackend);
        modelo.AplicarEstado(estadoCerebro.EstadoModelo);

        var tokenizador = new Tokenizador(estadoCerebro.ModeloTokenizador);
        var gerador = new Gerador(
            modelo,
            tokenizador,
            estadoCerebro.ModeloTokenizador,
            ParametrosAmostragemGeracao.DeConfiguracao(configuracao.Geracao),
            configuracao.SementeAleatoria);

        var memoriaEpistemica = ConstrutorMemoriaEpistemica.Construir(corpus);

        return new CerebroCarregado
        {
            EstadoCerebro = estadoCerebro,
            Modelo = modelo,
            Gerador = gerador,
            Tokenizador = tokenizador,
            ModeloTokenizador = estadoCerebro.ModeloTokenizador,
            MemoriaEpistemica = memoriaEpistemica
        };
    }

    private sealed class CerebroCarregado
    {
        public EstadoCerebroTreinado EstadoCerebro { get; init; } = null!;
        public MiniTransformador Modelo { get; init; } = null!;
        public Gerador Gerador { get; init; } = null!;
        public Tokenizador Tokenizador { get; init; } = null!;
        public ModeloTokenizador ModeloTokenizador { get; init; } = null!;
        public MemoriaEpistemica MemoriaEpistemica { get; init; } = null!;
    }
}
