using System.Text.RegularExpressions;

namespace MiniLlmDidatico.Ferramentas;

public sealed class OcorrenciaBuscaArquivo
{
    public string Caminho { get; init; } = string.Empty;
    public int Linha { get; init; }
    public string TextoLinha { get; init; } = string.Empty;
}

public sealed class FerramentaBuscaArquivos
{
    private static readonly HashSet<string> DiretoriosIgnorados = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        "build",
        "backups",
        ".git",
        "node_modules"
    };

    public IReadOnlyList<string> LocalizarArquivos(string raizProjeto, string referencia, int maximo = 5)
    {
        if (string.IsNullOrWhiteSpace(raizProjeto) || string.IsNullOrWhiteSpace(referencia) || !Directory.Exists(raizProjeto))
        {
            return Array.Empty<string>();
        }

        if (Path.IsPathRooted(referencia) && File.Exists(referencia))
        {
            return [referencia];
        }

        var referenciaNormalizada = referencia.Trim().Trim('"', '\'');
        var candidatos = EnumerarArquivos(raizProjeto)
            .Where(caminho =>
            {
                var nome = Path.GetFileName(caminho);
                if (string.Equals(nome, referenciaNormalizada, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return caminho.Contains(referenciaNormalizada, StringComparison.OrdinalIgnoreCase);
            })
            .Take(maximo)
            .ToList();

        if (candidatos.Count > 0)
        {
            return candidatos;
        }

        return EnumerarArquivos(raizProjeto)
            .Where(caminho =>
            {
                var nome = Path.GetFileNameWithoutExtension(caminho);
                return nome.Contains(referenciaNormalizada, StringComparison.OrdinalIgnoreCase);
            })
            .Take(maximo)
            .ToList();
    }

    public IReadOnlyList<OcorrenciaBuscaArquivo> BuscarConteudo(
        string raizProjeto,
        string padrao,
        int maximoArquivos = 12,
        int maximoOcorrencias = 20)
    {
        if (string.IsNullOrWhiteSpace(raizProjeto) ||
            string.IsNullOrWhiteSpace(padrao) ||
            !Directory.Exists(raizProjeto))
        {
            return Array.Empty<OcorrenciaBuscaArquivo>();
        }

        var regex = new Regex(padrao, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var ocorrencias = new List<OcorrenciaBuscaArquivo>();
        var arquivosVisitados = 0;

        foreach (var caminho in EnumerarArquivos(raizProjeto))
        {
            if (arquivosVisitados >= maximoArquivos || ocorrencias.Count >= maximoOcorrencias)
            {
                break;
            }

            arquivosVisitados++;
            string[] linhas;
            try
            {
                linhas = File.ReadAllLines(caminho);
            }
            catch
            {
                continue;
            }

            for (var i = 0; i < linhas.Length && ocorrencias.Count < maximoOcorrencias; i++)
            {
                if (!regex.IsMatch(linhas[i]))
                {
                    continue;
                }

                ocorrencias.Add(new OcorrenciaBuscaArquivo
                {
                    Caminho = caminho,
                    Linha = i + 1,
                    TextoLinha = linhas[i].Trim()
                });
            }
        }

        return ocorrencias;
    }

    private static IEnumerable<string> EnumerarArquivos(string raizProjeto)
    {
        var pilha = new Stack<string>();
        pilha.Push(raizProjeto);

        while (pilha.Count > 0)
        {
            var diretorio = pilha.Pop();
            IEnumerable<string> subdiretorios;
            try
            {
                subdiretorios = Directory.EnumerateDirectories(diretorio);
            }
            catch
            {
                continue;
            }

            foreach (var subdiretorio in subdiretorios)
            {
                var nome = Path.GetFileName(subdiretorio);
                if (DiretoriosIgnorados.Contains(nome))
                {
                    continue;
                }

                pilha.Push(subdiretorio);
            }

            IEnumerable<string> arquivos;
            try
            {
                arquivos = Directory.EnumerateFiles(diretorio);
            }
            catch
            {
                continue;
            }

            foreach (var arquivo in arquivos)
            {
                yield return arquivo;
            }
        }
    }
}
