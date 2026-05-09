using System.Diagnostics;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;
using MiniLlmDidatico.Treinamento;

namespace MiniLlmDidatico.Cognicao;

public sealed class MemoriaEpistemica
{
    public List<EntradaMemoriaEpistemica> Entradas { get; init; } = new();
    public List<FormaEpistemica> Formas { get; init; } = new();
    public List<LigacaoEpistemica> Ligacoes { get; init; } = new();
    public HashSet<string> IndiceFormas { get; init; } = new(StringComparer.Ordinal);
}

public sealed class EntradaMemoriaEpistemica
{
    public string Identificador { get; init; } = string.Empty;
    public string Dominio { get; init; } = "geral";
    public string Tipo { get; init; } = string.Empty;
    public string TextoNormalizado { get; init; } = string.Empty;
    public string Pergunta { get; init; } = string.Empty;
    public string Resposta { get; init; } = string.Empty;
    public List<string> Formas { get; init; } = new();
}

public sealed class FormaEpistemica
{
    public string Texto { get; init; } = string.Empty;
    public int Frequencia { get; init; }
}

public sealed class LigacaoEpistemica
{
    public string Origem { get; init; } = string.Empty;
    public string Destino { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public int Forca { get; init; }
}

public sealed class EntradaUsuarioInterpretada
{
    public string EntradaOriginal { get; init; } = string.Empty;
    public string TextoNormalizado { get; init; } = string.Empty;
    public string TipoPragmatica { get; init; } = string.Empty;
    public string TextoNuclear { get; init; } = string.Empty;
    public string PromptBase { get; init; } = string.Empty;
}

public sealed class AnalisePromptEpistemica
{
    public string EntradaOriginal { get; init; } = string.Empty;
    public string TipoPragmatica { get; init; } = string.Empty;
    public string TipoApoioPredominante { get; init; } = string.Empty;
    public string DominioPredominante { get; init; } = string.Empty;
    public string NomeIdentidade { get; init; } = string.Empty;
    public string AmbienteBase { get; init; } = string.Empty;
    public string TempoDeVidaTexto { get; init; } = string.Empty;
    public string PromptBase { get; init; } = string.Empty;
    public string PromptEnriquecido { get; init; } = string.Empty;
    public List<string> FormasAtivas { get; init; } = new();
    public List<string> FormasForaDaMemoria { get; init; } = new();
    public List<string> SimbolosDesconhecidos { get; init; } = new();
    public List<string> TrechosRecuperados { get; init; } = new();
    public List<string> MemoriaCurta { get; init; } = new();
    public List<string> MemoriaMedia { get; init; } = new();
    public List<string> MemoriaLonga { get; init; } = new();
    public List<string> ContextosFerramenta { get; init; } = new();
    public EstadoCognitivoTurno? EstadoCognitivo { get; init; }
    public List<LigacaoEpistemica> SignosRelevantes { get; init; } = new();
    public List<string> CorrecoesSugeridas { get; init; } = new();
    public string PerguntaCuriosaSugerida { get; init; } = string.Empty;
    public string MotivoReescrita { get; init; } = string.Empty;
    public double ApoioNoCorpus { get; init; }
}

public static class InterpretadorEntradaUsuario
{
    public static EntradaUsuarioInterpretada Interpretar(string entradaUsuario)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        if (string.IsNullOrWhiteSpace(textoNormalizado))
        {
            return new EntradaUsuarioInterpretada
            {
                EntradaOriginal = entradaUsuario,
                TextoNormalizado = string.Empty,
                TipoPragmatica = "vazio",
                TextoNuclear = string.Empty,
                PromptBase = string.Empty
            };
        }

        const string prefixoTextoLivre = "texto :";
        if (textoNormalizado.StartsWith(prefixoTextoLivre, StringComparison.Ordinal))
        {
            var textoNuclear = textoNormalizado[prefixoTextoLivre.Length..].Trim();
            return new EntradaUsuarioInterpretada
            {
                EntradaOriginal = entradaUsuario,
                TextoNormalizado = textoNormalizado,
                TipoPragmatica = "texto-livre",
                TextoNuclear = textoNuclear,
                PromptBase = textoNuclear
            };
        }

