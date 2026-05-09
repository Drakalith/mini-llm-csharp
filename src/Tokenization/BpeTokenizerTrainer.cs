using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniLlmDidatico.Tokenizacao;

public sealed class TreinadorTokenizadorBpe
{
    private const string SeparadorMescla = "\u001F";
    private const int MaximoPartesPorToken = 2;

    public ResultadoTreinoTokenizador Treinar(
        IEnumerable<string> textos,
        int maximoMesclas,
        Action<int, int, string>? aoAtualizarProgresso = null)
    {
        var textosLista = textos.ToList();
        var sequencias = new List<List<string>>(textosLista.Count);
        var totalEtapas = Math.Max(textosLista.Count + Math.Max(maximoMesclas, 1), 1);
        var progressoAtual = 0;
        var cronometroAtualizacao = Stopwatch.StartNew();

        for (var indiceTexto = 0; indiceTexto < textosLista.Count; indiceTexto++)
        {
            var tokens = PreTokenizar(textosLista[indiceTexto]);
            if (tokens.Count > 0)
            {
                sequencias.Add(tokens);
            }

            progressoAtual++;
            if (aoAtualizarProgresso is not null &&
                (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indiceTexto == textosLista.Count - 1))
            {
                aoAtualizarProgresso(progressoAtual, totalEtapas, "pre-tokenizando corpus");
                cronometroAtualizacao.Restart();
            }
        }

        var mesclas = new List<RegraMescla>();

        for (var indiceMescla = 0; indiceMescla < maximoMesclas; indiceMescla++)
        {
            var frequenciasPares = ContarPares(sequencias);
            if (frequenciasPares.Count == 0)
            {
                break;
            }

            var melhorPar = frequenciasPares
                .OrderByDescending(x => x.Value)
                .ThenBy(x => RenderizarSimbolo(x.Key.Esquerda), StringComparer.Ordinal)
                .ThenBy(x => RenderizarSimbolo(x.Key.Direita), StringComparer.Ordinal)
                .First();

            if (melhorPar.Value < 2)
            {
                break;
            }

            var mesclado = $"{melhorPar.Key.Esquerda}{SeparadorMescla}{melhorPar.Key.Direita}";
            var regra = new RegraMescla
            {
                Esquerda = melhorPar.Key.Esquerda,
                Direita = melhorPar.Key.Direita,
                Mesclado = mesclado,
                FrequenciaNoTreino = melhorPar.Value
            };
            mesclas.Add(regra);

            foreach (var sequencia in sequencias)
            {
                AplicarMesclaEm(sequencia, regra);
            }

            progressoAtual++;
            aoAtualizarProgresso?.Invoke(progressoAtual, totalEtapas, "aprendendo mesclas bpe");
        }

        var vocabulario = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["<PREENCHIMENTO>"] = 0,
            ["<INICIO>"] = 1,
            ["<FIM>"] = 2,
            ["<DESCONHECIDO>"] = 3
        };

        foreach (var simbolo in sequencias
                     .SelectMany(x => x)
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(RenderizarSimbolo, StringComparer.Ordinal))
        {
            if (!vocabulario.ContainsKey(simbolo))
            {
                vocabulario[simbolo] = vocabulario.Count;
            }
        }

        var modelo = new ModeloTokenizador
        {
            Vocabulario = vocabulario,
            Mesclas = mesclas
        };

        aoAtualizarProgresso?.Invoke(totalEtapas, totalEtapas, "finalizando tokenizador");

