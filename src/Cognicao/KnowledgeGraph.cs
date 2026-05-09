using System.Text.Json;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class TriplaConhecimento
{
    public string Sujeito { get; init; } = string.Empty;
    public string Relacao { get; init; } = string.Empty;
    public string Objeto { get; init; } = string.Empty;
    public string Fonte { get; init; } = string.Empty;
    public string Dominio { get; init; } = string.Empty;
    public int Frequencia { get; set; } = 1;
}

public sealed class GrafoConhecimentoPersistido
{
    public string VersaoFormato { get; set; } = "1";
    public List<TriplaConhecimento> Triplas { get; init; } = new();
}

public sealed class GrafoConhecimento
{
    private static readonly Dictionary<string, string> RelacoesHumanizadas = new(StringComparer.Ordinal)
    {
        ["isa"] = "é",
        ["hasproperty"] = "tem",
        ["capableof"] = "pode",
        ["atlocation"] = "está em",
        ["partof"] = "é parte de",
        ["usedfor"] = "serve para",
        ["createdby"] = "foi criado por",
        ["relatedto"] = "está relacionado a"
    };

    private static readonly HashSet<string> SujeitosInvalidos = new(StringComparer.Ordinal)
    {
        "que", "quem", "ele", "ela", "eles", "elas", "isso", "isto", "aquilo",
        "eu", "tu", "nos", "vos", "o", "a", "os", "as", "um", "uma", "uns", "umas",
        "hoje", "ontem", "amanha", "agora", "sempre", "nunca", "ja", "ainda",
        "meu", "minha", "meus", "minhas", "teu", "tua", "seu", "sua", "seus", "suas",
        "nao", "sim", "talvez", "pois", "porque", "quando", "onde", "como"
    };