        if (textoNormalizado.StartsWith("pergunta :", StringComparison.Ordinal) ||
            textoNormalizado.Contains("resposta :", StringComparison.Ordinal))
        {
            return new EntradaUsuarioInterpretada
            {
                EntradaOriginal = entradaUsuario,
                TextoNormalizado = textoNormalizado,
                TipoPragmatica = "estrutura-explicita",
                TextoNuclear = textoNormalizado,
                PromptBase = textoNormalizado
            };
        }

        return new EntradaUsuarioInterpretada
        {
            EntradaOriginal = entradaUsuario,
            TextoNormalizado = textoNormalizado,
            TipoPragmatica = "pergunta",
            TextoNuclear = textoNormalizado,
            PromptBase = ConstrutorExemplosTreino.ConstruirPrompt(textoNormalizado)
        };
    }
}

public static class ConstrutorMemoriaEpistemica
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "a", "ao", "aos", "as", "com", "da", "das", "de", "do", "dos", "e", "em",
        "na", "nas", "no", "nos", "o", "os", "para", "pergunta", "por", "qual",
        "quais", "que", "quem", "como", "onde", "quando", "resposta", "se", "sem",
        "texto", "um", "uma", "umas", "uns"
    };

    public static MemoriaEpistemica Construir(
        IReadOnlyList<EntradaCorpus> corpus,
        Action<int, int, string>? aoAtualizarProgresso = null)
    {
        var entradas = new List<EntradaMemoriaEpistemica>();
        var frequenciasFormas = new Dictionary<string, int>(StringComparer.Ordinal);
        var ligacoes = new Dictionary<string, int>(StringComparer.Ordinal);
        var cronometroAtualizacao = Stopwatch.StartNew();

        for (var indiceEntrada = 0; indiceEntrada < corpus.Count; indiceEntrada++)
        {
            var entrada = corpus[indiceEntrada];
            var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Texto);
            var formas = ExtrairFormasConteudo(textoNormalizado);
            if (formas.Count == 0)
            {
                if (aoAtualizarProgresso is not null &&
                    (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indiceEntrada == corpus.Count - 1))
                {
                    aoAtualizarProgresso(indiceEntrada + 1, Math.Max(corpus.Count, 1), "organizando memoria epistemica");
                    cronometroAtualizacao.Restart();
                }
                continue;
            }

            var tipo = "texto-livre";
            var pergunta = string.Empty;
            var resposta = string.Empty;

            if (ConstrutorExemplosTreino.TentarSepararPerguntaResposta(textoNormalizado, out pergunta, out resposta))
            {
                tipo = "pergunta-resposta";
            }

            foreach (var forma in formas)
            {
                frequenciasFormas[forma] = frequenciasFormas.TryGetValue(forma, out var atual) ? atual + 1 : 1;
            }

            AdicionarLigacoesSequenciais(formas, "sequencia", ligacoes);

            if (tipo == "pergunta-resposta")
            {
                var formasPergunta = ExtrairFormasConteudo(pergunta);
                var formasResposta = ExtrairFormasConteudo(resposta);

                foreach (var formaPergunta in formasPergunta.Distinct())
                {
                    foreach (var formaResposta in formasResposta.Distinct())
                    {
                        AdicionarLigacao(formaPergunta, formaResposta, "pergunta-resposta", 3, ligacoes);
                    }
                }
            }

            entradas.Add(new EntradaMemoriaEpistemica
            {
                Identificador = entrada.Identificador,
                Dominio = entrada.Dominio,
                Tipo = tipo,
                TextoNormalizado = textoNormalizado,
                Pergunta = pergunta,
                Resposta = resposta,
                Formas = formas.Distinct().ToList()
            });

            if (aoAtualizarProgresso is not null &&
                (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indiceEntrada == corpus.Count - 1))
            {
                aoAtualizarProgresso(indiceEntrada + 1, Math.Max(corpus.Count, 1), "organizando memoria epistemica");
                cronometroAtualizacao.Restart();
            }
        }

        return new MemoriaEpistemica
        {
            Entradas = entradas,
            Formas = frequenciasFormas
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new FormaEpistemica
                {
                    Texto = x.Key,
                    Frequencia = x.Value
                })
                .ToList(),
            Ligacoes = ligacoes
                .Select(x =>
                {
                    var partes = x.Key.Split('|');
                    return new LigacaoEpistemica
                    {
                        Origem = partes[0],
                        Destino = partes[1],
                        Tipo = partes[2],
                        Forca = x.Value
                    };
                })
                .OrderByDescending(x => x.Forca)
                .ThenBy(x => x.Tipo, StringComparer.Ordinal)
                .ThenBy(x => x.Origem, StringComparer.Ordinal)
                .ToList(),
            IndiceFormas = frequenciasFormas.Keys.ToHashSet(StringComparer.Ordinal)
        };
    }

    public static List<string> ExtrairFormasConteudo(string texto)
    {
        return TreinadorTokenizadorBpe.PreTokenizar(texto)
            .Select(parte => parte.Trim())
            .Where(parte => !string.IsNullOrWhiteSpace(parte))
            .Where(parte => !(parte.Length == 1 && char.IsPunctuation(parte[0])))
            .Where(parte => !Stopwords.Contains(parte))
            .ToList();
    }

    private static void AdicionarLigacoesSequenciais(IReadOnlyList<string> formas, string tipo, Dictionary<string, int> ligacoes)
    {
        for (var i = 0; i < formas.Count - 1; i++)
        {
            if (formas[i] == formas[i + 1])
            {
                continue;
            }

            AdicionarLigacao(formas[i], formas[i + 1], tipo, 1, ligacoes);
        }
    }

    private static void AdicionarLigacao(string origem, string destino, string tipo, int incremento, Dictionary<string, int> ligacoes)
    {
        var chave = $"{origem}|{destino}|{tipo}";
        ligacoes[chave] = ligacoes.TryGetValue(chave, out var atual) ? atual + incremento : incremento;
    }
}