        return new ResultadoTreinoTokenizador(modelo, sequencias);
    }

    public static List<string> PreTokenizar(string texto)
    {
        var textoNormalizado = NormalizarTexto(texto);
        if (string.IsNullOrWhiteSpace(textoNormalizado))
        {
            return new List<string>();
        }

        return Regex.Matches(textoNormalizado, @"\p{L}+|\p{N}+|[^\s\p{L}\p{N}]")
            .Select(match => match.Value)
            .ToList();
    }

    public static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var textoSemControles = RemoverCaracteresDeControle(texto);
        var textoMinusculo = textoSemControles
            .Trim()
            .ToLowerInvariant();

        textoMinusculo = Regex.Replace(textoMinusculo, @"\s+", " ");
        textoMinusculo = Regex.Replace(textoMinusculo, @"\s*([:;,.!?])\s*", " $1 ");
        textoMinusculo = Regex.Replace(textoMinusculo, @"\s+", " ");

        return textoMinusculo.Trim();
    }

    public static string FormatarTextoParaExibicao(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var formatado = Regex.Replace(texto, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        formatado = Regex.Replace(formatado, @"\s+([,.;:!?])", "$1", RegexOptions.CultureInvariant);
        formatado = Regex.Replace(formatado, @"([¿¡(\[{])\s+", "$1", RegexOptions.CultureInvariant);
        formatado = Regex.Replace(formatado, @"\s+([)\]}])", "$1", RegexOptions.CultureInvariant);
        return formatado.Trim();
    }

    private static string RemoverCaracteresDeControle(string texto)
    {
        var builder = new StringBuilder(texto.Length);

        foreach (var caractere in texto)
        {
            if (char.IsControl(caractere))
            {
                if (caractere is '\r' or '\n' or '\t')
                {
                    builder.Append(' ');
                }

                continue;
            }

            builder.Append(caractere);
        }

        return builder.ToString();
    }

    public static string RenderizarSimbolo(string simbolo)
    {
        var partes = simbolo.Split(SeparadorMescla, StringSplitOptions.None);
        return DesfazerTokenizacao(partes);
    }

    private static Dictionary<ChavePar, int> ContarPares(List<List<string>> sequencias)
    {
        var frequencias = new Dictionary<ChavePar, int>();

        foreach (var sequencia in sequencias)
        {
            for (var i = 0; i < sequencia.Count - 1; i++)
            {
                var esquerda = sequencia[i];
                var direita = sequencia[i + 1];

                if (ContarPartes(esquerda) + ContarPartes(direita) > MaximoPartesPorToken)
                {
                    continue;
                }

                if (ContemPontuacao(esquerda) || ContemPontuacao(direita))
                {
                    continue;
                }

                var chave = new ChavePar(esquerda, direita);
                frequencias[chave] = frequencias.TryGetValue(chave, out var frequenciaAtual) ? frequenciaAtual + 1 : 1;
            }
        }

        return frequencias;
    }

    private static int ContarPartes(string simbolo)
    {
        return simbolo.Split(SeparadorMescla, StringSplitOptions.None).Length;
    }

    private static bool ContemPontuacao(string simbolo)
    {
        return simbolo
            .Split(SeparadorMescla, StringSplitOptions.None)
            .Any(EhPontuacao);
    }

    private static void AplicarMesclaEm(List<string> sequencia, RegraMescla regra)
    {
        if (sequencia.Count < 2)
        {
            return;
        }

        var indice = 0;
        while (indice < sequencia.Count - 1)
        {
            if (sequencia[indice] == regra.Esquerda && sequencia[indice + 1] == regra.Direita)
            {
                sequencia[indice] = regra.Mesclado;
                sequencia.RemoveAt(indice + 1);
            }
            else
            {
                indice++;
            }
        }
    }

    private static string DesfazerTokenizacao(IEnumerable<string> partes)
    {
        var resultado = "";

        foreach (var parte in partes)
        {
            if (string.IsNullOrWhiteSpace(parte))
            {
                continue;
            }

            if (resultado.Length == 0)
            {
                resultado = parte;
                continue;
            }

            resultado += EhPontuacao(parte) ? parte : " " + parte;
        }

        return resultado;
    }

    private static bool EhPontuacao(string token)
    {
        return token.Length == 1 && char.IsPunctuation(token[0]);
    }

    private readonly record struct ChavePar(string Esquerda, string Direita);
}

public sealed class ResultadoTreinoTokenizador
{
    public ResultadoTreinoTokenizador(ModeloTokenizador modelo, IReadOnlyList<IReadOnlyList<string>> sequenciasMescladas)
    {
        Modelo = modelo;
        SequenciasMescladas = sequenciasMescladas;
    }

    public ModeloTokenizador Modelo { get; }
    public IReadOnlyList<IReadOnlyList<string>> SequenciasMescladas { get; }
}
