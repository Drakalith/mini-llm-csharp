using System.Text.RegularExpressions;
using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Ferramentas;

public static class SintetizadorRespostaContextual
{
    public static string TentarSintetizar(string entradaUsuario, IReadOnlyList<string> contextosFerramenta)
    {
        if (contextosFerramenta.Count == 0)
        {
            return string.Empty;
        }

        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var sentencasPontuadas = new List<(string Sentenca, int Pontuacao)>();
        foreach (var contexto in contextosFerramenta)
        {
            var contextoTratado = RemoverRotuloInicial(contexto);
            foreach (var sentenca in QuebrarEmSentencas(contextoTratado))
            {
                var sentencaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(sentenca);
                if (string.IsNullOrWhiteSpace(sentencaNormalizada))
                {
                    continue;
                }

                var pontuacao = PontuarSentenca(sentencaNormalizada, formasAtivas);
                sentencasPontuadas.Add((sentencaNormalizada, pontuacao));
            }
        }

        var melhores = sentencasPontuadas
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Sentenca.Length)
            .Select(x => x.Sentenca)
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToList();

        if (melhores.Count == 0)
        {
            return string.Empty;
        }

        if (sentencasPontuadas.Any(x => x.Pontuacao > 0))
        {
            return string.Join(" ", melhores).Trim();
        }

        return string.Empty;
    }

    public static bool TemEvidenciaObservacionalForte(IReadOnlyList<string> contextosFerramenta)
    {
        return contextosFerramenta.Any(contexto =>
        {
            var texto = TreinadorTokenizadorBpe.NormalizarTexto(contexto);
            return texto.Contains("fato atual confirmado pelo sistema", StringComparison.Ordinal) ||
                   texto.Contains("arquivo referenciado", StringComparison.Ordinal) ||
                   texto.Contains("ocorrencias em arquivos", StringComparison.Ordinal) ||
                   texto.Contains("pagina web", StringComparison.Ordinal) ||
                   texto.Contains("resultados web", StringComparison.Ordinal);
        });
    }

    public static bool RespostaPareceDesalinhada(string entradaUsuario, string resposta)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        var respostaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(resposta);

        var formasEntrada = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        if (formasEntrada.Count == 0)
        {
            return false;
        }

        var formasResposta = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(respostaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        if (formasResposta.Count == 0)
        {
            return true;
        }

        return formasEntrada.Intersect(formasResposta, StringComparer.Ordinal).Count() == 0;
    }

    private static int PontuarSentenca(string sentenca, IReadOnlySet<string> formasAtivas)
    {
        var formasSentenca = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(sentenca)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var pontuacao = formasSentenca.Count(formasAtivas.Contains) * 10;
        foreach (var forma in formasAtivas)
        {
            if (forma is "dia" or "hoje" && sentenca.Contains("data atual", StringComparison.Ordinal))
            {
                pontuacao += 6;
            }

            if (forma is "dia" or "hoje" && sentenca.Contains("dia de hoje", StringComparison.Ordinal))
            {
                pontuacao += 8;
            }

            if (forma is "hora" or "horas" && sentenca.Contains("hora atual", StringComparison.Ordinal))
            {
                pontuacao += 8;
            }

            if (forma is "sistema" or "computador" && sentenca.Contains("sistema operacional", StringComparison.Ordinal))
            {
                pontuacao += 6;
            }

            if (forma is "significa" or "significado" &&
                (sentenca.Contains("significado", StringComparison.Ordinal) ||
                 sentenca.Contains("define", StringComparison.Ordinal) ||
                 sentenca.Contains("defini", StringComparison.Ordinal)))
            {
                pontuacao += 10;
            }

            if (forma is "palavra" &&
                (sentenca.Contains("palavra", StringComparison.Ordinal) ||
                 sentenca.Contains("termo", StringComparison.Ordinal)))
            {
                pontuacao += 6;
            }
        }

        return pontuacao;
    }

    private static IEnumerable<string> QuebrarEmSentencas(string texto)
    {
        return Regex.Split(texto, @"(?<=[.!?])\s+")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string RemoverRotuloInicial(string texto)
    {
        var indice = texto.IndexOf(':');
        var resultado = indice < 0
            ? texto
            : texto[(indice + 1)..].Trim();

        if (resultado.StartsWith("fato atual confirmado pelo sistema :", StringComparison.OrdinalIgnoreCase))
        {
            resultado = resultado["fato atual confirmado pelo sistema :".Length..].Trim();
        }

        return resultado;
    }
}
