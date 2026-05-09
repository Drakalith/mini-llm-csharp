namespace MiniLlmDidatico.Dados;

public sealed class EntradaCorpus
{
    public string Identificador { get; set; } = string.Empty;
    public string Dominio { get; set; } = "geral";
    public string Texto { get; set; } = string.Empty;
}
