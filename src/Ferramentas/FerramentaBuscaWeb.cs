using System.Net;
using System.Text.RegularExpressions;

namespace MiniLlmDidatico.Ferramentas;

public sealed class ResultadoBuscaWeb
{
    public string Titulo { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Resumo { get; init; } = string.Empty;
}

public sealed class ConteudoPaginaWeb
{
    public string Url { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string TextoExtraido { get; init; } = string.Empty;
    public bool FoiTruncado { get; init; }
}

public sealed class FerramentaBuscaWeb
{
    private static readonly HttpClient ClienteHttp = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    static FerramentaBuscaWeb()
    {
        ClienteHttp.DefaultRequestHeaders.UserAgent.ParseAdd("MiniLlmDidatico/1.0");
    }

    public async Task<IReadOnlyList<ResultadoBuscaWeb>> PesquisarAsync(string consulta, int maximo = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(consulta))
        {
            return Array.Empty<ResultadoBuscaWeb>();
        }

        var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(consulta)}";
        var html = await ClienteHttp.GetStringAsync(url, cancellationToken);
        var resultados = new List<ResultadoBuscaWeb>();

        foreach (Match match in Regex.Matches(
                     html,
                     "<a[^>]*class=\"result__a\"[^>]*href=\"(?<url>[^\"]+)\"[^>]*>(?<titulo>.*?)</a>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var link = ResolverUrlResultado(WebUtility.HtmlDecode(match.Groups["url"].Value));
            var titulo = LimparHtml(match.Groups["titulo"].Value);
            if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(titulo))
            {
                continue;
            }

            var resumo = ExtrairResumoResultado(html, match.Index);

            resultados.Add(new ResultadoBuscaWeb
            {
                Titulo = titulo,
                Url = link,
                Resumo = resumo
            });

            if (resultados.Count >= maximo)
            {
                break;
            }
        }

        return resultados;
    }

    public async Task<ConteudoPaginaWeb?> BaixarPaginaAsync(string url, int maximoCaracteres = 2400, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var html = await ClienteHttp.GetStringAsync(url, cancellationToken);
        var titulo = string.Empty;
        var tituloMatch = Regex.Match(html, @"<title[^>]*>(?<titulo>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (tituloMatch.Success)
        {
            titulo = LimparHtml(tituloMatch.Groups["titulo"].Value);
        }

        var texto = LimparHtml(html);
        var truncado = false;
        if (texto.Length > maximoCaracteres)
        {
            texto = texto[..maximoCaracteres].TrimEnd();
            truncado = true;
        }

        return new ConteudoPaginaWeb
        {
            Url = url,
            Titulo = titulo,
            TextoExtraido = texto,
            FoiTruncado = truncado
        };
    }

    private static string LimparHtml(string html)
    {
        var semScript = Regex.Replace(html, @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var semStyle = Regex.Replace(semScript, @"<style\b[^<]*(?:(?!</style>)<[^<]*)*</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var semTags = Regex.Replace(semStyle, "<[^>]+>", " ");
        var decodificado = WebUtility.HtmlDecode(semTags);
        return Regex.Replace(decodificado, @"\s+", " ").Trim();
    }

    private static string ExtrairResumoResultado(string html, int indiceReferencia)
    {
        var trechoBusca = html[indiceReferencia..Math.Min(html.Length, indiceReferencia + 4000)];
        var correspondencia = Regex.Match(
            trechoBusca,
            "<a[^>]*class=\"result__snippet\"[^>]*>(?<texto>.*?)</a>|<div[^>]*class=\"result__snippet\"[^>]*>(?<textoAlternativo>.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!correspondencia.Success)
        {
            return string.Empty;
        }

        var texto = correspondencia.Groups["texto"].Success
            ? correspondencia.Groups["texto"].Value
            : correspondencia.Groups["textoAlternativo"].Value;

        return LimparHtml(texto);
    }

    private static string ResolverUrlResultado(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (url.StartsWith("//duckduckgo.com/l/?", StringComparison.OrdinalIgnoreCase))
        {
            url = "https:" + url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Host.Contains("duckduckgo.com", StringComparison.OrdinalIgnoreCase))
        {
            var destino = ExtrairParametroConsulta(uri.Query, "uddg");
            if (!string.IsNullOrWhiteSpace(destino))
            {
                return WebUtility.UrlDecode(destino);
            }
        }

        return url;
    }

    private static string ExtrairParametroConsulta(string query, string nome)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var consulta = query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var parte in consulta)
        {
            var indice = parte.IndexOf('=');
            if (indice <= 0)
            {
                continue;
            }

            var chave = parte[..indice];
            if (!string.Equals(chave, nome, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return parte[(indice + 1)..];
        }

        return string.Empty;
    }
}
