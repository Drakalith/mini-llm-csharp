namespace MiniLlmDidatico.Canal;

public interface IChatChannel
{
    Task<string?> LerMensagemAsync(string sessionId, CancellationToken ct = default);
    Task EnviarMensagemAsync(string sessionId, string mensagem, CancellationToken ct = default);
    Task ExibirPromptAsync(string sessionId, string rotulo, CancellationToken ct = default);
}
