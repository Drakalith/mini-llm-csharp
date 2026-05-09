using System.Text.RegularExpressions;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class EloCadeiaRaciocinio
{
    public int Ordem { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Pergunta { get; init; } = string.Empty;
    public string Resposta { get; init; } = string.Empty;
    public double Confianca { get; init; }
    public bool BloqueiaGeracao { get; init; }
}

public sealed class ResultadoCadeiaRaciocinio
{
    public List<EloCadeiaRaciocinio> Elos { get; init; } = new();
    public bool DeveGerarNeural { get; init; }
    public string RespostaDireta { get; init; } = string.Empty;
    public string PromptGuiado { get; init; } = string.Empty;
    public double ConfiancaGeral { get; init; }
    public string MotivoDecisao { get; init; } = string.Empty;
}

public static class CadeiaRaciocinio
{
    public static ResultadoCadeiaRaciocinio Executar(
        string entradaUsuario,
        AnalisePromptEpistemica analise,
        MemoriaEpistemica memoria,
        GestorMemoriaConversacional gestorMemoria,
        CortexConversacional? cortex,
        EstadoVocabularioDinamico vocabulario,
        GrafoConhecimento? grafo = null,
        List<VetorConceitual>? matrizConceitual = null,
        List<string>? categoriasEmergentes = null)
    {
        var elos = new List<EloCadeiaRaciocinio>();
        var ordem = 1;

        var respostaEvidencia = RespondedorBaseadoEmEvidencias.TentarResponder(analise, memoria);
        var respostaCognitiva = cortex?.TentarResponderComEstado(analise) ?? string.Empty;

        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(
            TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario));
        var fatosGrafo = grafo?.RecuperarFatosRelacionados(formasAtivas, maximo: 3)
            .Where(f => f.Length > 12 && f.Contains(" ", StringComparison.Ordinal))
            .ToList() ?? new List<string>();

        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        var respostaMemoriaLonga = BuscarMemoriaLongoPrazo(entradaNormalizada, gestorMemoria, matrizConceitual, categoriasEmergentes);

        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_grafo_conhecimento",
            Pergunta = "Ha fatos no grafo de conhecimento?",
            Resposta = fatosGrafo.Count > 0 ? $"sim: {string.Join(" | ", fatosGrafo.Take(3))}" : "nao",
            Confianca = fatosGrafo.Count > 0 ? 0.85 : 0.2,
            BloqueiaGeracao = false
        });

        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_corpus",
            Pergunta = "Existe resposta direta no corpus?",
            Resposta = !string.IsNullOrWhiteSpace(respostaEvidencia) ? $"sim: '{respostaEvidencia[..Math.Min(40, respostaEvidencia.Length)]}'" : "nao",
            Confianca = analise.ApoioNoCorpus,
            BloqueiaGeracao = false
        });

        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_memoria_experiencial",
            Pergunta = "Existe resposta na memoria experiencial?",
            Resposta = !string.IsNullOrWhiteSpace(respostaCognitiva) ? $"sim: '{respostaCognitiva[..Math.Min(40, respostaCognitiva.Length)]}'" : "nao",
            Confianca = cortex is not null ? 0.6 : 0.0,
            BloqueiaGeracao = false
        });

        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_memoria_longo_prazo",
            Pergunta = "Ha fatos na memoria de longo prazo?",
            Resposta = !string.IsNullOrWhiteSpace(respostaMemoriaLonga) ? $"sim: '{respostaMemoriaLonga[..Math.Min(50, respostaMemoriaLonga.Length)]}'" : "nao",
            Confianca = !string.IsNullOrWhiteSpace(respostaMemoriaLonga) ? 0.75 : 0.2,
            BloqueiaGeracao = false
        });

        var intencao = analise.EstadoCognitivo?.Intencao ?? string.Empty;
        var ehDeclaracao = intencao.Contains("informar", StringComparison.Ordinal);

        var palavrasNovas = formasAtivas
            .Where(f => f.Length >= 3 && !memoria.IndiceFormas.Contains(f))
            .ToList();

        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_vocabulario",
            Pergunta = "Ha palavras fora do vocabulario conhecido?",
            Resposta = palavrasNovas.Count > 0 ? $"sim: {string.Join(", ", palavrasNovas.Take(3))}" : "nao",
            Confianca = palavrasNovas.Count == 0 ? 0.9 : 0.4,
            BloqueiaGeracao = false
        });

        var temConflito = analise.EstadoCognitivo?.Conflitos.Count > 0;
        elos.Add(new EloCadeiaRaciocinio
        {
            Ordem = ordem++,
            Tipo = "verificacao_conflito",
            Pergunta = "Ha conflito de referencias?",
            Resposta = temConflito ? $"sim: {string.Join(", ", analise.EstadoCognitivo!.Conflitos.Take(2))}" : "nao",
            Confianca = temConflito ? 0.3 : 0.8,
            BloqueiaGeracao = temConflito && analise.EstadoCognitivo?.Conflitos.Any(x => x.Contains("sem correcao explicita", StringComparison.Ordinal)) == true
        });

        var deveGerarNeural = !elos.Any(x => x.BloqueiaGeracao);
        var respostaDireta = string.Empty;
        var promptGuiado = string.Empty;

        var respostaCognitivaEspecifica = !string.IsNullOrWhiteSpace(respostaCognitiva) &&
                                          !GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva) &&
                                          !respostaCognitiva.Contains("voce me disse", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("me deixa lembrar", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("me refresca", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("acho que", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("voce mencionou", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("voce falou", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("essa semana", StringComparison.OrdinalIgnoreCase) &&
                                          !respostaCognitiva.Contains("voce trabalha em", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(respostaMemoriaLonga) && !ehDeclaracao)
        {
            respostaDireta = respostaMemoriaLonga;
            deveGerarNeural = false;
        }
        else if (respostaCognitivaEspecifica)
        {
            respostaDireta = respostaCognitiva;
            deveGerarNeural = false;
        }
        else if (analise.ApoioNoCorpus >= 0.95 && !string.IsNullOrWhiteSpace(respostaEvidencia) && !ehDeclaracao)
        {
            respostaDireta = respostaEvidencia;
            deveGerarNeural = false;
        }
        else if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
                 !GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva))
        {
            respostaDireta = respostaCognitiva;
            deveGerarNeural = false;
        }
        else if (fatosGrafo.Count > 0 && !ehDeclaracao)
        {
            promptGuiado = $"Fatos conhecidos: {string.Join(". ", fatosGrafo)}. {analise.PromptEnriquecido}";
        }
        else if (!string.IsNullOrWhiteSpace(respostaEvidencia) && !ehDeclaracao)
        {
            promptGuiado = $"{respostaEvidencia} {analise.PromptEnriquecido}";
        }
        else
        {
            promptGuiado = analise.PromptEnriquecido;
        }

        var confiancaGeral = elos.Average(x => x.Confianca);
        var motivoDecisao = deveGerarNeural
            ? "nenhuma fonte direta bloqueou; geracao neural necessaria"
            : respostaDireta != string.Empty
                ? "resposta direta encontrada com confianca suficiente"
                : "resposta parcial encontrada; geracao guiada ativada";

        return new ResultadoCadeiaRaciocinio
        {
            Elos = elos,
            DeveGerarNeural = deveGerarNeural,
            RespostaDireta = respostaDireta,
            PromptGuiado = promptGuiado,
            ConfiancaGeral = confiancaGeral,
            MotivoDecisao = motivoDecisao
        };
    }

    private static string BuscarMemoriaLongoPrazo(string entradaNormalizada, GestorMemoriaConversacional gestorMemoria, List<VetorConceitual>? matrizConceitual, List<string>? categoriasEmergentes)
    {
        var formasEntrada = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Where(x => x.Length > 2 && !FormasGenericas.Contains(x))
            .ToHashSet(StringComparer.Ordinal);

        if (formasEntrada.Count == 0)
        {
            return string.Empty;
        }

        var candidatas = gestorMemoria.Estado.LembrancasLongoPrazo
            .Select(lembranca => new
            {
                Lembranca = lembranca,
                Pontuacao = PontuarMemoriaLongoPrazo(lembranca, formasEntrada, entradaNormalizada, matrizConceitual)
            })
            .Where(x => x.Pontuacao > 0)
            .OrderByDescending(x => x.Pontuacao)
            .ThenByDescending(x => x.Lembranca.Forca)
            .ThenByDescending(x => x.Lembranca.FrequenciaUso)
            .ToList();

        if (candidatas.Count == 0)
        {
            return string.Empty;
        }

        var melhor = candidatas.First();
        if (melhor.Pontuacao < 15)
        {
            return string.Empty;
        }

        var entidadePrincipal = melhor.Lembranca.EntidadePrincipal;
        if (!string.IsNullOrWhiteSpace(entidadePrincipal) && entidadePrincipal != "geral")
        {
            var memoriasRelacionadas = gestorMemoria.BuscarMemoriasPorEntidade(entidadePrincipal)
                .Where(m => m.Chave != melhor.Lembranca.Chave)
                .Take(2)
                .ToList();

            if (memoriasRelacionadas.Count > 0)
            {
                var textoPrincipal = TreinadorTokenizadorBpe.NormalizarTexto(melhor.Lembranca.Texto);
                var textosExtras = memoriasRelacionadas
                    .Select(m => TreinadorTokenizadorBpe.NormalizarTexto(m.Texto))
                    .ToList();

                return CombinarMemoriasRelacionadas(textoPrincipal, textosExtras, entradaNormalizada);
            }
        }

        var texto = TreinadorTokenizadorBpe.NormalizarTexto(melhor.Lembranca.Texto);
        return FormatarRespostaMemoria(texto, entradaNormalizada);
    }

    private static string CombinarMemoriasRelacionadas(string textoPrincipal, List<string> textosExtras, string entradaNormalizada)
    {
        var partes = new List<string> { FormatarRespostaMemoria(textoPrincipal, entradaNormalizada) };

        foreach (var textoExtra in textosExtras)
        {
            var formatado = FormatarRespostaMemoria(textoExtra, entradaNormalizada);
            if (!string.IsNullOrWhiteSpace(formatado) && !partes.Contains(formatado, StringComparer.Ordinal))
            {
                partes.Add(formatado);
            }
        }

        return string.Join(". ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string ConverterPronomes(string texto)
    {
        var regras = RegrasMorfologicas.Carregar();
        var palavras = texto.Split(' ');
        var resultado = new List<string>();

        for (int i = 0; i < palavras.Length; i++)
        {
            var palavra = palavras[i];
            var limpa = palavra.TrimEnd('.', ',', '!', '?', ';', ':', ')', ']');
            var pontuacao = palavra.Substring(limpa.Length);

            if (regras.MapaPronomes.TryGetValue(limpa, out var substituto))
            {
                resultado.Add(substituto + pontuacao);
            }
            else
            {
                resultado.Add(palavra);
            }
        }

        return string.Join(" ", resultado);
    }

    private static string ConverterVerbos(string texto)
    {
        var regras = RegrasMorfologicas.Carregar();
        var palavras = texto.Split(' ');
        var resultado = new List<string>();

        for (int i = 0; i < palavras.Length; i++)
        {
            var palavra = palavras[i];
            var limpa = palavra.TrimEnd('.', ',', '!', '?', ';', ':', ')', ']');
            var pontuacao = palavra.Substring(limpa.Length);
            var anterior = i > 0 ? palavras[i - 1].TrimEnd('.', ',', '!', '?', ';', ':', ')', ']') : string.Empty;

            bool deveConjugar = anterior.Equals("voce", StringComparison.OrdinalIgnoreCase) ||
                                anterior.Equals("se", StringComparison.OrdinalIgnoreCase);

            if (deveConjugar && limpa.Length > 2)
            {
                if (regras.VerbosIrregularesPresente.TryGetValue(limpa, out var pres))
                {
                    resultado.Add(pres + pontuacao);
                    continue;
                }

                if (regras.VerbosIrregularesPretPerfeito.TryGetValue(limpa, out var pret))
                {
                    resultado.Add(pret + pontuacao);
                    continue;
                }
            }

            bool jaProcessado = false;
            foreach (var padrao in regras.PadroesConjugacao)
            {
                if (limpa.Length >= padrao.TamanhoMinimo &&
                    limpa.EndsWith(padrao.SufixoEntrada, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(padrao.SufixoExcluido) &&
                        limpa.EndsWith(padrao.SufixoExcluido, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (padrao.SufixoEntrada == padrao.SufixoSaida)
                    {
                        if (deveConjugar)
                        {
                            resultado.Add(palavra);
                            jaProcessado = true;
                            break;
                        }
                        continue;
                    }

                    if (padrao.RaizTerminacoes is not null)
                    {
                        var raiz = limpa.Substring(0, limpa.Length - padrao.SufixoEntrada.Length);
                        if (padrao.RaizTerminacoes.Any(t => raiz.EndsWith(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            var novoVerbo = raiz + padrao.SufixoSaida + pontuacao;
                            if (deveConjugar)
                            {
                                resultado.Add(novoVerbo);
                            }
                            else
                            {
                                resultado.Add(palavra);
                            }
                            jaProcessado = true;
                            break;
                        }
                    }
                    else if (padrao.RaizTerminacoesExcluidas is not null)
                    {
                        var raiz = limpa.Substring(0, limpa.Length - padrao.SufixoEntrada.Length);
                        if (!padrao.RaizTerminacoesExcluidas.Any(t => raiz.EndsWith(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            var novoVerbo = raiz + padrao.SufixoSaida + pontuacao;
                            if (deveConjugar)
                            {
                                resultado.Add(novoVerbo);
                            }
                            else
                            {
                                resultado.Add(palavra);
                            }
                            jaProcessado = true;
                            break;
                        }
                    }
                    else
                    {
                        var raiz = limpa.Substring(0, limpa.Length - padrao.SufixoEntrada.Length);
                        var novoVerbo = raiz + padrao.SufixoSaida + pontuacao;
                        if (deveConjugar)
                        {
                            resultado.Add(novoVerbo);
                        }
                        else
                        {
                            resultado.Add(palavra);
                        }
                        jaProcessado = true;
                        break;
                    }
                }
            }

            if (!jaProcessado)
            {
                resultado.Add(palavra);
            }
        }

        return string.Join(" ", resultado);
    }

    private static string FormatarRespostaMemoria(string textoMemoria, string entradaNormalizada)
    {
        var resposta = ConverterPronomes(textoMemoria);
        resposta = ConverterVerbos(resposta);

        resposta = Regex.Replace(resposta, @"\b, e e \b", ", ", RegexOptions.CultureInvariant);
        resposta = Regex.Replace(resposta, @"\b e e \b", " e ", RegexOptions.CultureInvariant);
        resposta = Regex.Replace(resposta, @"\be e \b", "e ", RegexOptions.CultureInvariant);

        return resposta;
    }

    private static readonly HashSet<string> FormasGenericas = new(StringComparer.Ordinal)
    {
        "eu", "meu", "minha", "meus", "minhas", "mim", "me", "comigo",
        "voce", "você", "te", "tu", "seu", "sua", "seus", "suas",
        "que", "o", "a", "os", "as", "um", "uma", "e", "ou", "nao", "sim",
        "faco", "faz", "fazer", "tem", "ter", "sao", "ser", "estar",
        "ele", "ela", "eles", "elas", "isso", "isto", "aquilo",
        "muito", "mais", "pouco", "bem", "mal", "ja", "ainda", "tambem",
        "onde", "quando", "como", "porque", "qual", "quais", "quem"
    };

    private static int PontuarMemoriaLongoPrazo(
        LembrancaConsolidada lembranca,
        HashSet<string> formasEntrada,
        string entradaNormalizada,
        List<VetorConceitual>? matrizConceitual)
    {
        var pontuacao = 0;
        var textoLower = lembranca.Texto.ToLowerInvariant();

        var overlap = formasEntrada.Count(f => textoLower.Contains(f));
        if (overlap == 0)
        {
            return 0;
        }

        pontuacao += overlap * 20;

        foreach (var forma in formasEntrada)
        {
            if (lembranca.FormasChave.Contains(forma, StringComparer.Ordinal))
            {
                pontuacao += 10;
            }
        }

        if (matrizConceitual is not null && matrizConceitual.Count > 0)
        {
            var sigEntrada = MatrizConceitual.GerarAssinatura(entradaNormalizada);
            var sigMemoria = MatrizConceitual.GerarAssinatura(lembranca.Texto);
            var similaridade = MatrizConceitual.Similaridade(sigEntrada.alta, sigMemoria.alta, sigEntrada.baixa, sigMemoria.baixa);
            pontuacao += (int)(similaridade * 50);
        }

        var diferencaTempo = RelogioBrasil.AgoraBrasilSp() - lembranca.UltimaAtualizacaoBrasilSp;
        if (diferencaTempo.TotalMinutes < 30) pontuacao += 15;
        else if (diferencaTempo.TotalHours < 2) pontuacao += 10;
        else if (diferencaTempo.TotalHours < 24) pontuacao += 5;

        if (lembranca.Recencia == "recente") pontuacao += 10;
        else if (lembranca.Recencia == "media") pontuacao += 5;

        return pontuacao;
    }
}
