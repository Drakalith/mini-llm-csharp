using System.Text.RegularExpressions;
using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Ferramentas;

public sealed class MotorFerramentasContextuais
{
    private readonly string _raizProjeto;
    private readonly string _raizArtefatos;
    private readonly FerramentaSistema _ferramentaSistema = new();
    private readonly FerramentaLeituraArquivos _ferramentaLeituraArquivos = new();
    private readonly FerramentaBuscaArquivos _ferramentaBuscaArquivos = new();
    private readonly FerramentaBuscaWeb _ferramentaBuscaWeb = new();
    private readonly GrafoConhecimento _grafoConhecimento;

    public MotorFerramentasContextuais(
        string raizProjeto,
        string raizArtefatos,
        IReadOnlyList<EntradaCorpus> corpus,
        EstadoMemoriaConversacional? memoriaConversacional)
    {
        _raizProjeto = raizProjeto;
        _raizArtefatos = raizArtefatos;
        _grafoConhecimento = RepositorioGrafoConhecimento.CarregarOuReconstruir(
            raizArtefatos,
            corpus,
            memoriaConversacional);
    }

    public ContextoFerramentasConversacionais CriarContexto(string entradaUsuario)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var contexto = new ContextoFerramentasConversacionais();
        contexto.Blocos.AddRange(_ferramentaSistema.CriarBlocosTemporalESistema(entradaNormalizada, formasAtivas));

        foreach (var blocoArquivo in CriarBlocosDeArquivo(entradaUsuario, entradaNormalizada))
        {
            contexto.Blocos.Add(blocoArquivo);
        }

        foreach (var blocoWeb in CriarBlocosWeb(entradaUsuario, entradaNormalizada))
        {
            contexto.Blocos.Add(blocoWeb);
        }

        if (DeveUsarGrafoConhecimento(entradaNormalizada, formasAtivas))
        {
            var fatosRelacionados = _grafoConhecimento.RecuperarFatosRelacionados(formasAtivas, maximo: 3);
            if (fatosRelacionados.Count > 0)
            {
                contexto.Blocos.Add(new BlocoContextoFerramenta
                {
                    Origem = "grafo-conhecimento",
                    Rotulo = "fatos relacionados",
                    Texto = $"fatos relacionados : {string.Join(" | ", fatosRelacionados.Select(TreinadorTokenizadorBpe.NormalizarTexto))} ."
                });
            }
        }

