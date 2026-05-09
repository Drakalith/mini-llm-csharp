namespace MiniLlmDidatico.Treinamento;

public sealed class ExemploTreino
{
    public string Identificador { get; set; } = string.Empty;
    public string Dominio { get; set; } = "geral";
    public string TextoOriginal { get; set; } = string.Empty;
    public IReadOnlyList<int> IdsCompletos { get; set; } = Array.Empty<int>();
    public IReadOnlyList<int> IdsEntrada { get; set; } = Array.Empty<int>();
    public IReadOnlyList<int> IdsAlvo { get; set; } = Array.Empty<int>();
    public IReadOnlyList<double> PesosPerda { get; set; } = Array.Empty<double>();
}
