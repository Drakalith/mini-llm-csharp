using System.Text.RegularExpressions;
using MiniLlmDidatico.Ferramentas;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public static class RespondedorBaseadoEmEvidencias
{
    public static string TentarResponder(
        AnalisePromptEpistemica analisePrompt,
        MemoriaEpistemica memoria)
    {
        var respostaFactual = TentarResponderComFerramentas(analisePrompt);
        if (!string.IsNullOrWhiteSpace(respostaFactual))
        {
            return TreinadorTokenizadorBpe.FormatarTextoParaExibicao(respostaFactual);
        }

        var respostaPerguntaResposta = TentarResponderPerguntaResposta(analisePrompt, memoria);
        if (!string.IsNullOrWhiteSpace(respostaPerguntaResposta))
        {
            return TreinadorTokenizadorBpe.FormatarTextoParaExibicao(respostaPerguntaResposta);
        }

        return TreinadorTokenizadorBpe.FormatarTextoParaExibicao(
            TentarResponderDialogoExemplificado(analisePrompt, memoria));
    }

    public static bool PodePersistirResposta(
        AnalisePromptEpistemica analisePrompt,
        MemoriaEpistemica memoria,
        string resposta)
    {
        if (GestorMemoriaConversacional.TextoEstaDegradado(resposta))
        {
            return false;
        }

        if (!SintetizadorRespostaContextual.RespostaPareceDesalinhada(analisePrompt.EntradaOriginal, resposta))
        {
            return true;
        }

        var respostaEvidenciada = TentarResponder(analisePrompt, memoria);
        return !string.IsNullOrWhiteSpace(respostaEvidenciada) &&
               string.Equals(
                   TreinadorTokenizadorBpe.NormalizarTexto(respostaEvidenciada),
                   TreinadorTokenizadorBpe.NormalizarTexto(resposta),
                   StringComparison.Ordinal);
    }

    private static string TentarResponderComFerramentas(AnalisePromptEpistemica analisePrompt)
    {
        if (!SintetizadorRespostaContextual.TemEvidenciaObservacionalForte(analisePrompt.ContextosFerramenta))
        {
            return string.Empty;
        }

        return SintetizadorRespostaContextual.TentarSintetizar(
            analisePrompt.EntradaOriginal,
            analisePrompt.ContextosFerramenta);
    }

    private static string TentarResponderPerguntaResposta(
        AnalisePromptEpistemica analisePrompt,
        MemoriaEpistemica memoria)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(analisePrompt.EntradaOriginal);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (formasAtivas.Count == 0)
        {
            return string.Empty;
        }

        var melhorResposta = memoria.Entradas
            .Where(x => x.Tipo == "pergunta-resposta" && !string.IsNullOrWhiteSpace(x.Resposta))
            .Select(x => new
            {
                Entrada = x,
                Pontuacao = PontuarPerguntaResposta(analisePrompt, formasAtivas, x)
            })
            .Where(x => x.Pontuacao >= 30)
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Entrada.Resposta.Length)
            .Select(x => x.Entrada.Resposta)
            .FirstOrDefault();

        return melhorResposta ?? string.Empty;
    }

    private static string TentarResponderDialogoExemplificado(
        AnalisePromptEpistemica analisePrompt,
        MemoriaEpistemica memoria)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(analisePrompt.EntradaOriginal);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var melhorResposta = memoria.Entradas
            .Where(x => x.Tipo == "texto-livre")
            .Select(x => ExtrairParDialogo(x.TextoNormalizado))
            .Where(x => x is not null)
            .Select(x => new
            {
                Par = x!,
                Pontuacao = PontuarDialogo(analisePrompt, entradaNormalizada, formasAtivas, x!)
            })
            .Where(x => x.Pontuacao >= 35)
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Par.Resposta.Length)
            .Select(x => x.Par.Resposta)
            .FirstOrDefault();

        return melhorResposta ?? string.Empty;
    }

    private static int PontuarPerguntaResposta(
        AnalisePromptEpistemica analisePrompt,
        HashSet<string> formasAtivas,
        EntradaMemoriaEpistemica entrada)
    {
        var perguntaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Pergunta);
        var formasPergunta = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(perguntaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (formasPergunta.Count == 0)
        {
            return 0;
        }

        var intersecao = formasPergunta.Intersect(formasAtivas, StringComparer.Ordinal).Count();
        if (intersecao == 0)
        {
            return 0;
        }

        var pontuacao = intersecao * 20;
        var comparavelEntrada = NormalizarComparavel(analisePrompt.EntradaOriginal);
        var comparavelPergunta = NormalizarComparavel(entrada.Pergunta);

        if (comparavelEntrada == comparavelPergunta)
        {
            pontuacao += 80;
        }
        else if (comparavelPergunta.Contains(comparavelEntrada, StringComparison.Ordinal) ||
                 comparavelEntrada.Contains(comparavelPergunta, StringComparison.Ordinal))
        {
            pontuacao += 30;
        }

        if (formasAtivas.IsSubsetOf(formasPergunta))
        {
            pontuacao += 30;
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.DominioPredominante) &&
            string.Equals(analisePrompt.DominioPredominante, entrada.Dominio, StringComparison.Ordinal))
        {
            pontuacao += 15;
        }

        return pontuacao;
    }

    private static int PontuarDialogo(
        AnalisePromptEpistemica analisePrompt,
        string entradaNormalizada,
        HashSet<string> formasAtivas,
        ParDialogoExemplificado dialogo)
    {
        var formasDialogo = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(dialogo.Pergunta)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var intersecao = formasDialogo.Intersect(formasAtivas, StringComparer.Ordinal).Count();
        var pontuacao = intersecao * 18;

        var comparavelEntrada = NormalizarComparavel(entradaNormalizada);
        var comparavelPergunta = NormalizarComparavel(dialogo.Pergunta);

        if (comparavelEntrada == comparavelPergunta)
        {
            pontuacao += 80;
        }
        else if (comparavelPergunta.Contains(comparavelEntrada, StringComparison.Ordinal) ||
                 comparavelEntrada.Contains(comparavelPergunta, StringComparison.Ordinal))
        {
            pontuacao += 25;
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.DominioPredominante) &&
            string.Equals(analisePrompt.DominioPredominante, dialogo.Dominio, StringComparison.Ordinal))
        {
            pontuacao += 10;
        }

        return pontuacao;
    }

    private static ParDialogoExemplificado? ExtrairParDialogo(string textoNormalizado)
    {
        var correspondencia = Regex.Match(
            textoNormalizado,
            @"(?:usuario|usuário)\s*:\s*(.+?)\s*(?:oria|ôria)\s*:\s*(.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        if (!correspondencia.Success)
        {
            return null;
        }

        var pergunta = correspondencia.Groups[1].Value.Trim();
        var resposta = correspondencia.Groups[2].Value.Trim();

        if (string.IsNullOrWhiteSpace(pergunta) || string.IsNullOrWhiteSpace(resposta))
        {
            return null;
        }

        return new ParDialogoExemplificado
        {
            Pergunta = pergunta,
            Resposta = resposta,
            Dominio = "exemplos-de-resposta"
        };
    }

    private static string NormalizarComparavel(string texto)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        var semPontuacao = new string(textoNormalizado
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray());

        return Regex.Replace(semPontuacao, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private sealed class ParDialogoExemplificado
    {
        public string Pergunta { get; init; } = string.Empty;
        public string Resposta { get; init; } = string.Empty;
        public string Dominio { get; init; } = string.Empty;
    }
}