public sealed class AnalisadorEpistemicoPrompt
{
    private readonly MemoriaEpistemica _memoria;
    private readonly Tokenizador _tokenizador;

    public AnalisadorEpistemicoPrompt(MemoriaEpistemica memoria, Tokenizador tokenizador)
    {
        _memoria = memoria;
        _tokenizador = tokenizador;
    }

    public AnalisePromptEpistemica Analisar(string entradaUsuario)
    {
        var interpretada = InterpretadorEntradaUsuario.Interpretar(entradaUsuario);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(interpretada.TextoNuclear);
        var conjuntoFormasAtivas = formasAtivas.ToHashSet(StringComparer.Ordinal);
        var dominioPredominante = DeterminarDominioPredominante(conjuntoFormasAtivas);

        var entradasTextoLivre = _memoria.Entradas
            .Where(x => x.Tipo == "texto-livre");

        if (!string.IsNullOrWhiteSpace(dominioPredominante))
        {
            var entradasMesmoDominio = entradasTextoLivre
                .Where(x => x.Dominio == dominioPredominante)
                .ToList();

            if (entradasMesmoDominio.Count > 0)
            {
                entradasTextoLivre = entradasMesmoDominio;
            }
        }

        var trechosRecuperados = entradasTextoLivre
            .Select(x => new
            {
                x.TextoNormalizado,
                Pontuacao = PontuarEntrada(conjuntoFormasAtivas, x.Formas)
            })
            .Where(x => x.Pontuacao > 0)
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.TextoNormalizado, StringComparer.Ordinal)
            .Take(3)
            .Select(x => x.TextoNormalizado)
            .ToList();

        var signosRelevantes = _memoria.Ligacoes
            .Where(x => conjuntoFormasAtivas.Contains(x.Origem))
            .OrderByDescending(x => x.Forca)
            .ThenBy(x => x.Tipo, StringComparer.Ordinal)
            .Take(5)
            .ToList();

        var formasForaDaMemoria = formasAtivas
            .Where(x => !_memoria.IndiceFormas.Contains(x))
            .Distinct()
            .ToList();

