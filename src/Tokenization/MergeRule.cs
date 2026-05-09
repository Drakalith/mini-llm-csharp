namespace MiniLlmDidatico.Tokenizacao;

public sealed class RegraMescla
{
    public string Esquerda { get; set; } = string.Empty;
    public string Direita { get; set; } = string.Empty;
    public string Mesclado { get; set; } = string.Empty;
    public int FrequenciaNoTreino { get; set; }
}
