namespace MiniLlmDidatico.Dados;

using MiniLlmDidatico.Arquivos;

public static class CarregadorCorpus
{
    private static readonly System.Text.RegularExpressions.Regex RegexDominio =
        new(@"^\[\s*dom[ií]nio\s*:\s*(.+?)\s*\]$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex RegexPerguntaSemResposta =
        new(@"^pergunta\s*:\s*(.+?)\s*$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex RegexInicioBlocoRespostas =
        new(@"^\[\s*respostas?\s*(?::)?\s*\]?\s*$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly System.Text.RegularExpressions.Regex RegexFimBlocoRespostas =
        new(@"^(?:\]|\[\s*/\s*respostas?\s*\])$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static IReadOnlyList<EntradaCorpus> Carregar(string caminho)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException($"Corpus nao encontrado: {caminho}");
        }

        var extensao = Path.GetExtension(caminho);
        return extensao.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            ? CarregarTxt(caminho)
            : CarregarJson(caminho);
    }

    private static IReadOnlyList<EntradaCorpus> CarregarTxt(string caminho)
    {
        var entradas = new List<EntradaCorpus>();
        var linhas = File.ReadAllLines(caminho, PadroesSerializacao.Utf8SemBom);
        var dominioAtual = "geral";
        var perguntaPendente = string.Empty;
        var linhaPerguntaPendente = string.Empty;
        var emBlocoRespostas = false;
        var linhaInicioBlocoRespostas = -1;
        var quantidadeRespostasNoBloco = 0;

        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i].Trim();
            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (emBlocoRespostas)
            {
                if (RegexFimBlocoRespostas.IsMatch(linha))
                {
                    if (quantidadeRespostasNoBloco == 0)
                    {
                        throw new InvalidOperationException($"O bloco [respostas:] iniciado na linha {linhaInicioBlocoRespostas} nao contem nenhuma resposta valida.");
                    }

                    emBlocoRespostas = false;
                    perguntaPendente = string.Empty;
                    linhaPerguntaPendente = string.Empty;
                    quantidadeRespostasNoBloco = 0;
                    linhaInicioBlocoRespostas = -1;
                    continue;
                }

                if (RegexDominio.IsMatch(linha) ||
                    RegexInicioBlocoRespostas.IsMatch(linha) ||
                    EhPerguntaSemResposta(linha, out _))
                {
                    throw new InvalidOperationException($"Encontrada uma nova instrucao de corpus dentro de [respostas:] na linha {i + 1}. Feche o bloco com ] antes de abrir outro.");
                }

                var respostaNormalizada = NormalizarLinhaRespostaDeBloco(linha);
                if (string.IsNullOrWhiteSpace(respostaNormalizada))
                {
                    continue;
                }

                AdicionarEntrada(
                    entradas,
                    dominioAtual,
                    $"pergunta: {perguntaPendente} resposta: {respostaNormalizada}");
                quantidadeRespostasNoBloco++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(perguntaPendente) && RegexInicioBlocoRespostas.IsMatch(linha))
            {
                emBlocoRespostas = true;
                linhaInicioBlocoRespostas = i + 1;
                quantidadeRespostasNoBloco = 0;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(perguntaPendente))
            {
                AdicionarEntrada(entradas, dominioAtual, linhaPerguntaPendente);
                perguntaPendente = string.Empty;
                linhaPerguntaPendente = string.Empty;
            }

            var correspondenciaDominio = RegexDominio.Match(linha);
            if (correspondenciaDominio.Success)
            {
                dominioAtual = NormalizarDominio(correspondenciaDominio.Groups[1].Value);
                continue;
            }

            if (RegexInicioBlocoRespostas.IsMatch(linha))
            {
                throw new InvalidOperationException($"Foi encontrado um bloco [respostas:] na linha {i + 1} sem uma pergunta imediatamente antes.");
            }

            if (EhPerguntaSemResposta(linha, out var perguntaSemResposta))
            {
                perguntaPendente = perguntaSemResposta;
                linhaPerguntaPendente = linha;
                continue;
            }

            AdicionarEntrada(entradas, dominioAtual, linha);
        }

        if (emBlocoRespostas)
        {
            throw new InvalidOperationException($"O bloco [respostas:] iniciado na linha {linhaInicioBlocoRespostas} nao foi fechado com ] ate o fim do arquivo.");
        }

        if (!string.IsNullOrWhiteSpace(perguntaPendente))
        {
            AdicionarEntrada(entradas, dominioAtual, linhaPerguntaPendente);
        }

        if (entradas.Count == 0)
        {
            throw new InvalidOperationException("O corpus.txt nao contem linhas validas para treino.");
        }

        return entradas;
    }

    private static void AdicionarEntrada(List<EntradaCorpus> entradas, string dominio, string texto)
    {
        entradas.Add(new EntradaCorpus
        {
            Identificador = $"linha-{entradas.Count + 1:000}",
            Dominio = dominio,
            Texto = texto
        });
    }

    private static bool EhPerguntaSemResposta(string linha, out string pergunta)
    {
        pergunta = string.Empty;

        if (linha.Contains("resposta:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var correspondencia = RegexPerguntaSemResposta.Match(linha);
        if (!correspondencia.Success)
        {
            return false;
        }

        pergunta = correspondencia.Groups[1].Value.Trim();
        return !string.IsNullOrWhiteSpace(pergunta);
    }

    private static string NormalizarLinhaRespostaDeBloco(string linha)
    {
        var resposta = linha.Trim();
        resposta = System.Text.RegularExpressions.Regex.Replace(
            resposta,
            @"^(?:[-*]\s+|\d+[.)]\s+)",
            string.Empty);

        return resposta.Trim();
    }

    private static IReadOnlyList<EntradaCorpus> CarregarJson(string caminho)
    {
        var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
        var opcoes = PadroesSerializacao.CriarOpcoesJson(
            writeIndented: true,
            propertyNameCaseInsensitive: true);

        var entradas = System.Text.Json.JsonSerializer.Deserialize<List<EntradaCorpus>>(json, opcoes)
                      ?? throw new InvalidOperationException("Falha ao desserializar corpus.json");

        foreach (var entrada in entradas)
        {
            entrada.Dominio = NormalizarDominio(entrada.Dominio);
        }

        return entradas;
    }

    private static string NormalizarDominio(string dominio)
    {
        if (string.IsNullOrWhiteSpace(dominio))
        {
            return "geral";
        }

        var dominioNormalizado = System.Text.RegularExpressions.Regex.Replace(
            dominio.Trim().ToLowerInvariant(),
            @"\s+",
            " ");

        return string.IsNullOrWhiteSpace(dominioNormalizado)
            ? "geral"
            : dominioNormalizado;
    }
}
