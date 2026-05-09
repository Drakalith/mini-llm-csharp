using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Ferramentas;
using MiniLlmDidatico.Geracao;
using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Canal;

public sealed class ProcessadorRespostas
{
    private readonly ConfiguracaoAplicacao _configuracao;
    private readonly MiniTransformador _modelo;
    private readonly Gerador _gerador;
    private readonly Tokenizador _tokenizador;
    private readonly ModeloTokenizador _modeloTokenizador;
    private readonly MemoriaEpistemica _memoriaEpistemica;
    private readonly string _diretorioArtefatos;

    public ProcessadorRespostas(
        ConfiguracaoAplicacao configuracao,
        MiniTransformador modelo,
        Gerador gerador,
        Tokenizador tokenizador,
        ModeloTokenizador modeloTokenizador,
        MemoriaEpistemica memoriaEpistemica,
        string diretorioArtefatos)
    {
        _configuracao = configuracao;
        _modelo = modelo;
        _gerador = gerador;
        _tokenizador = tokenizador;
        _modeloTokenizador = modeloTokenizador;
        _memoriaEpistemica = memoriaEpistemica;
        _diretorioArtefatos = diretorioArtefatos;
    }

    public string ProcessarMensagem(
        string entradaUsuario,
        SessaoUsuario sessao,
        out string fontePredominante)
    {
        var palavrasNovas = VocabularioDinamico.DetectarPalavrasNovas(
            entradaUsuario, _modeloTokenizador, sessao.Vocabulario!);
        if (palavrasNovas.Count > 0)
        {
            sessao.Vocabulario = VocabularioDinamico.RegistrarPalavrasNovas(
                sessao.Vocabulario!, palavrasNovas, "conversa");
            VocabularioDinamico.Persistir(_diretorioArtefatos, sessao.Vocabulario!);
        }

        var analisePrompt = CriarAnalisePrompt(entradaUsuario, sessao);

        var cadeiaRaciocinio = CadeiaRaciocinio.Executar(
            entradaUsuario,
            analisePrompt,
            _memoriaEpistemica,
            sessao.Memoria!,
            sessao.Cortex,
            sessao.Vocabulario!,
            sessao.Grafo,
            sessao.MatrizConceitual,
            sessao.CategoriasEmergentes);

        string textoResposta;

        if (!string.IsNullOrWhiteSpace(cadeiaRaciocinio.RespostaDireta))
        {
            textoResposta = TreinadorTokenizadorBpe.FormatarTextoParaExibicao(cadeiaRaciocinio.RespostaDireta);
            fontePredominante = "cadeia raciocinio (resposta direta)";
        }
        else
        {
            var respostaBaseadaEmEvidencias = RespondedorBaseadoEmEvidencias.TentarResponder(
                analisePrompt,
                _memoriaEpistemica);
            var respostaCognitiva = sessao.Cortex?.TentarResponderComEstado(analisePrompt) ?? string.Empty;

            var promptFinal = cadeiaRaciocinio.PromptGuiado;
            if (!string.IsNullOrWhiteSpace(cadeiaRaciocinio.PromptGuiado) &&
                cadeiaRaciocinio.PromptGuiado != analisePrompt.PromptEnriquecido)
            {
                promptFinal = InjetarContextoAprendizado(promptFinal, entradaUsuario, sessao);
            }

            textoResposta = TreinadorTokenizadorBpe.FormatarTextoParaExibicao(
                GerarRespostaComRaciocinio(analisePrompt, respostaBaseadaEmEvidencias, respostaCognitiva, cadeiaRaciocinio));
            fontePredominante = DeterminarFonteResposta(textoResposta, respostaBaseadaEmEvidencias, respostaCognitiva);
        }

        var dominioAssociado = analisePrompt.DominioPredominante;
        sessao.Memoria!.RegistrarTurnoUsuario(entradaUsuario, analisePrompt.EstadoCognitivo?.ResumoThink, dominioAssociado);

        sessao.Grafo?.PopularDeTexto(entradaUsuario, "turno-usuario", dominioAssociado ?? "geral");
        RepositorioGrafoConhecimento.Persistir(_diretorioArtefatos, sessao.Grafo!);

        if (sessao.MatrizConceitual is not null && sessao.CategoriasEmergentes is not null)
        {
            var termos = entradaUsuario.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .ToList();
            foreach (var termo in termos)
            {
                MatrizConceitual.EncontrarOuCriar(termo, sessao.MatrizConceitual);
            }

            var novaCategoria = MatrizConceitual.InferirCategoria(entradaUsuario, sessao.MatrizConceitual, sessao.CategoriasEmergentes);
            if (novaCategoria != "geral" && !sessao.CategoriasEmergentes.Contains(novaCategoria, StringComparer.OrdinalIgnoreCase))
            {
                sessao.CategoriasEmergentes.Add(novaCategoria);
            }

            MatrizConceitual.Persistir(_diretorioArtefatos, sessao.MatrizConceitual, sessao.CategoriasEmergentes);
        }

        var podePersistirResposta = RespondedorBaseadoEmEvidencias.PodePersistirResposta(
                analisePrompt,
                _memoriaEpistemica,
                textoResposta);
        if (podePersistirResposta)
        {
            sessao.Memoria.RegistrarTurnoModelo(textoResposta);
            sessao.Grafo?.PopularDeTexto(textoResposta, "resposta-oria", dominioAssociado ?? "geral");
        }

        sessao.Cortex?.RegistrarResultadoTurno(
            analisePrompt.EstadoCognitivo,
            entradaUsuario,
            textoResposta,
            fontePredominante,
            podePersistirResposta ? "boa" : "baixa");

        sessao.Memoria.Persistir();
        RepositorioGrafoConhecimento.Persistir(_diretorioArtefatos, sessao.Grafo!);

        return textoResposta;
    }

