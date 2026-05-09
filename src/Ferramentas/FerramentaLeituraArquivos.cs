using System.Text;

namespace MiniLlmDidatico.Ferramentas;

public sealed class ResultadoLeituraArquivo
{
    public string Caminho { get; init; } = string.Empty;
    public string Conteudo { get; init; } = string.Empty;
    public int TotalLinhasLidas { get; init; }
    public bool FoiTruncado { get; init; }
}

public sealed class FerramentaLeituraArquivos
{
    public ResultadoLeituraArquivo? LerTexto(string caminho, int maximoLinhas = 40, int maximoCaracteres = 2400)
    {
        if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
        {
            return null;
        }

        var linhas = File.ReadAllLines(caminho, Encoding.UTF8);
        var truncado = false;
        var buffer = new StringBuilder();
        var linhasIncluidas = 0;

        foreach (var linha in linhas)
        {
            if (linhasIncluidas >= maximoLinhas || buffer.Length >= maximoCaracteres)
            {
                truncado = true;
                break;
            }

            buffer.AppendLine(linha);
            linhasIncluidas++;
        }

        var conteudo = buffer.ToString().TrimEnd();
        if (conteudo.Length > maximoCaracteres)
        {
            conteudo = conteudo[..maximoCaracteres].TrimEnd();
            truncado = true;
        }

        return new ResultadoLeituraArquivo
        {
            Caminho = caminho,
            Conteudo = conteudo,
            TotalLinhasLidas = linhasIncluidas,
            FoiTruncado = truncado
        };
    }
}
