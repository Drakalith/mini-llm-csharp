using System.Diagnostics;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Treinamento;

public static class ConstrutorExemplosTreino
{
    public static IReadOnlyList<ExemploTreino> Construir(
        IReadOnlyList<EntradaCorpus> corpus,
        Tokenizador tokenizador,
        int maximoComprimentoSequencia,
        Action<int, int, string>? aoAtualizarProgresso = null)
    {
        var exemplos = new List<ExemploTreino>();
        var cronometroAtualizacao = Stopwatch.StartNew();

        for (var indiceEntrada = 0; indiceEntrada < corpus.Count; indiceEntrada++)
        {
            var entrada = corpus[indiceEntrada];
            if (TentarSepararPerguntaResposta(entrada.Texto, out var pergunta, out var resposta))
            {
                var prompt = ConstruirPrompt(pergunta, entrada.Dominio);
                var idsPrompt = tokenizador.Codificar(prompt, adicionarInicio: true, adicionarFim: false);
                var textoCompletoPerguntaResposta = $"{prompt} {resposta}".Trim();
                TentarAdicionarExemplo(exemplos, tokenizador, entrada.Identificador, entrada.Dominio, textoCompletoPerguntaResposta, maximoComprimentoSequencia, idsPrompt.Count - 1);
            }
            else
            {
                var textoLivre = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Texto);
                if (!string.IsNullOrWhiteSpace(textoLivre))
                {
                    TentarAdicionarExemplo(exemplos, tokenizador, entrada.Identificador, entrada.Dominio, ComporTextoLivreComDominio(textoLivre, entrada.Dominio), maximoComprimentoSequencia);
                }
            }

            if (aoAtualizarProgresso is not null &&
                (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indiceEntrada == corpus.Count - 1))
            {
                aoAtualizarProgresso(indiceEntrada + 1, Math.Max(corpus.Count, 1), "montando exemplos de treino");
                cronometroAtualizacao.Restart();
            }
        }