    private AnalisePromptEpistemica CriarAnalisePrompt(string entradaUsuario, SessaoUsuario sessao)
    {
        var analisador = new AnalisadorEpistemicoPrompt(_memoriaEpistemica, _tokenizador);
        var analiseBase = analisador.Analisar(entradaUsuario);
        var contextoConversacional = sessao.Memoria!.CriarContexto(entradaUsuario);
        var contextoFerramentas = new ContextoFerramentasConversacionais();
        var estadoCognitivo = sessao.Cortex?.PrepararTurno(
            analiseBase,
            contextoConversacional,
            contextoFerramentas);

        var blocosContexto = new List<string>();

        if (!string.IsNullOrWhiteSpace(contextoConversacional.NomeModelo))
        {
            blocosContexto.Add($"identidade : {TreinadorTokenizadorBpe.NormalizarTexto(contextoConversacional.NomeModelo)} .");
        }

        if (!string.IsNullOrWhiteSpace(contextoConversacional.TempoDeVidaTexto))
        {
            blocosContexto.Add($"tempo de vida : {TreinadorTokenizadorBpe.NormalizarTexto(contextoConversacional.TempoDeVidaTexto)} .");
        }

        if (!string.IsNullOrWhiteSpace(contextoConversacional.AmbienteBase))
        {
            blocosContexto.Add($"ambiente : {TreinadorTokenizadorBpe.NormalizarTexto(contextoConversacional.AmbienteBase)} .");
        }

        if (!string.IsNullOrWhiteSpace(contextoConversacional.DiferencaEuUsuario))
        {
            blocosContexto.Add($"eu e usuario : {TreinadorTokenizadorBpe.NormalizarTexto(contextoConversacional.DiferencaEuUsuario)} .");
        }

        if (!string.IsNullOrWhiteSpace(contextoConversacional.MissaoBase))
        {
            blocosContexto.Add($"missao : {TreinadorTokenizadorBpe.NormalizarTexto(contextoConversacional.MissaoBase)} .");
        }

        if (!string.IsNullOrWhiteSpace(estadoCognitivo?.ContextoCompacto))
        {
            blocosContexto.Add(TreinadorTokenizadorBpe.NormalizarTexto(estadoCognitivo.ContextoCompacto));
        }

        if (contextoConversacional.MemoriaCurta.Count > 0)
        {
            blocosContexto.Add($"memoria curta : {string.Join(" | ", contextoConversacional.MemoriaCurta.Take(3).Select(TreinadorTokenizadorBpe.NormalizarTexto))} .");
        }

        if (contextoConversacional.MemoriaMedia.Count > 0)
        {
            blocosContexto.Add($"memoria media : {string.Join(" | ", contextoConversacional.MemoriaMedia.Take(2).Select(TreinadorTokenizadorBpe.NormalizarTexto))} .");
        }

        if (contextoConversacional.MemoriaLonga.Count > 0)
        {
            blocosContexto.Add($"memoria longa : {string.Join(" | ", contextoConversacional.MemoriaLonga.Take(4).Select(TreinadorTokenizadorBpe.NormalizarTexto))} .");
        }

        blocosContexto.Add(analiseBase.PromptEnriquecido);

        var promptCompleto = string.Join(" ", blocosContexto.Where(x => !string.IsNullOrWhiteSpace(x)));

        var correcoes = new List<string>(analiseBase.CorrecoesSugeridas);
        if (contextoConversacional.MemoriaCurta.Count > 0 ||
            contextoConversacional.MemoriaMedia.Count > 0 ||
            contextoConversacional.MemoriaLonga.Count > 0)
        {
            correcoes.Add("usar memoria curta, media e longa antes da resposta");
        }

        if (contextoFerramentas.Blocos.Count > 0)
        {
            correcoes.Add("usar fatos observados por ferramentas antes da resposta");
        }

        if (!string.IsNullOrWhiteSpace(estadoCognitivo?.ContextoCompacto))
        {
            correcoes.Add("usar estado cognitivo compacto antes da resposta");
        }

        return new AnalisePromptEpistemica
        {
            EntradaOriginal = analiseBase.EntradaOriginal,
            TipoPragmatica = analiseBase.TipoPragmatica,
            DominioPredominante = analiseBase.DominioPredominante,
            FormasAtivas = analiseBase.FormasAtivas,
            SimbolosDesconhecidos = analiseBase.SimbolosDesconhecidos,
            ApoioNoCorpus = analiseBase.ApoioNoCorpus,
            PromptEnriquecido = promptCompleto,
            EstadoCognitivo = estadoCognitivo,
            CorrecoesSugeridas = correcoes
        };
    }

