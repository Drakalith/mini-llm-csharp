using MiniLlmDidatico.Cli;

namespace MiniLlmDidatico.Canal;

public sealed class ConsoleChannel : IChatChannel
{
    public Task<string?> LerMensagemAsync(string sessionId, CancellationToken ct = default)
    {
        return Task.FromResult(RenderizadorConsole.LerPrompt("Voce> "));
    }

    public Task EnviarMensagemAsync(string sessionId, string mensagem, CancellationToken ct = default)
    {
        RenderizadorConsole.Texto(mensagem);
        return Task.CompletedTask;
    }

    public Task ExibirPromptAsync(string sessionId, string rotulo, CancellationToken ct = default)
    {
        RenderizadorConsole.Prompt(rotulo);
        return Task.CompletedTask;
    }
}
