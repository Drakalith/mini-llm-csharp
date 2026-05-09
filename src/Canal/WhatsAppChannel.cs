using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MiniLlmDidatico.Canal;

public sealed class WhatsAppChannel : IChatChannel
{
    private readonly WhatsAppConfig _config;
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppChannel> _logger;
    private readonly Dictionary<string, Queue<string>> _filasMensagens = new();
    private readonly object _lockFilas = new();

    public WhatsAppChannel(WhatsAppConfig config, HttpClient http, ILogger<WhatsAppChannel> logger)
    {
        _config = config;
        _http = http;
        _logger = logger;
    }

    public void EnfileirarMensagem(string sessionId, string mensagem)
    {
        lock (_lockFilas)
        {
            if (!_filasMensagens.TryGetValue(sessionId, out var fila))
            {
                fila = new Queue<string>();
                _filasMensagens[sessionId] = fila;
            }
            fila.Enqueue(mensagem);
        }
    }

    public async Task<string?> LerMensagemAsync(string sessionId, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            string? mensagem = null;
            lock (_lockFilas)
            {
                if (_filasMensagens.TryGetValue(sessionId, out var fila) && fila.Count > 0)
                {
                    mensagem = fila.Dequeue();
                }
            }

            if (mensagem != null)
            {
                return mensagem;
            }

            await Task.Delay(500, ct);
        }

        return null;
    }

    public async Task EnviarMensagemAsync(string sessionId, string mensagem, CancellationToken ct = default)
    {
        try
        {
            var numero = sessionId.Replace("@s.whatsapp.net", "");
            var url = $"{_config.EvolutionApiUrl}/message/sendText/{_config.InstanceName}";

            var payload = new
            {
                number = numero,
                text = mensagem,
                delay = 1200
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.TryAddWithoutValidation("apikey", _config.ApiKey);
            var response = await _http.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Mensagem enviada para {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem para {SessionId}", sessionId);
        }
    }

    public Task ExibirPromptAsync(string sessionId, string rotulo, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