    private string InjetarContextoAprendizado(string promptBase, string entradaUsuario, SessaoUsuario sessao)
    {
        var fatosGrafo = sessao.Grafo?.RecuperarFatosRelacionados(
            ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(
                TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario)),
            maximo: 3);

        if (fatosGrafo is null || fatosGrafo.Count == 0)
        {
            return promptBase;
        }

        var contextoFatos = string.Join(" . ", fatosGrafo);
        return $"fatos aprendidos: {contextoFatos} . {promptBase}";
    }

    private string GerarRespostaComRaciocinio(
        AnalisePromptEpistemica analisePrompt,
        string respostaBaseadaEmEvidencias,
        string respostaCognitiva,
        ResultadoCadeiaRaciocinio cadeia)
    {
        if (cadeia.DeveGerarNeural)
        {
            var resposta = _gerador.Gerar(
                analisePrompt.PromptEnriquecido,
                _configuracao.Geracao.MaximoNovosTokens).TextoCompletoDecodificado.Trim();

            if (!string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias) &&
                !GestorMemoriaConversacional.TextoEstaDegradado(respostaBaseadaEmEvidencias) &&
                (GestorMemoriaConversacional.TextoEstaDegradado(resposta) ||
                 SintetizadorRespostaContextual.RespostaPareceDesalinhada(analisePrompt.EntradaOriginal, resposta)))
            {
                return respostaBaseadaEmEvidencias;
            }

            if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
                !GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva) &&
                (GestorMemoriaConversacional.TextoEstaDegradado(resposta) ||
                 SintetizadorRespostaContextual.RespostaPareceDesalinhada(analisePrompt.EntradaOriginal, resposta)))
            {
                return respostaCognitiva;
            }

            return !GestorMemoriaConversacional.TextoEstaDegradado(resposta)
                ? resposta
                : CriarRespostaSeguraQuandoGeracaoFalha(analisePrompt);
        }

        return cadeia.RespostaDireta;
    }

    private static string CriarRespostaSeguraQuandoGeracaoFalha(AnalisePromptEpistemica analise)
    {
        var intencao = analise.EstadoCognitivo?.Intencao ?? string.Empty;
        if (intencao.Contains("perguntar", StringComparison.OrdinalIgnoreCase))
        {
            return "ainda nao consegui formular uma resposta boa para essa pergunta. me da mais contexto ou reformula?";
        }
        return "essa parte ainda esta alem do que eu consigo gerar bem. me fala mais sobre o contexto?";
    }

    private static string DeterminarFonteResposta(
        string textoResposta,
        string respostaBaseadaEmEvidencias,
        string respostaCognitiva)
    {
        if (!string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias) &&
            textoResposta.Contains(respostaBaseadaEmEvidencias[..Math.Min(20, respostaBaseadaEmEvidencias.Length)], StringComparison.OrdinalIgnoreCase))
        {
            return "evidencia";
        }

        if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
            textoResposta.Contains(respostaCognitiva[..Math.Min(20, respostaCognitiva.Length)], StringComparison.OrdinalIgnoreCase))
        {
            return "memoria experiencial";
        }

        return "geracao neural";
    }
}
