using System.Text.Json;
using MiniLlmDidatico.Arquivos;

namespace MiniLlmDidatico.Tokenizacao;

public sealed class ModeloTokenizador
{
    public string TokenPreenchimento { get; init; } = "<PREENCHIMENTO>";
    public string TokenInicio { get; init; } = "<INICIO>";
    public string TokenFim { get; init; } = "<FIM>";
    public string TokenDesconhecido { get; init; } = "<DESCONHECIDO>";

    public Dictionary<string, int> Vocabulario { get; init; } = new();
    public List<RegraMescla> Mesclas { get; init; } = new();

    public int IdPreenchimento => Vocabulario[TokenPreenchimento];
    public int IdInicio => Vocabulario[TokenInicio];
    public int IdFim => Vocabulario[TokenFim];
    public int IdDesconhecido => Vocabulario[TokenDesconhecido];
    public int TamanhoVocabulario => Vocabulario.Count;

    public string? TentarObterTokenPorId(int id)
    {
        foreach (var par in Vocabulario)
        {
            if (par.Value == id)
            {
                return par.Key;
            }
        }

        return null;
    }

    public void Salvar(string caminho)
    {
        var diretorio = Path.GetDirectoryName(caminho);
        if (!string.IsNullOrWhiteSpace(diretorio))
        {
            Directory.CreateDirectory(diretorio);
        }

        var json = JsonSerializer.Serialize(this, PadroesSerializacao.CriarOpcoesJson());
        File.WriteAllText(caminho, json, PadroesSerializacao.Utf8SemBom);
    }
}