        return exemplos;
    }

    public static string ConstruirPrompt(string perguntaUsuario, string? dominio = null)
    {
        var perguntaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(perguntaUsuario);
        if (!string.IsNullOrWhiteSpace(perguntaNormalizada) && !perguntaNormalizada.EndsWith("?", StringComparison.Ordinal))
        {
            perguntaNormalizada = $"{perguntaNormalizada} ?";
        }

        return $"{ConstruirPrefixoDominio(dominio)} pergunta : {perguntaNormalizada} resposta :".Trim();
    }

    public static string ComporTextoTreino(EntradaCorpus entrada)
    {
        if (TentarSepararPerguntaResposta(entrada.Texto, out var pergunta, out var resposta))
        {
            return $"{ConstruirPrompt(pergunta, entrada.Dominio)} {TreinadorTokenizadorBpe.NormalizarTexto(resposta)}".Trim();
        }

        var textoLivre = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Texto);
        return ComporTextoLivreComDominio(textoLivre, entrada.Dominio);
    }

    public static string ConstruirPrefixoDominio(string? dominio)
    {
        // O dominio segue disponivel como metadado interno, mas nao entra no texto treinado.
        return string.Empty;
    }

    public static string NormalizarDominioParaModelo(string? dominio)
    {
        if (string.IsNullOrWhiteSpace(dominio))
        {
            return string.Empty;
        }

        return dominio
            .Trim()
            .ToLowerInvariant()
            .Replace(' ', '_')
            .Replace('-', '_');
    }

    private static string ComporTextoLivreComDominio(string textoLivre, string dominio)
    {
        var prefixoDominio = ConstruirPrefixoDominio(dominio);
        return string.IsNullOrWhiteSpace(prefixoDominio)
            ? textoLivre
            : $"{prefixoDominio} {textoLivre}".Trim();
    }

    public static bool TentarSepararPerguntaResposta(string textoBruto, out string pergunta, out string resposta)
    {
        pergunta = string.Empty;
        resposta = string.Empty;

        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(textoBruto);
        const string marcadorResposta = "resposta :";

        var indiceResposta = textoNormalizado.IndexOf(marcadorResposta, StringComparison.Ordinal);
        if (indiceResposta < 0)
        {
            return false;
        }

        var esquerda = textoNormalizado[..indiceResposta].Trim();
        var direita = textoNormalizado[(indiceResposta + marcadorResposta.Length)..].Trim();

        const string marcadorPergunta = "pergunta :";
        if (!esquerda.StartsWith(marcadorPergunta, StringComparison.Ordinal))
        {
            return false;
        }

        pergunta = esquerda[marcadorPergunta.Length..].Trim();
        resposta = direita.Trim();

        return !string.IsNullOrWhiteSpace(pergunta) && !string.IsNullOrWhiteSpace(resposta);
    }

    private static void TentarAdicionarExemplo(List<ExemploTreino> exemplos, Tokenizador tokenizador, string identificador, string dominio, string textoCompleto, int maximoComprimentoSequencia, int quantidadeAlvosPrefixo = 0)
    {
        var idsCompletos = tokenizador.Codificar(textoCompleto, adicionarInicio: true, adicionarFim: true);
        if (idsCompletos.Count < 2)
        {
            return;
        }

        var pesosPerda = Enumerable.Repeat(1.0, idsCompletos.Count - 1).ToArray();
        for (var i = 0; i < quantidadeAlvosPrefixo && i < pesosPerda.Length; i++)
        {
            pesosPerda[i] = 0.0;
        }

        var quantidadePares = idsCompletos.Count - 1;
        var maximoParesPorExemplo = Math.Max(1, maximoComprimentoSequencia);

        if (quantidadePares <= maximoParesPorExemplo)
        {
            AdicionarExemplo(exemplos, identificador, dominio, textoCompleto, idsCompletos, pesosPerda);
            return;
        }

        var contextoCompartilhado = Math.Min(Math.Max(1, maximoParesPorExemplo / 4), maximoParesPorExemplo - 1);
        var passo = Math.Max(1, maximoParesPorExemplo - contextoCompartilhado);
        var indiceParte = 1;

        for (var inicioPar = 0; inicioPar < quantidadePares; inicioPar += passo)
        {
            var fimParExclusivo = Math.Min(inicioPar + maximoParesPorExemplo, quantidadePares);
            var idsFatia = idsCompletos.Skip(inicioPar).Take((fimParExclusivo - inicioPar) + 1).ToArray();
            var pesosFatia = pesosPerda.Skip(inicioPar).Take(fimParExclusivo - inicioPar).ToArray();

            if (inicioPar > 0)
            {
                var quantidadeContextoSemPerda = Math.Min(contextoCompartilhado, pesosFatia.Length);
                for (var i = 0; i < quantidadeContextoSemPerda; i++)
                {
                    pesosFatia[i] = 0.0;
                }
            }

            var identificadorParte = $"{identificador}/parte-{indiceParte:000}";
            var textoParte = tokenizador.Decodificar(idsFatia);
            AdicionarExemplo(exemplos, identificadorParte, dominio, textoParte, idsFatia, pesosFatia);

            indiceParte++;

            if (fimParExclusivo >= quantidadePares)
            {
                break;
            }
        }
    }

    private static void AdicionarExemplo(List<ExemploTreino> exemplos, string identificador, string dominio, string textoOriginal, IReadOnlyList<int> idsCompletos, IReadOnlyList<double> pesosPerda)
    {
        exemplos.Add(new ExemploTreino
        {
            Identificador = identificador,
            Dominio = dominio,
            TextoOriginal = textoOriginal,
            IdsCompletos = idsCompletos,
            IdsEntrada = idsCompletos.Take(idsCompletos.Count - 1).ToArray(),
            IdsAlvo = idsCompletos.Skip(1).ToArray(),
            PesosPerda = pesosPerda
        });
    }
}
