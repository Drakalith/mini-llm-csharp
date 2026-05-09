namespace MiniLlmDidatico.Cli;

public enum TomConsole
{
    Padrao,
    Info,
    Sucesso,
    Aviso,
    Erro,
    Muted,
    Destaque
}

public static class RenderizadorConsole
{
    private const int LarguraPadraoSeparador = 88;
    private static readonly object TravaSaida = new();
    private static readonly bool CoresAtivadas =
        !Console.IsOutputRedirected &&
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));

    public static bool UsaCores => CoresAtivadas;

    public static void Cabecalho(string texto)
    {
        LinhaEmBranco();
        Separador('=');
        Texto(texto, TomConsole.Destaque);
        Separador('=');
    }

    public static void Secao(string titulo, string? descricao = null)
    {
        LinhaEmBranco();
        Texto(titulo, TomConsole.Destaque);
        Separador('-');

        if (!string.IsNullOrWhiteSpace(descricao))
        {
            Texto(descricao, TomConsole.Muted);
        }
    }

    public static void Subtitulo(string texto)
    {
        LinhaEmBranco();
        Texto(texto, TomConsole.Info);
    }

    public static void Texto(string texto, TomConsole tom = TomConsole.Padrao)
    {
        EscreverInterno(texto + Environment.NewLine, tom);
    }

    public static void Alerta(string texto)
    {
        Texto("[AVISO] " + texto, TomConsole.Aviso);
    }

    public static void Erro(string texto)
    {
        Texto("[ERRO] " + texto, TomConsole.Erro);
    }

    public static void Sucesso(string texto)
    {
        Texto("[OK] " + texto, TomConsole.Sucesso);
    }

    public static void Observacao(string texto)
    {
        Texto(texto, TomConsole.Muted);
    }

    public static void ChaveValor(
        string chave,
        object? valor,
        TomConsole tomValor = TomConsole.Padrao,
        int larguraChave = 30)
    {
        var rotulo = (chave.TrimEnd() + ":").PadRight(Math.Max(12, larguraChave));
        lock (TravaSaida)
        {
            EscreverSemNovaLinha(rotulo, TomConsole.Info);
            EscreverSemNovaLinha((valor?.ToString() ?? string.Empty) + Environment.NewLine, tomValor);
        }
    }

    public static void Status(string chave, bool ativo, string textoSucesso = "sim", string textoNegado = "nao")
    {
        var texto = ativo ? $"[OK] {textoSucesso}" : $"[NAO] {textoNegado}";
        ChaveValor(chave, texto, ativo ? TomConsole.Sucesso : TomConsole.Aviso);
    }

    public static void ItemLista(string texto, TomConsole tomTexto = TomConsole.Padrao, string marcador = "-")
    {
        lock (TravaSaida)
        {
            EscreverSemNovaLinha(marcador + " ", TomConsole.Muted);
            EscreverSemNovaLinha(texto + Environment.NewLine, tomTexto);
        }
    }

    public static void Separador(char caractere = '-', int largura = LarguraPadraoSeparador, TomConsole tom = TomConsole.Muted)
    {
        Texto(new string(caractere, largura), tom);
    }

    public static void LinhaEmBranco()
    {
        Console.WriteLine();
    }

    public static void Prompt(string rotulo, TomConsole tom = TomConsole.Destaque)
    {
        EscreverSemNovaLinha(rotulo, tom);
    }

    public static string? LerPrompt(string rotulo, TomConsole tom = TomConsole.Destaque)
    {
        Prompt(rotulo, tom);
        return Console.ReadLine();
    }

    public static void LinhaProgresso(string texto, TomConsole tom = TomConsole.Info)
    {
        lock (TravaSaida)
        {
            AplicarTom(tom);
            Console.Write("\r" + texto);
            RestaurarTom();
        }
    }

    private static void EscreverInterno(string texto, TomConsole tom)
    {
        lock (TravaSaida)
        {
            EscreverSemNovaLinha(texto, tom);
        }
    }

    private static void EscreverSemNovaLinha(string texto, TomConsole tom)
    {
        AplicarTom(tom);
        Console.Write(texto);
        RestaurarTom();
    }

    private static void AplicarTom(TomConsole tom)
    {
        if (!CoresAtivadas)
        {
            return;
        }

        Console.ForegroundColor = tom switch
        {
            TomConsole.Info => ConsoleColor.Cyan,
            TomConsole.Sucesso => ConsoleColor.Green,
            TomConsole.Aviso => ConsoleColor.Yellow,
            TomConsole.Erro => ConsoleColor.Red,
            TomConsole.Muted => ConsoleColor.DarkGray,
            TomConsole.Destaque => ConsoleColor.White,
            _ => ConsoleColor.Gray
        };
    }

    private static void RestaurarTom()
    {
        if (!CoresAtivadas)
        {
            return;
        }

        Console.ResetColor();
    }
}
