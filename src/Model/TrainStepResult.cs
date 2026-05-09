namespace MiniLlmDidatico.Modelo;

public sealed class ResultadoPassoTreino
{
    public double Perda { get; set; }
    public required CachePassagemDireta Cache { get; init; }
}
