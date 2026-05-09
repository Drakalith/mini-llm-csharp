using System.Text.Json;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class EntradaVocabularioDinamico
{
    public string Palavra { get; init; } = string.Empty;
    public string Decomposicao { get; init; } = string.Empty;
    public List<string> ContextosUso { get; init; } = new();
    public int Frequencia { get; set; } = 1;
    public DateTimeOffset PrimeiroRegistro { get; init; }
    public DateTimeOffset UltimaOcorrencia { get; set; }
    public double ConfiancaDecomposicao { get; set; }
}

public sealed class EstadoVocabularioDinamico
{
    public string VersaoFormato { get; set; } = "1";
    public List<EntradaVocabularioDinamico> Entradas { get; init; } = new();
}

public static class VocabularioDinamico
{
    private const string NomeArquivo = "15_vocabulario_dinamico.json";
    private static readonly JsonSerializerOptions OpcoesJson = PadroesSerializacao.CriarOpcoesJson(
        writeIndented: true,
        propertyNameCaseInsensitive: true);

    public static EstadoVocabularioDinamico Carregar(string raizArtefatos)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        if (!File.Exists(caminho))
        {
            return new EstadoVocabularioDinamico();
        }

        try
        {
            var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
            return JsonSerializer.Deserialize<EstadoVocabularioDinamico>(json, OpcoesJson)
                   ?? new EstadoVocabularioDinamico();
        }
        catch
        {
            return new EstadoVocabularioDinamico();
        }
    }

    public static void Persistir(string raizArtefatos, EstadoVocabularioDinamico estado)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        var json = JsonSerializer.Serialize(estado, OpcoesJson);
        File.WriteAllText(caminho, json, PadroesSerializacao.Utf8SemBom);
    }

    public static List<string> DetectarPalavrasNovas(
        string texto,
        ModeloTokenizador modelo,
        EstadoVocabularioDinamico estado)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        var palavrasNovas = new List<string>();

        var tokens = TreinadorTokenizadorBpe.PreTokenizar(textoNormalizado);
        foreach (var token in tokens)
        {
            var tokenLimpo = token.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(tokenLimpo) || tokenLimpo.Length < 3)
            {
                continue;
            }

            if (char.IsPunctuation(tokenLimpo[0]) || char.IsPunctuation(tokenLimpo[^1]))
            {
                continue;
            }

            var jaConhecida = modelo.Vocabulario.ContainsKey(tokenLimpo);
            var jaRegistrada = estado.Entradas.Any(x =>
                string.Equals(x.Palavra, tokenLimpo, StringComparison.Ordinal));

            if (!jaConhecida && !jaRegistrada)
            {
                palavrasNovas.Add(tokenLimpo);
            }
        }

        return palavrasNovas.Distinct(StringComparer.Ordinal).ToList();
    }

    public static EstadoVocabularioDinamico RegistrarPalavrasNovas(
        EstadoVocabularioDinamico estado,
        List<string> palavrasNovas,
        string contexto)
    {
        var agora = RelogioBrasil.AgoraBrasilSp();

        foreach (var palavra in palavrasNovas)
        {
            var existente = estado.Entradas.FirstOrDefault(x =>
                string.Equals(x.Palavra, palavra, StringComparison.Ordinal));

            if (existente is null)
            {
                var decomposicao = TentarDecompor(palavra, estado);
                estado.Entradas.Add(new EntradaVocabularioDinamico
                {
                    Palavra = palavra,
                    Decomposicao = decomposicao,
                    ContextosUso = { contexto },
                    Frequencia = 1,
                    PrimeiroRegistro = agora,
                    UltimaOcorrencia = agora,
                    ConfiancaDecomposicao = string.IsNullOrWhiteSpace(decomposicao) ? 0.0 : 0.5
                });
            }
            else
            {
                existente.Frequencia++;
                existente.UltimaOcorrencia = agora;
                if (!existente.ContextosUso.Contains(contexto, StringComparer.Ordinal))
                {
                    existente.ContextosUso.Add(contexto);
                    if (existente.ContextosUso.Count > 8)
                    {
                        existente.ContextosUso.RemoveAt(0);
                    }
                }
            }
        }

        if (estado.Entradas.Count > 512)
        {
            var excesso = estado.Entradas
                .OrderBy(x => x.Frequencia)
                .ThenBy(x => x.UltimaOcorrencia)
                .Take(estado.Entradas.Count - 512)
                .ToList();

            foreach (var item in excesso)
            {
                estado.Entradas.Remove(item);
            }
        }

        return estado;
    }

    public static string ObterRepresentacao(string palavra, EstadoVocabularioDinamico estado)
    {
        var entrada = estado.Entradas.FirstOrDefault(x =>
            string.Equals(x.Palavra, palavra, StringComparison.Ordinal));

        if (entrada is null)
        {
            return palavra;
        }

        if (!string.IsNullOrWhiteSpace(entrada.Decomposicao))
        {
            return entrada.Decomposicao;
        }

        return palavra;
    }

    public static string EnriquecerTextoComVocabulario(
        string texto,
        EstadoVocabularioDinamico estado)
    {
        var tokens = TreinadorTokenizadorBpe.PreTokenizar(texto);
        var partes = new List<string>();

        foreach (var token in tokens)
        {
            var tokenLimpo = token.Trim().ToLowerInvariant();
            var representacao = ObterRepresentacao(tokenLimpo, estado);
            partes.Add(representacao);
        }

        return string.Join(" ", partes.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string TentarDecompor(string palavra, EstadoVocabularioDinamico estado)
    {
        if (palavra.Length < 4)
        {
            return string.Empty;
        }

        for (var tamanho = 2; tamanho <= palavra.Length / 2; tamanho++)
        {
            var parte1 = palavra[..tamanho];
            var parte2 = palavra[tamanho..];

            var parte1Conhecida = estado.Entradas.Any(x =>
                string.Equals(x.Palavra, parte1, StringComparison.Ordinal));
            var parte2Conhecida = estado.Entradas.Any(x =>
                string.Equals(x.Palavra, parte2, StringComparison.Ordinal));

            if (parte1Conhecida || parte2Conhecida)
            {
                return $"{parte1} {parte2}";
            }
        }

        return string.Empty;
    }
}
