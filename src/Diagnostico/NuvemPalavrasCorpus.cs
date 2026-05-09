using System.Diagnostics;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Diagnostico;

public sealed class PalavraNuvemCorpus
{
    public string Texto { get; init; } = string.Empty;
    public int Frequencia { get; init; }
    public int EntradasDistintas { get; init; }
    public int VariacaoContexto { get; init; }
    public double PesoVisual { get; init; }
}

public sealed class RelatorioNuvemPalavrasCorpus
{
    public int TotalPalavrasConsideradas { get; init; }
    public int VocabularioConteudo { get; init; }
    public List<string> LinhasNuvem { get; init; } = new();
    public List<PalavraNuvemCorpus> AssuntosDominantes { get; init; } = new();
    public List<PalavraNuvemCorpus> PalavrasMaisVariadas { get; init; } = new();
}

public static class AnalisadorNuvemPalavrasCorpus
{
    private static readonly Regex _regexConteudo = new(@"^[\p{L}][\p{L}\p{N}_-]*$", RegexOptions.Compiled);
    private static readonly HashSet<string> _stopwords =
    [
        "a", "ao", "aos", "as", "ate", "até", "com", "como", "da", "das", "de", "do", "dos", "e", "é",
        "ela", "ele", "eles", "em", "entre", "era", "essa", "esse", "esta", "está", "este", "eu", "foi",
        "isso", "isto", "ja", "já", "la", "lá", "mais", "mas", "me", "mesmo", "muito", "na", "nas",
        "nem", "nao", "não", "no", "nos", "o", "os", "ou", "para", "pela", "pelas", "pelo", "pelos",
        "por", "porque", "pra", "qual", "que", "quem", "se", "sem", "ser", "sera", "será", "seu", "seus",
        "sua", "suas", "sao", "são", "tambem", "também", "tem", "têm", "ter", "texto", "um", "uma",
        "voce", "você", "pergunta", "resposta", "www", "http", "https", "site", "com", "br", "sobre",
        "pode", "dia", "às", "quando", "ainda", "todos", "anos", "bem", "fazer", "tempo", "pessoas",
        "assim", "só", "the", "and", "for", "from", "to", "in", "or", "post", "posts", "pingback",
        "arquivo", "arquivos", "tag", "tags", "home", "blog", "blogger", "wordpress", "facebook",
        "twitter", "email", "page", "pages", "comentario", "comentários", "comentario", "admin", "link"
    ];

    public static RelatorioNuvemPalavrasCorpus Analisar(
        IReadOnlyList<EntradaCorpus> corpus,
        Action<int, int, string>? aoAtualizarProgresso = null)
    {
        var estatisticas = new Dictionary<string, EstatisticaInterna>(StringComparer.OrdinalIgnoreCase);
        var totalPalavras = 0;
        var minimoEntradasDistintas = corpus.Count >= 500 ? 5 : corpus.Count >= 100 ? 3 : 1;
        var minimoFrequencia = corpus.Count >= 500 ? 5 : corpus.Count >= 100 ? 3 : 1;
        var cronometroAtualizacao = Stopwatch.StartNew();

        for (var indiceEntrada = 0; indiceEntrada < corpus.Count; indiceEntrada++)
        {
            var entrada = corpus[indiceEntrada];
            var texto = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Texto);
            var tokens = texto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(EhTokenConteudo)
                .ToList();

            totalPalavras += tokens.Count;

            for (var indice = 0; indice < tokens.Count; indice++)
            {
                var token = tokens[indice];
                if (!estatisticas.TryGetValue(token, out var estatistica))
                {
                    estatistica = new EstatisticaInterna(token);
                    estatisticas[token] = estatistica;
                }

                estatistica.Frequencia++;
                estatistica.Entradas.Add(entrada.Identificador);

                if (indice > 0)
                {
                    estatistica.Contextos.Add(tokens[indice - 1]);
                }

                if (indice < tokens.Count - 1)
                {
                    estatistica.Contextos.Add(tokens[indice + 1]);
                }
            }

            if (aoAtualizarProgresso is not null &&
                (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indiceEntrada == corpus.Count - 1))
            {
                aoAtualizarProgresso(indiceEntrada + 1, Math.Max(corpus.Count, 1), "mapeando palavras do corpus");
                cronometroAtualizacao.Restart();
            }
        }