        contexto.Evidencias.AddRange(contexto.Blocos.Select(x => $"{x.Origem}: {x.Rotulo}"));
        return contexto;
    }

    public void RegistrarTurnoUsuario(string texto)
    {
        if (_grafoConhecimento.PopularDeTexto(texto, "turno-usuario", "conversa") > 0)
        {
            RepositorioGrafoConhecimento.Persistir(_raizArtefatos, _grafoConhecimento);
        }
    }

    public void RegistrarTurnoModelo(string texto)
    {
        if (_grafoConhecimento.PopularDeTexto(texto, "turno-modelo", "conversa") > 0)
        {
            RepositorioGrafoConhecimento.Persistir(_raizArtefatos, _grafoConhecimento);
        }
    }

    private IEnumerable<BlocoContextoFerramenta> CriarBlocosDeArquivo(string entradaOriginal, string entradaNormalizada)
    {
        var referencia = ExtrairReferenciaArquivo(entradaOriginal);
        if (string.IsNullOrWhiteSpace(referencia))
        {
            yield break;
        }

        var candidatos = _ferramentaBuscaArquivos.LocalizarArquivos(_raizProjeto, referencia, maximo: 2);
        foreach (var caminho in candidatos)
        {
            var leitura = _ferramentaLeituraArquivos.LerTexto(caminho);
            if (leitura is null)
            {
                continue;
            }

            var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(leitura.Conteudo);
            if (string.IsNullOrWhiteSpace(textoNormalizado))
            {
                continue;
            }

            yield return new BlocoContextoFerramenta
            {
                Origem = "arquivo",
                Rotulo = Path.GetFileName(caminho),
                Texto = $"arquivo referenciado : nome {Path.GetFileName(caminho)} conteudo {textoNormalizado} ."
            };
        }

        if (candidatos.Count == 0 &&
            !string.IsNullOrWhiteSpace(referencia) &&
            entradaNormalizada.Contains("arquivo", StringComparison.Ordinal))
        {
            var ocorrencias = _ferramentaBuscaArquivos.BuscarConteudo(_raizProjeto, Regex.Escape(referencia), maximoArquivos: 8, maximoOcorrencias: 5);
            if (ocorrencias.Count > 0)
            {
                var resumo = string.Join(
                    " | ",
                    ocorrencias.Select(x => $"{Path.GetFileName(x.Caminho)} linha {x.Linha} {TreinadorTokenizadorBpe.NormalizarTexto(x.TextoLinha)}"));

                yield return new BlocoContextoFerramenta
                {
                    Origem = "arquivo",
                    Rotulo = "ocorrencias de texto",
                    Texto = $"ocorrencias em arquivos : {resumo} ."
                };
            }
        }
    }

    private IEnumerable<BlocoContextoFerramenta> CriarBlocosWeb(string entradaOriginal, string entradaNormalizada)
    {
        var url = ExtrairUrl(entradaOriginal);
        if (!string.IsNullOrWhiteSpace(url))
        {
            ConteudoPaginaWeb? pagina = null;
            try
            {
                pagina = _ferramentaBuscaWeb.BaixarPaginaAsync(url).GetAwaiter().GetResult();
            }
            catch
            {
                pagina = null;
            }

            if (pagina is not null && !string.IsNullOrWhiteSpace(pagina.TextoExtraido))
            {
                yield return new BlocoContextoFerramenta
                {
                    Origem = "web-fetch",
                    Rotulo = pagina.Titulo,
                    Texto = $"pagina web : url {pagina.Url} titulo {TreinadorTokenizadorBpe.NormalizarTexto(pagina.Titulo)} conteudo {TreinadorTokenizadorBpe.NormalizarTexto(pagina.TextoExtraido)} ."
                };
            }

            yield break;
        }

        if (!DevePesquisarNaWeb(entradaNormalizada))
        {
            yield break;
        }

        var consultaWeb = ExtrairConsultaWeb(entradaOriginal, entradaNormalizada);
        if (string.IsNullOrWhiteSpace(consultaWeb))
        {
            yield break;
        }

        IReadOnlyList<ResultadoBuscaWeb> resultados;
        try
        {
            resultados = _ferramentaBuscaWeb.PesquisarAsync(consultaWeb, maximo: 3).GetAwaiter().GetResult();
        }
        catch
        {
            resultados = Array.Empty<ResultadoBuscaWeb>();
        }

        if (resultados.Count == 0)
        {
            yield break;
        }

        var resumo = string.Join(
            " | ",
            resultados.Select(x =>
            {
                var partes = new List<string>
                {
                    TreinadorTokenizadorBpe.NormalizarTexto(x.Titulo),
                    TreinadorTokenizadorBpe.NormalizarTexto(x.Url)
                };

                if (!string.IsNullOrWhiteSpace(x.Resumo))
                {
                    partes.Add(TreinadorTokenizadorBpe.NormalizarTexto(x.Resumo));
                }

                return string.Join(" ", partes.Where(y => !string.IsNullOrWhiteSpace(y)));
            }));

        yield return new BlocoContextoFerramenta
        {
            Origem = "web-search",
            Rotulo = "resultados web",
            Texto = $"resultados web : consulta {TreinadorTokenizadorBpe.NormalizarTexto(consultaWeb)} . {resumo} ."
        };

        var primeiroResultado = resultados.FirstOrDefault();
        if (primeiroResultado is null || string.IsNullOrWhiteSpace(primeiroResultado.Url))
        {
            yield break;
        }

        ConteudoPaginaWeb? paginaResultado = null;
        try
        {
            paginaResultado = _ferramentaBuscaWeb.BaixarPaginaAsync(primeiroResultado.Url, maximoCaracteres: 1800).GetAwaiter().GetResult();
        }
        catch
        {
            paginaResultado = null;
        }

        if (paginaResultado is null || string.IsNullOrWhiteSpace(paginaResultado.TextoExtraido))
        {
            yield break;
        }

        yield return new BlocoContextoFerramenta
        {
            Origem = "web-fetch",
            Rotulo = string.IsNullOrWhiteSpace(paginaResultado.Titulo) ? primeiroResultado.Titulo : paginaResultado.Titulo,
            Texto = $"pagina web : consulta {TreinadorTokenizadorBpe.NormalizarTexto(consultaWeb)} . url {paginaResultado.Url} . titulo {TreinadorTokenizadorBpe.NormalizarTexto(paginaResultado.Titulo)} . conteudo {TreinadorTokenizadorBpe.NormalizarTexto(paginaResultado.TextoExtraido)} ."
        };
    }

    private static bool DevePesquisarNaWeb(string entradaNormalizada)
    {
        return entradaNormalizada.Contains("internet", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("na web", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("pesquise", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("site", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("artigo", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("noticia", StringComparison.Ordinal) ||
               entradaNormalizada.Contains("noticias", StringComparison.Ordinal);
    }

    private static bool DeveUsarGrafoConhecimento(string entradaNormalizada, IReadOnlyCollection<string> formasAtivas)
    {
        if (formasAtivas.Count == 0 || EhInteracaoSocialSimples(entradaNormalizada))
        {
            return false;
        }

        if (entradaNormalizada.Contains("lembra", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("o que sabe", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("fale sobre", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("me diga", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("explique", StringComparison.Ordinal))
        {
            return true;
        }

        if (!entradaNormalizada.Contains("?", StringComparison.Ordinal))
        {
            return formasAtivas.Count >= 4;
        }

        return formasAtivas.Count >= 2 &&
               formasAtivas.Any(forma => forma.Length >= 4);
    }

    private static bool EhInteracaoSocialSimples(string entradaNormalizada)
    {
        var texto = Regex.Replace(entradaNormalizada, @"\s+", " ", RegexOptions.CultureInvariant)
            .Trim(' ', '.', '!', '?', ',', ';', ':');

        if (string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        string[] padroesSociais =
        [
            "oi",
            "ola",
            "olá",
            "bom dia",
            "boa tarde",
            "boa noite",
            "e ai",
            "e aí",
            "salve",
            "tudo bem",
            "como vai",
            "como voce esta",
            "como você está",
            "como vc esta",
            "como vc está"
        ];

        return padroesSociais.Any(padrao =>
            string.Equals(texto, padrao, StringComparison.Ordinal) ||
            texto.StartsWith(padrao + " ", StringComparison.Ordinal));
    }

    private static string ExtrairConsultaWeb(string entradaOriginal, string entradaNormalizada)
    {
        var semUrl = Regex.Replace(entradaOriginal, @"https?://\S+", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var consulta = TreinadorTokenizadorBpe.NormalizarTexto(semUrl);

        consulta = Regex.Replace(
            consulta,
            @"\b(pesquise|procure|busque|veja|olhe|pesquisar|procurar|buscar)\b",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        consulta = Regex.Replace(
            consulta,
            @"\b(na internet|na web|na net|internet|web|site|sites|artigo|artigos|noticia|noticias)\b",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        consulta = Regex.Replace(
            consulta,
            @"\b(sobre|pra mim|para mim|por favor)\b",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        consulta = Regex.Replace(consulta, @"\s+", " ", RegexOptions.CultureInvariant).Trim(' ', '.', '?', '!', ':', ';', ',');

        return string.IsNullOrWhiteSpace(consulta)
            ? entradaNormalizada
            : consulta;
    }

    private static string ExtrairReferenciaArquivo(string entrada)
    {
        var match = Regex.Match(entrada, @"([A-Za-z0-9_\-./\\]+?\.(json|txt|md|cs|mmd))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return string.Empty;
    }

    private static string ExtrairUrl(string entrada)
    {
        var match = Regex.Match(entrada, @"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.TrimEnd('.', ',', ';', ')', ']') : string.Empty;
    }
}