        var simbolosDesconhecidos = _tokenizador.ListarSimbolosDesconhecidos(interpretada.PromptBase).ToList();
        var forcaPerguntaResposta = _memoria.Ligacoes
            .Where(x => x.Tipo == "pergunta-resposta" && conjuntoFormasAtivas.Contains(x.Origem))
            .Sum(x => x.Forca);
        var forcaTextoLivre = trechosRecuperados.Count > 0 ? 1 : 0;
        var tipoApoioPredominante = forcaPerguntaResposta > 0
            ? "pergunta-resposta"
            : forcaTextoLivre > 0
                ? "texto-livre"
                : "fraco";

        var promptReescrito = TentarReescreverPromptAPartirDaMemoria(
            interpretada,
            trechosRecuperados,
            simbolosDesconhecidos,
            forcaPerguntaResposta,
            out var motivoReescrita);

        var promptBaseComDominio = AplicarDominioAoPrompt(interpretada, dominioPredominante);
        var prefixoDominio = ConstrutorExemplosTreino.ConstruirPrefixoDominio(dominioPredominante);

        var correcoes = new List<string>();

        if (simbolosDesconhecidos.Count > 0)
        {
            correcoes.Add("reescrever o prompt com formas ja presentes no corpus");
        }

        if (!string.IsNullOrWhiteSpace(promptReescrito))
        {
            correcoes.Add("trocar o prompt por uma trilha conhecida da memoria");
        }
        else if (trechosRecuperados.Count > 0)
        {
            correcoes.Add("usar memoria recuperada como contexto antes da geracao");
        }
        else if (formasAtivas.Count > 0 && forcaPerguntaResposta == 0)
        {
            correcoes.Add("falta apoio direto no corpus para essa combinacao de formas");
        }

        var promptEnriquecido = !string.IsNullOrWhiteSpace(promptReescrito)
            ? AplicarPrefixoDominioEmTextoLivre(promptReescrito, dominioPredominante)
            : trechosRecuperados.Count == 0
                ? promptBaseComDominio
                : string.IsNullOrWhiteSpace(prefixoDominio)
                    ? $"{string.Join(" ", trechosRecuperados)} {promptBaseComDominio}".Trim()
                    : $"{prefixoDominio} {string.Join(" ", trechosRecuperados)} {RemoverPrefixoDominio(promptBaseComDominio, prefixoDominio)}".Trim();

        var apoioNoCorpus = Math.Min(
            1.0,
            (trechosRecuperados.Count / 3.0) + (forcaPerguntaResposta > 0 ? 1.0 : 0.0));

