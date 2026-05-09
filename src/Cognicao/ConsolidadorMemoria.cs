using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Diagnostico;

namespace MiniLlmDidatico.Cognicao;

public sealed record ResultadoConsolidacao
{
    public int MemoriasMescladas { get; init; }
    public int MemoriasDecaidas { get; init; }
    public int MemoriasRemovidas { get; init; }
    public int FatosNovosConsolidados { get; init; }
    public List<string> Alteracoes { get; init; } = new();
}

public static class ConsolidadorMemoria
{
    public static ResultadoConsolidacao Consolidar(
        GestorMemoriaConversacional gestorMemoria,
        CortexConversacional cortex,
        GrafoConhecimento grafo,
        EstadoVocabularioDinamico vocabulario,
        int diasDecaimento = 7,
        int frequenciaMinimaConsolidacao = 3)
    {
        var resultado = new ResultadoConsolidacao();
        var agora = RelogioBrasil.AgoraBrasilSp();
        var alteracoes = new List<string>();

        resultado = MesclarLembrancasSimilares(gestorMemoria, agora, alteracoes);
        resultado = resultado with
        {
            MemoriasDecaidas = resultado.MemoriasDecaidas + AplicarDecaimentoTemporal(gestorMemoria, agora, diasDecaimento, alteracoes),
            MemoriasRemovidas = resultado.MemoriasRemovidas + RemoverMemoriasFracas(gestorMemoria, frequenciaMinimaConsolidacao, alteracoes)
        };

        resultado = resultado with
        {
            FatosNovosConsolidados = ConsolidarFatosGrafo(grafo, alteracoes)
        };

        resultado = resultado with
        {
            Alteracoes = alteracoes.Distinct(StringComparer.Ordinal).Take(20).ToList()
        };

        return resultado;
    }

    private static ResultadoConsolidacao MesclarLembrancasSimilares(
        GestorMemoriaConversacional gestor,
        DateTimeOffset agora,
        List<string> alteracoes)
    {
        var estado = gestor.Estado;
        var mescladas = 0;

        var grupos = estado.LembrancasLongoPrazo
            .GroupBy(x => ConstruirChaveSemanticaAgrupada(x.Texto))
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var grupo in grupos)
        {
            var maisForte = grupo.OrderByDescending(x => x.Forca).First();
            var outras = grupo.Where(x => x != maisForte).ToList();

            foreach (var outra in outras)
            {
                maisForte.Texto = MesclarTextos(maisForte.Texto, outra.Texto);
                maisForte.Forca += outra.Forca;
                maisForte.UltimaAtualizacaoBrasilSp = agora;
                estado.LembrancasLongoPrazo.Remove(outra);
                mescladas++;
                alteracoes.Add($"mesclada: '{outra.Texto[..Math.Min(30, outra.Texto.Length)]}' -> '{maisForte.Texto[..Math.Min(30, maisForte.Texto.Length)]}'");
            }
        }

        return new ResultadoConsolidacao
        {
            MemoriasMescladas = mescladas,
            MemoriasDecaidas = 0,
            MemoriasRemovidas = 0,
            FatosNovosConsolidados = 0,
            Alteracoes = alteracoes
        };
    }

    private static int AplicarDecaimentoTemporal(
        GestorMemoriaConversacional gestor,
        DateTimeOffset agora,
        int diasDecaimento,
        List<string> alteracoes)
    {
        var estado = gestor.Estado;
        var decaidas = 0;

        foreach (var lembranca in estado.LembrancasLongoPrazo)
        {
            var diasDesdeAtualizacao = (agora - lembranca.UltimaAtualizacaoBrasilSp).TotalDays;
            if (diasDesdeAtualizacao > diasDecaimento)
            {
                var fatorDecaimento = Math.Max(0.1, 1.0 - (diasDesdeAtualizacao - diasDecaimento) / 30.0);
                var forcaAnterior = lembranca.Forca;
                lembranca.Forca = (int)Math.Max(1, lembranca.Forca * fatorDecaimento);
                if (lembranca.Forca < forcaAnterior)
                {
                    decaidas++;
                    if (decaidas <= 5)
                    {
                        alteracoes.Add($"decaimento: '{lembranca.Texto[..Math.Min(30, lembranca.Texto.Length)]}' forca {forcaAnterior} -> {lembranca.Forca}");
                    }
                }
            }
        }

        return decaidas;
    }

    private static int RemoverMemoriasFracas(
        GestorMemoriaConversacional gestor,
        int frequenciaMinima,
        List<string> alteracoes)
    {
        var estado = gestor.Estado;
        var removidas = 0;
        var agora = RelogioBrasil.AgoraBrasilSp();

        var fracas = estado.LembrancasLongoPrazo
            .Where(x => x.Forca < frequenciaMinima)
            .Where(x => (agora - x.PrimeiroRegistroBrasilSp).TotalHours > 2)
            .ToList();

        foreach (var fraca in fracas)
        {
            estado.LembrancasLongoPrazo.Remove(fraca);
            removidas++;
            if (removidas <= 3)
            {
                alteracoes.Add($"removida (fraca): '{fraca.Texto[..Math.Min(30, fraca.Texto.Length)]}' forca={fraca.Forca}");
            }
        }

        return removidas;
    }

    private static int ConsolidarFatosGrafo(GrafoConhecimento grafo, List<string> alteracoes)
    {
        var consolidados = 0;

        var triplasFrequentes = grafo.Triplas
            .Where(x => x.Frequencia >= 2)
            .OrderByDescending(x => x.Frequencia)
            .Take(50)
            .ToList();

        foreach (var tripla in triplasFrequentes)
        {
            tripla.Frequencia = Math.Max(tripla.Frequencia, 2);
            consolidados++;
        }

        if (consolidados > 0)
        {
            alteracoes.Add($"{consolidados} triplas do grafo consolidadas por frequencia");
        }

        return consolidados;
    }

    private static string ConstruirChaveSemanticaAgrupada(string texto)
    {
        var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(texto)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Take(4)
            .ToList();

        return string.Join("|", formas);
    }

    private static string MesclarTextos(string texto1, string texto2)
    {
        if (texto1.Contains(texto2, StringComparison.OrdinalIgnoreCase))
        {
            return texto1;
        }

        if (texto2.Contains(texto1, StringComparison.OrdinalIgnoreCase))
        {
            return texto2;
        }

        var formas1 = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(texto1).ToHashSet(StringComparer.Ordinal);
        var formas2 = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(texto2).ToHashSet(StringComparer.Ordinal);

        var formasComuns = formas1.Intersect(formas2, StringComparer.Ordinal).Count();
        var totalFormas = formas1.Union(formas2, StringComparer.Ordinal).Count();

        if (totalFormas > 0 && formasComuns / (double)totalFormas > 0.5)
        {
            return texto1.Length >= texto2.Length ? texto1 : texto2;
        }

        return $"{texto1.TrimEnd('.', ' ', '?', '!')}; {texto2.TrimStart(' ')}";
    }
}
