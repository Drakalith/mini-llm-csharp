using System.Text.Json;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Diagnostico;

namespace MiniLlmDidatico.Cognicao;

public sealed class VetorConceitual
{
    public string Conceito { get; init; } = string.Empty;
    public ulong AssinaturaAlta { get; set; }
    public ulong AssinaturaBaixa { get; set; }
    public List<string> TermosAssociados { get; init; } = new();
    public int Frequencia { get; set; } = 1;
    public DateTimeOffset UltimaAtivacao { get; set; }
    public string CategoriaEmergente { get; set; } = string.Empty;
}

public sealed class EstadoMatrizConceitual
{
    public string VersaoFormato { get; set; } = "1";
    public List<VetorConceitual> Vetores { get; init; } = new();
    public List<string> CategoriasEmergentes { get; init; } = new();
}

public static class MatrizConceitual
{
    private const string NomeArquivo = "17_matriz_conceitual.json";
    private const int TotalBits = 128;

    public static List<VetorConceitual> CarregarOuCriar(string raizArtefatos)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);

        if (File.Exists(caminho))
        {
            try
            {
                var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
                var estado = JsonSerializer.Deserialize<EstadoMatrizConceitual>(json, PadroesSerializacao.CriarOpcoesJson());
                if (estado is not null && estado.Vetores.Count > 0)
                    return estado.Vetores;
            }
            catch { }
        }

        var vetores = CriarVetoresVazios();
        Persistir(raizArtefatos, vetores, new List<string>());
        return vetores;
    }

    public static void Persistir(string raizArtefatos, List<VetorConceitual> vetores, List<string> categorias)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        var estado = new EstadoMatrizConceitual { Vetores = vetores, CategoriasEmergentes = categorias };
        var json = JsonSerializer.Serialize(estado, PadroesSerializacao.CriarOpcoesJson());
        File.WriteAllText(caminho, json, PadroesSerializacao.Utf8SemBom);
    }

    private static List<VetorConceitual> CriarVetoresVazios()
    {
        return new List<VetorConceitual>();
    }

    public static (ulong alta, ulong baixa) GerarAssinatura(string texto)
    {
        ulong alta = 0;
        ulong baixa = 0;
        var textoLower = texto.ToLowerInvariant();

        for (int i = 0; i < textoLower.Length; i++)
        {
            int hash = (textoLower[i] * 31 + i) & 0x7F;
            if (hash < 64)
                alta |= (1UL << hash);
            else
                baixa |= (1UL << (hash - 64));
        }

        var bigrams = System.Text.RegularExpressions.Regex.Matches(textoLower, @"\w{2,4}");
        foreach (System.Text.RegularExpressions.Match m in bigrams)
        {
            int hash = HashString(m.Value);
            if (hash < 64)
                alta |= (1UL << hash);
            else
                baixa |= (1UL << (hash - 64));
        }

        return (alta, baixa);
    }

    public static VetorConceitual? EncontrarOuCriar(string termo, List<VetorConceitual> vetores)
    {
        var existente = vetores.FirstOrDefault(v =>
            v.Conceito.Equals(termo, StringComparison.OrdinalIgnoreCase) ||
            v.TermosAssociados.Any(t => t.Equals(termo, StringComparison.OrdinalIgnoreCase)));

        if (existente is not null)
        {
            existente.Frequencia++;
            existente.UltimaAtivacao = RelogioBrasil.AgoraBrasilSp();
            return existente;
        }

        var (alta, baixa) = GerarAssinatura(termo);
        var novo = new VetorConceitual
        {
            Conceito = termo.ToLowerInvariant(),
            AssinaturaAlta = alta,
            AssinaturaBaixa = baixa,
            TermosAssociados = new List<string> { termo.ToLowerInvariant() },
            UltimaAtivacao = RelogioBrasil.AgoraBrasilSp()
        };

        vetores.Add(novo);
        return novo;
    }

    public static void AssociarTermos(string conceitoBase, string novoTermo, List<VetorConceitual> vetores)
    {
        var vetor = vetores.FirstOrDefault(v => v.Conceito.Equals(conceitoBase, StringComparison.OrdinalIgnoreCase));
        if (vetor is not null && !vetor.TermosAssociados.Contains(novoTermo, StringComparer.OrdinalIgnoreCase))
        {
            vetor.TermosAssociados.Add(novoTermo.ToLowerInvariant());
            var (alta, baixa) = GerarAssinatura(novoTermo);
            vetor.AssinaturaAlta |= alta;
            vetor.AssinaturaBaixa |= baixa;
        }
    }

    public static double Similaridade(ulong a1, ulong a2, ulong b1, ulong b2)
    {
        var xorAlta = a1 ^ a2;
        var xorBaixa = b1 ^ b2;
        var coincidencias = TotalBits - BitCount(xorAlta) - BitCount(xorBaixa);
        return (double)coincidencias / TotalBits;
    }

    private static int BitCount(ulong valor)
    {
        int count = 0;
        while (valor > 0)
        {
            count += (int)(valor & 1);
            valor >>= 1;
        }
        return count;
    }

    public static List<VetorConceitual> EncontrarRelacionados(string texto, List<VetorConceitual> vetores, double limiar = 0.4)
    {
        var (alta, baixa) = GerarAssinatura(texto);

        return vetores
            .Select(v => new { Vetor = v, Sim = Similaridade(alta, v.AssinaturaAlta, baixa, v.AssinaturaBaixa) })
            .Where(x => x.Sim >= limiar)
            .OrderByDescending(x => x.Sim)
            .Select(x => x.Vetor)
            .ToList();
    }

    public static string InferirCategoria(string texto, List<VetorConceitual> vetores, List<string> categorias)
    {
        var relacionados = EncontrarRelacionados(texto, vetores, 0.3);
        if (relacionados.Count == 0) return "geral";

        var termos = relacionados.SelectMany(v => v.TermosAssociados).Distinct().ToList();
        foreach (var cat in categorias)
        {
            if (termos.Any(t => t.Contains(cat, StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains(t, StringComparison.OrdinalIgnoreCase)))
                return cat;
        }

        return "geral";
    }

    public static bool SaoRelacionados(string texto1, string texto2, List<VetorConceitual> vetores, double limiar = 0.5)
    {
        var sig1 = GerarAssinatura(texto1);
        var sig2 = GerarAssinatura(texto2);
        return Similaridade(sig1.alta, sig2.alta, sig1.baixa, sig2.baixa) >= limiar;
    }

    private static int HashString(string s)
    {
        int hash = 0;
        foreach (char c in s)
            hash = (hash * 31 + c) & 0x7F;
        return hash;
    }
}