        var palavrasBrutas = estatisticas.Values
            .Select(x => CriarPalavraNuvem(x, corpus.Count))
            .OrderByDescending(x => x.PesoVisual)
            .ThenByDescending(x => x.Frequencia)
            .ThenBy(x => x.Texto)
            .ToList();

        var palavras = palavrasBrutas
            .Where(x => x.EntradasDistintas >= minimoEntradasDistintas && x.Frequencia >= minimoFrequencia)
            .ToList();

        if (palavras.Count < 16)
        {
            palavras = palavrasBrutas;
        }

        return new RelatorioNuvemPalavrasCorpus
        {
            TotalPalavrasConsideradas = totalPalavras,
            VocabularioConteudo = palavras.Count,
            LinhasNuvem = CriarLinhasNuvem(palavras.Take(16).ToList()),
            AssuntosDominantes = palavras.Take(8).ToList(),
            PalavrasMaisVariadas = palavras
                .OrderByDescending(x => x.VariacaoContexto * System.Math.Log(1.0 + x.EntradasDistintas))
                .ThenByDescending(x => x.VariacaoContexto)
                .ThenByDescending(x => x.Frequencia)
                .ThenBy(x => x.Texto)
                .Take(8)
                .ToList()
        };
    }

    private static List<string> CriarLinhasNuvem(IReadOnlyList<PalavraNuvemCorpus> palavras)
    {
        var linhas = new List<string>();
        for (var indice = 0; indice < palavras.Count; indice += 4)
        {
            var grupo = palavras.Skip(indice).Take(4).Select(FormatarEtiquetaVisual);
            linhas.Add(string.Join("   ", grupo));
        }

        return linhas;
    }

    private static string FormatarEtiquetaVisual(PalavraNuvemCorpus palavra)
    {
        var texto = palavra.PesoVisual switch
        {
            >= 5.5 => palavra.Texto.ToUpperInvariant(),
            >= 3.5 => char.ToUpperInvariant(palavra.Texto[0]) + palavra.Texto[1..],
            _ => palavra.Texto
        };

        return texto.PadRight(14);
    }

    private static bool EhTokenConteudo(string token)
    {
        return token.Length > 1
               && _regexConteudo.IsMatch(token)
               && !token.All(char.IsDigit)
               && token.IndexOf("http", StringComparison.OrdinalIgnoreCase) < 0
               && token.IndexOf("www", StringComparison.OrdinalIgnoreCase) < 0
               && !_stopwords.Contains(token);
    }

    private static PalavraNuvemCorpus CriarPalavraNuvem(EstatisticaInterna estatistica, int totalEntradas)
    {
        var entradasDistintas = estatistica.Entradas.Count;
        var variacao = estatistica.Contextos.Count;
        var peso = System.Math.Log(1.0 + estatistica.Frequencia)
                   * System.Math.Log(1.0 + entradasDistintas)
                   * System.Math.Log(2.0 + variacao);

        return new PalavraNuvemCorpus
        {
            Texto = estatistica.Texto,
            Frequencia = estatistica.Frequencia,
            EntradasDistintas = entradasDistintas,
            VariacaoContexto = variacao,
            PesoVisual = peso
        };
    }

    private sealed class EstatisticaInterna
    {
        public EstatisticaInterna(string texto)
        {
            Texto = texto;
        }

        public string Texto { get; }
        public int Frequencia { get; set; }
        public HashSet<string> Entradas { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Contextos { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
