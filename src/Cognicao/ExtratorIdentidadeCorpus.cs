using System.Globalization;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public static class ExtratorIdentidadeCorpus
{
    public static PerfilIdentidadeConversacional Extrair(IReadOnlyList<EntradaCorpus> corpus)
    {
        var identidade = new PerfilIdentidadeConversacional();

        var nomeExtraido = ExtrairNome(corpus);
        if (!string.IsNullOrWhiteSpace(nomeExtraido))
        {
            identidade.NomeModelo = nomeExtraido;
        }

        identidade.AmbienteBase = ResumirDominio(corpus, "ambiente-digital", 2);
        identidade.DiferencaEuUsuario = ResumirDominio(corpus, "eu-e-usuario", 2);
        identidade.MissaoBase = ResumirMultiplosDominios(corpus, ["missao-e-valores", "reflexao-antes-da-resposta"], 2);

        return identidade;
    }

    private static string ExtrairNome(IReadOnlyList<EntradaCorpus> corpus)
    {
        var candidatos = corpus
            .Where(x => x.Dominio.Contains("identidade", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Texto)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var texto in candidatos)
        {
            foreach (var padrao in new[]
                     {
                         @"meu nome é\s+(?<nome>[\p{L}\p{M}\-]+)",
                         @"eu sou\s+(?<nome>[\p{L}\p{M}\-]+)",
                         @"(?<nome>[\p{L}\p{M}\-]+)\s+é o nome"
                     })
            {
                var match = Regex.Match(texto, padrao, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    return Capitalizar(match.Groups["nome"].Value);
                }
            }
        }

        var primeiraLinha = candidatos.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(primeiraLinha))
        {
            return "Ôria";
        }

        var primeiroToken = Regex.Match(primeiraLinha.Trim(), @"^(?<nome>[\p{L}\p{M}\-]+)");
        return primeiroToken.Success ? Capitalizar(primeiroToken.Groups["nome"].Value) : "Ôria";
    }

    private static string ResumirDominio(IReadOnlyList<EntradaCorpus> corpus, string dominio, int maximoLinhas)
    {
        return string.Join(
            " ",
            corpus.Where(x => string.Equals(x.Dominio, dominio, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Texto.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("pergunta:", StringComparison.OrdinalIgnoreCase))
                .Take(maximoLinhas)
                .Select(TreinadorTokenizadorBpe.NormalizarTexto))
            .Trim();
    }

    private static string ResumirMultiplosDominios(IReadOnlyList<EntradaCorpus> corpus, IEnumerable<string> dominios, int maximoLinhas)
    {
        var dominiosSet = dominios.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return string.Join(
            " ",
            corpus.Where(x => dominiosSet.Contains(x.Dominio))
                .Select(x => x.Texto.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("pergunta:", StringComparison.OrdinalIgnoreCase))
                .Take(maximoLinhas)
                .Select(TreinadorTokenizadorBpe.NormalizarTexto))
            .Trim();
    }

    private static string Capitalizar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var cultura = new CultureInfo("pt-BR");
        return cultura.TextInfo.ToTitleCase(valor.ToLower(cultura));
    }
}
