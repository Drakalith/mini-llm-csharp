namespace MiniLlmDidatico.Cli;

public static class ExecutorPassosConsole
{
    public static void Cabecalho(string texto)
    {
        RenderizadorConsole.Cabecalho(texto);
    }

    public static void AguardarEnter(string texto)
    {
        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo(texto);
        Console.ReadLine();
    }
}
