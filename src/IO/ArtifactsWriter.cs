using System.Text;
using System.Text.Json;

namespace MiniLlmDidatico.Arquivos;

public sealed class GravadorArtefatos
{
    private readonly string _raiz;
    private readonly JsonSerializerOptions _opcoesJson = PadroesSerializacao.CriarOpcoesJson();

    public GravadorArtefatos(string raiz)
    {
        _raiz = raiz;
        Directory.CreateDirectory(_raiz);
    }

    public string Raiz => _raiz;

    public void GravarJson<T>(string caminhoRelativo, T dados)
    {
        var caminhoCompleto = Path.Combine(_raiz, caminhoRelativo);
        var diretorio = Path.GetDirectoryName(caminhoCompleto);
        if (!string.IsNullOrWhiteSpace(diretorio))
        {
            Directory.CreateDirectory(diretorio);
        }

        var json = JsonSerializer.Serialize(dados, _opcoesJson);
        File.WriteAllText(caminhoCompleto, json, PadroesSerializacao.Utf8SemBom);
    }

    public void GravarTexto(string caminhoRelativo, string conteudo)
    {
        var caminhoCompleto = Path.Combine(_raiz, caminhoRelativo);
        var diretorio = Path.GetDirectoryName(caminhoCompleto);
        if (!string.IsNullOrWhiteSpace(diretorio))
        {
            Directory.CreateDirectory(diretorio);
        }

        File.WriteAllText(caminhoCompleto, conteudo, PadroesSerializacao.Utf8SemBom);
    }
}
