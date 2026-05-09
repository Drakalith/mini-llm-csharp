namespace MiniLlmDidatico.Geracao;

public sealed class RastroGeracao
{
    public string PromptInterno { get; set; } = string.Empty;
    public string ModoSelecaoToken { get; set; } = "greedy";
    public double Temperatura { get; set; }
    public int TopK { get; set; }
    public double TopP { get; set; } = 1.0;
    public string TextoCompletoDecodificado { get; set; } = string.Empty;
    public List<PassoTokenGerado> Passos { get; set; } = new();
}

public sealed class PassoTokenGerado
{
    public int Passo { get; set; }
    public int IdTokenPrevisto { get; set; }
    public string TextoTokenPrevisto { get; set; } = string.Empty;
    public double ProbabilidadeTokenPrevisto { get; set; }
    public string TextoParcialDecodificado { get; set; } = string.Empty;
    public List<ProbabilidadeToken> MelhoresEscolhas { get; set; } = new();
}

public sealed class ProbabilidadeToken
{
    public int IdToken { get; set; }
    public string TextoToken { get; set; } = string.Empty;
    public double Probabilidade { get; set; }
}