        return new AnalisePromptEpistemica
        {
            EntradaOriginal = entradaUsuario,
            TipoPragmatica = interpretada.TipoPragmatica,
            TipoApoioPredominante = tipoApoioPredominante,
            DominioPredominante = dominioPredominante,
            PromptBase = promptBaseComDominio,
            PromptEnriquecido = promptEnriquecido,
            FormasAtivas = formasAtivas.Distinct().ToList(),
            FormasForaDaMemoria = formasForaDaMemoria,
            SimbolosDesconhecidos = simbolosDesconhecidos,
            TrechosRecuperados = trechosRecuperados,
            SignosRelevantes = signosRelevantes,
            CorrecoesSugeridas = correcoes,
            MotivoReescrita = motivoReescrita,
            ApoioNoCorpus = apoioNoCorpus
        };
    }

    private static int PontuarEntrada(HashSet<string> formasAtivas, IReadOnlyCollection<string> formasEntrada)
    {
        if (formasAtivas.Count == 0 || formasEntrada.Count == 0)
        {
            return 0;
        }

        var comuns = formasEntrada.Count(formasAtivas.Contains);
        if (comuns == 0)
        {
            return 0;
        }

        var bonusCobertura = formasAtivas.All(formasEntrada.Contains) ? 20 : 0;
        return comuns * 10 + bonusCobertura - formasEntrada.Count;
    }

    private static string AplicarDominioAoPrompt(EntradaUsuarioInterpretada interpretada, string dominioPredominante)
    {
        return interpretada.TipoPragmatica switch
        {
            "pergunta" => ConstrutorExemplosTreino.ConstruirPrompt(interpretada.TextoNuclear, dominioPredominante),
            "texto-livre" => AplicarPrefixoDominioEmTextoLivre(interpretada.TextoNuclear, dominioPredominante),
            "estrutura-explicita" => AplicarPrefixoDominioEmTextoLivre(interpretada.PromptBase, dominioPredominante),
            _ => interpretada.PromptBase
        };
    }

    private static string AplicarPrefixoDominioEmTextoLivre(string texto, string dominioPredominante)
    {
        var prefixo = ConstrutorExemplosTreino.ConstruirPrefixoDominio(dominioPredominante);
        return string.IsNullOrWhiteSpace(prefixo)
            ? texto
            : $"{prefixo} {texto}".Trim();
    }

    private static string RemoverPrefixoDominio(string texto, string prefixo)
    {
        if (string.IsNullOrWhiteSpace(prefixo) || string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        return texto.StartsWith(prefixo + " ", StringComparison.Ordinal)
            ? texto[(prefixo.Length + 1)..]
            : texto;
    }

    private string DeterminarDominioPredominante(HashSet<string> formasAtivas)
    {
        if (formasAtivas.Count == 0)
        {
            return string.Empty;
        }

        return _memoria.Entradas
            .Select(x => new
            {
                x.Dominio,
                Pontuacao = PontuarEntrada(formasAtivas, x.Formas)
            })
            .Where(x => x.Pontuacao > 0)
            .GroupBy(x => x.Dominio, StringComparer.Ordinal)
            .Select(x => new
            {
                Dominio = x.Key,
                Pontuacao = x.Sum(y => y.Pontuacao)
            })
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Dominio, StringComparer.Ordinal)
            .Select(x => x.Dominio)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string TentarReescreverPromptAPartirDaMemoria(
        EntradaUsuarioInterpretada interpretada,
        IReadOnlyList<string> trechosRecuperados,
        IReadOnlyList<string> simbolosDesconhecidos,
        int forcaPerguntaResposta,
        out string motivoReescrita)
    {
        motivoReescrita = string.Empty;

        if (interpretada.TipoPragmatica != "pergunta")
        {
            return string.Empty;
        }

        if (trechosRecuperados.Count == 0 || forcaPerguntaResposta > 0)
        {
            return string.Empty;
        }

        var prefixo = ExtrairPrefixoComum(trechosRecuperados);
        if (prefixo.Count < 2)
        {
            return string.Empty;
        }

        if (prefixo.Count > 0 && prefixo[^1].Length == 1 && char.IsPunctuation(prefixo[^1][0]))
        {
            prefixo.RemoveAt(prefixo.Count - 1);
        }

        if (prefixo.Count < 2)
        {
            return string.Empty;
        }

        motivoReescrita = "pergunta fora do padrao foi convertida para continuacao apoiada pela memoria";
        return Destokenizar(prefixo);
    }

    private static List<string> ExtrairPrefixoComum(IReadOnlyList<string> trechosRecuperados)
    {
        if (trechosRecuperados.Count == 0)
        {
            return new List<string>();
        }

        var tokenizados = trechosRecuperados
            .Select(TreinadorTokenizadorBpe.PreTokenizar)
            .Where(x => x.Count > 0)
            .ToList();

        if (tokenizados.Count == 0)
        {
            return new List<string>();
        }

        var prefixo = new List<string>(tokenizados[0]);
        foreach (var trecho in tokenizados.Skip(1))
        {
            var limite = Math.Min(prefixo.Count, trecho.Count);
            var indice = 0;
            while (indice < limite && prefixo[indice] == trecho[indice])
            {
                indice++;
            }

            prefixo = prefixo.Take(indice).ToList();
            if (prefixo.Count == 0)
            {
                break;
            }
        }

        return prefixo;
    }

    private static string Destokenizar(IEnumerable<string> partes)
    {
        var resultado = string.Empty;

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

            resultado += parte.Length == 1 && char.IsPunctuation(parte[0]) ? parte : " " + parte;
        }

        return resultado;
    }
}