    private static readonly (Regex Regex, string Relacao)[] PadroesExtracao =
    [
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+é\s+um[as]?\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})\b"), "isa"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+é\s+(?:muito\s+)?([\wáàâãéèêíïóòôõöç]{4,}(?:mente)?)\b"), "hasproperty"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+(?:pode|consegue|sabe)\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "capableof"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+tem\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "hasproperty"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+(?:está|fica|vive)\s+(?:no|na|em|em um|em uma)\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "atlocation"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+(?:é\s+)?(?:parte\s+de|faz\s+parte\s+de)\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "partof"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+(?:serve\s+para|é\s+usad[oa]\s+para)\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "usedfor"),
        (CriarRegex(@"(\b[\wáàâãéèêíïóòôõöç]{3,})\s+(?:foi\s+)?(?:criad[oa]|inventad[oa]|descobert[oa])\s+(?:por|pela|pelo)\s+([\wáàâãéèêíïóòôõöç]+(?:\s+[\wáàâãéèêíïóòôõöç]+){1,4})"), "createdby")
    ];

    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _arcos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _arcosInversos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TriplaConhecimento> _indiceTriplas = new(StringComparer.Ordinal);

    public IReadOnlyCollection<TriplaConhecimento> Triplas => _indiceTriplas.Values;

    public void Adicionar(string sujeito, string relacao, string objeto, string fonte = "manual", string dominio = "geral")
    {
        var s = Limpar(sujeito);
        var r = Limpar(relacao);
        var o = Limpar(objeto);
        if (string.IsNullOrWhiteSpace(s) || string.IsNullOrWhiteSpace(r) || string.IsNullOrWhiteSpace(o))
        {
            return;
        }

        _arcos.TryAdd(s, new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));
        _arcos[s].TryAdd(r, new HashSet<string>(StringComparer.Ordinal));
        _arcos[s][r].Add(o);

        _arcosInversos.TryAdd(o, new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));
        _arcosInversos[o].TryAdd(r, new HashSet<string>(StringComparer.Ordinal));
        _arcosInversos[o][r].Add(s);

        var chave = CriarChaveTripla(s, r, o);
        if (_indiceTriplas.TryGetValue(chave, out var existente))
        {
            existente.Frequencia++;
            return;
        }

        _indiceTriplas[chave] = new TriplaConhecimento
        {
            Sujeito = s,
            Relacao = r,
            Objeto = o,
            Fonte = fonte,
            Dominio = dominio,
            Frequencia = 1
        };
    }

    public void GarantirTripla(TriplaConhecimento tripla)
    {
        var sujeito = Limpar(tripla.Sujeito);
        var relacao = Limpar(tripla.Relacao);
        var objeto = Limpar(tripla.Objeto);
        if (string.IsNullOrWhiteSpace(sujeito) ||
            string.IsNullOrWhiteSpace(relacao) ||
            string.IsNullOrWhiteSpace(objeto))
        {
            return;
        }

        _arcos.TryAdd(sujeito, new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));
        _arcos[sujeito].TryAdd(relacao, new HashSet<string>(StringComparer.Ordinal));
        _arcos[sujeito][relacao].Add(objeto);

        _arcosInversos.TryAdd(objeto, new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));
        _arcosInversos[objeto].TryAdd(relacao, new HashSet<string>(StringComparer.Ordinal));
        _arcosInversos[objeto][relacao].Add(sujeito);

        var chave = CriarChaveTripla(sujeito, relacao, objeto);
        if (_indiceTriplas.TryGetValue(chave, out var existente))
        {
            existente.Frequencia = Math.Max(existente.Frequencia, Math.Max(1, tripla.Frequencia));
            return;
        }

        _indiceTriplas[chave] = new TriplaConhecimento
        {
            Sujeito = sujeito,
            Relacao = relacao,
            Objeto = objeto,
            Fonte = tripla.Fonte,
            Dominio = tripla.Dominio,
            Frequencia = Math.Max(1, tripla.Frequencia)
        };
    }

    public IReadOnlyList<string> Buscar(string sujeito, string? relacao = null)
    {
        var s = Limpar(sujeito);
        if (!_arcos.TryGetValue(s, out var relacoes))
        {
            return Array.Empty<string>();
        }

        if (!string.IsNullOrWhiteSpace(relacao))
        {
            var r = Limpar(relacao);
            return relacoes.TryGetValue(r, out var objetos)
                ? objetos.OrderBy(x => x, StringComparer.Ordinal).ToList()
                : Array.Empty<string>();
        }

        return relacoes
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<string> BuscarReverso(string objeto, string? relacao = null)
    {
        var o = Limpar(objeto);
        if (!_arcosInversos.TryGetValue(o, out var relacoes))
        {
            return Array.Empty<string>();
        }

        if (!string.IsNullOrWhiteSpace(relacao))
        {
            var r = Limpar(relacao);
            return relacoes.TryGetValue(r, out var sujeitos)
                ? sujeitos.OrderBy(x => x, StringComparer.Ordinal).ToList()
                : Array.Empty<string>();
        }

        return relacoes
            .SelectMany(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<IReadOnlyList<TriplaConhecimento>> Caminhos(string origem, string destino, int maximoProfundidade = 3)
    {
        var origemNormalizada = Limpar(origem);
        var destinoNormalizado = Limpar(destino);
        if (string.Equals(origemNormalizada, destinoNormalizado, StringComparison.Ordinal))
        {
            return
            [
                new List<TriplaConhecimento>
                {
                    new()
                    {
                        Sujeito = origemNormalizada,
                        Relacao = "identico",
                        Objeto = destinoNormalizado,
                        Fonte = "runtime"
                    }
                }
            ];
        }

        var resultados = new List<IReadOnlyList<TriplaConhecimento>>();
        var fila = new Queue<(string Atual, List<TriplaConhecimento> Trajeto, HashSet<string> Visitados)>();
        fila.Enqueue((origemNormalizada, new List<TriplaConhecimento>(), new HashSet<string>(StringComparer.Ordinal)));

        while (fila.Count > 0)
        {
            var (atual, trajeto, visitados) = fila.Dequeue();
            if (trajeto.Count >= maximoProfundidade || !visitados.Add(atual))
            {
                continue;
            }

            if (_arcos.TryGetValue(atual, out var relacoes))
            {
                foreach (var relacao in relacoes)
                {
                    foreach (var objeto in relacao.Value)
                    {
                        var tripla = _indiceTriplas[CriarChaveTripla(atual, relacao.Key, objeto)];
                        var novoTrajeto = new List<TriplaConhecimento>(trajeto) { tripla };
                        if (string.Equals(objeto, destinoNormalizado, StringComparison.Ordinal))
                        {
                            resultados.Add(novoTrajeto);
                            if (resultados.Count >= 10)
                            {
                                return resultados;
                            }
                        }
                        else
                        {
                            fila.Enqueue((objeto, novoTrajeto, new HashSet<string>(visitados, StringComparer.Ordinal)));
                        }
                    }
                }
            }
        }

        return resultados;
    }

    public IReadOnlyList<TriplaConhecimento> ExtrairDeTexto(string texto, string fonte = "extraido", string dominio = "geral")
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        var triplas = new List<TriplaConhecimento>();

        foreach (var (regex, relacao) in PadroesExtracao)
        {
            foreach (Match match in regex.Matches(textoNormalizado))
            {
                if (!match.Success || match.Groups.Count < 3)
                {
                    continue;
                }

                var sujeito = Limpar(match.Groups[1].Value);
                var objeto = Limpar(match.Groups[2].Value);
                if (sujeito.Length <= 2 || objeto.Length <= 3)
                {
                    continue;
                }

                if (SujeitosInvalidos.Contains(sujeito))
                {
                    continue;
                }

                if (SujeitosInvalidos.Contains(objeto) || objeto.Length < sujeito.Length * 0.5)
                {
                    continue;
                }

                triplas.Add(new TriplaConhecimento
                {
                    Sujeito = sujeito,
                    Relacao = relacao,
                    Objeto = objeto,
                    Fonte = fonte,
                    Dominio = dominio
                });
            }
        }

        return triplas;
    }

    public int PopularDeTexto(string texto, string fonte = "extraido", string dominio = "geral")
    {
        var triplas = ExtrairDeTexto(texto, fonte, dominio);
        foreach (var tripla in triplas)
        {
            Adicionar(tripla.Sujeito, tripla.Relacao, tripla.Objeto, tripla.Fonte, tripla.Dominio);
        }

        return triplas.Count;
    }

    public void PopularDoCorpus(IEnumerable<EntradaCorpus> corpus)
    {
        foreach (var entrada in corpus)
        {
            PopularDeTexto(entrada.Texto, entrada.Identificador, entrada.Dominio);
        }
    }

    public void PopularDaMemoria(EstadoMemoriaConversacional? memoria)
    {
        if (memoria is null)
        {
            return;
        }

        foreach (var lembranca in memoria.LembrancasLongoPrazo)
        {
            PopularDeTexto(lembranca.Texto, "memoria-longa", "conversa");
        }

        foreach (var turno in memoria.Turnos.Where(x => x.Papel == "usuario"))
        {
            PopularDeTexto(turno.Texto, "turno-usuario", "conversa");
        }
    }

    public IReadOnlyList<string> RecuperarFatosRelacionados(IEnumerable<string> termos, int maximo = 6)
    {
        var conjunto = termos
            .Select(Limpar)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 2 && !SujeitosInvalidos.Contains(x))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (conjunto.Count == 0)
        {
            return Array.Empty<string>();
        }

        var candidatos = new Dictionary<string, TriplaConhecimento>(StringComparer.Ordinal);
        foreach (var termo in conjunto)
        {
            AdicionarCandidatosPorSujeito(termo, candidatos);
            AdicionarCandidatosPorObjeto(termo, candidatos);
        }

        var fonteBusca = candidatos.Count > 0
            ? candidatos.Values
            : _indiceTriplas.Count <= 2_000
                ? _indiceTriplas.Values
                : Enumerable.Empty<TriplaConhecimento>();

        return fonteBusca
            .Where(tripla => tripla.Frequencia >= 1 && tripla.Sujeito.Length > 2 && tripla.Objeto.Length > 3 && !SujeitosInvalidos.Contains(tripla.Sujeito))
            .Select(tripla => new
            {
                Texto = Humanizar(tripla),
                Pontuacao = Pontuar(tripla, conjunto)
            })
            .Where(x => x.Pontuacao > 0 && x.Texto.Length > 10 && !x.Texto.Contains("tem meu", StringComparison.OrdinalIgnoreCase) && !x.Texto.Contains("voce pode", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Texto, StringComparer.Ordinal)
            .Take(maximo)
            .Select(x => x.Texto)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private void AdicionarCandidatosPorSujeito(
        string sujeito,
        Dictionary<string, TriplaConhecimento> candidatos)
    {
        if (!_arcos.TryGetValue(sujeito, out var relacoes))
        {
            return;
        }

        foreach (var (relacao, objetos) in relacoes)
        {
            foreach (var objeto in objetos)
            {
                var chave = CriarChaveTripla(sujeito, relacao, objeto);
                if (_indiceTriplas.TryGetValue(chave, out var tripla))
                {
                    candidatos.TryAdd(chave, tripla);
                }
            }
        }
    }

    private void AdicionarCandidatosPorObjeto(
        string objeto,
        Dictionary<string, TriplaConhecimento> candidatos)
    {
        if (!_arcosInversos.TryGetValue(objeto, out var relacoes))
        {
            return;
        }

        foreach (var (relacao, sujeitos) in relacoes)
        {
            foreach (var sujeito in sujeitos)
            {
                var chave = CriarChaveTripla(sujeito, relacao, objeto);
                if (_indiceTriplas.TryGetValue(chave, out var tripla))
                {
                    candidatos.TryAdd(chave, tripla);
                }
            }
        }
    }

    public GrafoConhecimentoPersistido CriarPersistencia()
    {
        return new GrafoConhecimentoPersistido
        {
            Triplas = _indiceTriplas.Values
                .OrderBy(x => x.Sujeito, StringComparer.Ordinal)
                .ThenBy(x => x.Relacao, StringComparer.Ordinal)
                .ThenBy(x => x.Objeto, StringComparer.Ordinal)
                .ToList()
        };
    }

    public void Carregar(GrafoConhecimentoPersistido persistido)
    {
        foreach (var tripla in persistido.Triplas)
        {
            GarantirTripla(tripla);
        }
    }

    private static string Humanizar(TriplaConhecimento tripla)
    {
        var relacao = RelacoesHumanizadas.TryGetValue(tripla.Relacao, out var humanizada)
            ? humanizada
            : tripla.Relacao;
        return $"{tripla.Sujeito} {relacao} {tripla.Objeto}";
    }

    private static int Pontuar(TriplaConhecimento tripla, HashSet<string> termos)
    {
        var pontuacao = tripla.Frequencia;
        foreach (var termo in termos)
        {
            if (tripla.Sujeito.Contains(termo, StringComparison.Ordinal) ||
                termo.Contains(tripla.Sujeito, StringComparison.Ordinal))
            {
                pontuacao += 12;
            }

            if (tripla.Objeto.Contains(termo, StringComparison.Ordinal) ||
                termo.Contains(tripla.Objeto, StringComparison.Ordinal))
            {
                pontuacao += 8;
            }

            if (tripla.Relacao.Contains(termo, StringComparison.Ordinal))
            {
                pontuacao += 4;
            }
        }

        return pontuacao;
    }

    private static string CriarChaveTripla(string sujeito, string relacao, string objeto)
    {
        return $"{sujeito}|{relacao}|{objeto}";
    }

    private static string Limpar(string texto)
    {
        return Regex.Replace(texto.Trim().ToLowerInvariant(), @"\s+", " ");
    }

    private static Regex CriarRegex(string padrao)
    {
        return new Regex(padrao, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

public static class RepositorioGrafoConhecimento
{
    private const string NomeArquivo = "13_grafo_conhecimento.json";

    public static GrafoConhecimento CarregarOuReconstruir(
        string raizArtefatos,
        IReadOnlyList<EntradaCorpus> corpus,
        EstadoMemoriaConversacional? memoriaConversacional = null)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        var grafo = new GrafoConhecimento();

        if (File.Exists(caminho))
        {
            try
            {
                var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
                var persistido = JsonSerializer.Deserialize<GrafoConhecimentoPersistido>(json, PadroesSerializacao.CriarOpcoesJson());
                if (persistido is not null)
                {
                    grafo.Carregar(persistido);
                }
            }
            catch
            {
            }
        }

        foreach (var entrada in corpus)
        {
            foreach (var tripla in grafo.ExtrairDeTexto(entrada.Texto, entrada.Identificador, entrada.Dominio))
            {
                grafo.GarantirTripla(tripla);
            }
        }

        if (memoriaConversacional is not null)
        {
            foreach (var lembranca in memoriaConversacional.LembrancasLongoPrazo)
            {
                foreach (var tripla in grafo.ExtrairDeTexto(lembranca.Texto, "memoria-longa", "conversa"))
                {
                    grafo.GarantirTripla(tripla);
                }
            }

            foreach (var turno in memoriaConversacional.Turnos.Where(x => x.Papel == "usuario"))
            {
                foreach (var tripla in grafo.ExtrairDeTexto(turno.Texto, "turno-usuario", "conversa"))
                {
                    grafo.GarantirTripla(tripla);
                }
            }
        }

        Persistir(raizArtefatos, grafo);
        return grafo;
    }

    public static void Persistir(string raizArtefatos, GrafoConhecimento grafo)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        var json = JsonSerializer.Serialize(grafo.CriarPersistencia(), PadroesSerializacao.CriarOpcoesJson());
        File.WriteAllText(caminho, json, PadroesSerializacao.Utf8SemBom);
    }
}
