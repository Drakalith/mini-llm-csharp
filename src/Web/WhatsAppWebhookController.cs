using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiniLlmDidatico.Canal;
using Microsoft.Extensions.Logging;

namespace MiniLlmDidatico.Web;

[ApiController]
[Route("webhook")]
public sealed class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppChannel _channel;
    private readonly GerenciadorSessoes _gerenciadorSessoes;
    private readonly ProcessadorRespostas _processador;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        WhatsAppChannel channel,
        GerenciadorSessoes gerenciadorSessoes,
        ProcessadorRespostas processador,
        ILogger<WhatsAppWebhookController> logger)
    {
        _channel = channel;
        _gerenciadorSessoes = gerenciadorSessoes;
        _processador = processador;
        _logger = logger;
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceberMensagem([FromBody] JsonElement payload)
    {
        try
        {
            if (!payload.TryGetProperty("event", out var eventProp) ||
                eventProp.GetString() != "messages.upsert")
            {
                return Ok();
            }

            if (!payload.TryGetProperty("data", out var dataProp))
            {
                return BadRequest();
            }

            var key = dataProp.GetProperty("key");
            var remoteJid = key.GetProperty("remoteJid").GetString();
            var fromMe = key.GetProperty("fromMe").GetBoolean();

            if (fromMe)
            {
                return Ok();
            }

            if (!dataProp.TryGetProperty("message", out var messageProp) ||
                !messageProp.TryGetProperty("conversation", out var conversationProp))
            {
                return Ok();
            }

            var mensagemUsuario = conversationProp.GetString();
            if (string.IsNullOrWhiteSpace(mensagemUsuario))
            {
                return Ok();
            }

            var sessionId = remoteJid ?? "unknown";
            _logger.LogInformation("Mensagem recebida de {SessionId}: {Mensagem}", sessionId, mensagemUsuario);

            var sessao = _gerenciadorSessoes.ObterOuCriarSessao(sessionId);
            _channel.EnfileirarMensagem(sessionId, mensagemUsuario);

            var resposta = _processador.ProcessarMensagem(mensagemUsuario, sessao, out var fonte);
            _logger.LogInformation("Resposta gerada para {SessionId} via {Fonte}", sessionId, fonte);

            await _channel.EnviarMensagemAsync(sessionId, resposta);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook WhatsApp");
            return StatusCode(500, "Erro interno ao processar mensagem");
        }
    }

    [HttpGet("whatsapp")]
    public IActionResult ValidarWebhook([FromQuery] string? hub_challenge)
    {
        return Ok(hub_challenge);
    }
}
