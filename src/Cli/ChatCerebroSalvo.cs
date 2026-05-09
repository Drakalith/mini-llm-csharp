using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Ferramentas;
using MiniLlmDidatico.Geracao;
using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Numerica;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cli;

public sealed class ChatCerebroSalvo
{
    private readonly ConfiguracaoAplicacao _configuracao;
    private readonly IReadOnlyList<EntradaCorpus> _corpus;
    private readonly GravadorArtefatos _gravadorArtefatos;
    private readonly RelatorioAutoConfiguracaoCorpus _relatorioAutoConfiguracaoCorpus;

    private EstadoCerebroTreinado? _estadoCerebro;
    private DiagnosticoPersistenciaCerebro? _diagnosticoCompatibilidadeAtual;
    private MemoriaEpistemica? _memoriaEpistemica;
    private GestorMemoriaConversacional? _memoriaConversacional;
    private MotorFerramentasContextuais? _motorFerramentas;
    private CortexConversacional? _cortexConversacional;
    private Tokenizador? _tokenizador;
    private Gerador? _gerador;
    private GrafoConhecimento? _grafoConhecimento;
    private EstadoVocabularioDinamico? _vocabularioDinamico;
    private List<VetorConceitual>? _matrizConceitual;
    private List<string>? _categoriasEmergentes;
    private bool _consolidacaoPendente;

    public ChatCerebroSalvo(
        ConfiguracaoAplicacao configuracao,
        IReadOnlyList<EntradaCorpus> corpus,
        GravadorArtefatos gravadorArtefatos,
        RelatorioAutoConfiguracaoCorpus relatorioAutoConfiguracaoCorpus)
    {
        _configuracao = configuracao;
        _corpus = corpus;
        _gravadorArtefatos = gravadorArtefatos;
        _relatorioAutoConfiguracaoCorpus = relatorioAutoConfiguracaoCorpus;
    }

    public void Executar()
    {
        CarregarCerebroSalvo();

        ExecutorPassosConsole.Cabecalho("CHAT COM CEREBRO SALVO");
        RenderizadorConsole.Observacao("Modo direto: carrega o cerebro salvo e abre apenas a conversa.");
        RenderizadorConsole.Observacao("O treino e pulado. A memoria conversacional continua ativa e persistente.");
        ExibirBannerCompatibilidade();

        RenderizadorConsole.Secao("Contexto do chat");
        RenderizadorConsole.ChaveValor("Tipo de modelo", "mini-transformador decoder-only salvo");
        RenderizadorConsole.ChaveValor("Nome", _memoriaConversacional!.Estado.Identidade.NomeModelo, TomConsole.Sucesso);
        RenderizadorConsole.ChaveValor("Criado em Brasil/SP", _estadoCerebro!.CriadoEmBrasilSp.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        RenderizadorConsole.ChaveValor("Tempo de vida", CriarContextoConversacional(string.Empty).TempoDeVidaTexto);
        RenderizadorConsole.ChaveValor("Backend atual", $"{_modelo!.ContextoBackend.NomeBackend} / {_modelo.ContextoBackend.DispositivoUsado}");
        RenderizadorConsole.ChaveValor("Perfil do corpus", _relatorioAutoConfiguracaoCorpus.PerfilCorpus, TomConsole.Info);
        RenderizadorConsole.ChaveValor(
            "Configuracao efetiva",
            $"seq={_configuracao.MaximoComprimentoSequencia}, merges={_configuracao.MaximoMesclasTokenizador}, emb={_configuracao.Modelo.DimensaoEmbedding}, ff={_configuracao.Modelo.DimensaoFeedForward}, camadas={_configuracao.Modelo.NumeroCamadas}");
        RenderizadorConsole.ChaveValor("Modo de geracao", _gerador!.DescreverModoSelecao());
        RenderizadorConsole.ChaveValor(
            "Corpus de apoio",
            $"{_corpus.Count} entradas em {_corpus.Select(x => x.Dominio).Distinct(StringComparer.Ordinal).Count()} dominios");
        RenderizadorConsole.Status("Cortex conversacional", _configuracao.Cognicao.Ativar && _cortexConversacional is not null);
        if (_configuracao.Cognicao.Ativar)
        {
            RenderizadorConsole.ChaveValor("Modo think", _configuracao.Cognicao.ModoThink, TomConsole.Info);
        }

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Observacao("Digite vazio para sair. Digite '/consolidar' para consolidar memorias.");

        while (true)
        {
            var entradaUsuario = RenderizadorConsole.LerPrompt("Voce> ");
            if (string.IsNullOrWhiteSpace(entradaUsuario))
            {
                RenderizadorConsole.Observacao("Encerrando chat.");
                return;
            }

            if (string.Equals(entradaUsuario.Trim(), "/consolidar", StringComparison.OrdinalIgnoreCase))
            {
                ExecutarConsolidacaoMemoria();
                continue;
            }

            var palavrasNovas = VocabularioDinamico.DetectarPalavrasNovas(
                entradaUsuario, _estadoCerebro!.ModeloTokenizador, _vocabularioDinamico!);
            if (palavrasNovas.Count > 0)
            {
                _vocabularioDinamico = VocabularioDinamico.RegistrarPalavrasNovas(
                    _vocabularioDinamico!, palavrasNovas, "conversa");
                VocabularioDinamico.Persistir(_gravadorArtefatos.Raiz, _vocabularioDinamico!);
            }

            var analisePrompt = CriarAnalisePrompt(entradaUsuario);

            var cadeiaRaciocinio = CadeiaRaciocinio.Executar(
                entradaUsuario,
                analisePrompt,
                _memoriaEpistemica!,
                _memoriaConversacional!,
                _cortexConversacional,
                _vocabularioDinamico!,
                _grafoConhecimento,
                _matrizConceitual,
                _categoriasEmergentes);

            RenderizadorCortexConversacional.ExibirThink(analisePrompt.EstadoCognitivo, _configuracao.Cognicao);
            ExibirCadeiaRaciocinio(cadeiaRaciocinio);

            string textoResposta;
            string fontePredominante;

            if (!string.IsNullOrWhiteSpace(cadeiaRaciocinio.RespostaDireta))
            {
                textoResposta = TreinadorTokenizadorBpe.FormatarTextoParaExibicao(cadeiaRaciocinio.RespostaDireta);
                fontePredominante = "cadeia raciocinio (resposta direta)";
            }
            else
            {
                var respostaBaseadaEmEvidencias = RespondedorBaseadoEmEvidencias.TentarResponder(
                    analisePrompt,
                    _memoriaEpistemica!);
                var respostaCognitiva = _cortexConversacional?.TentarResponderComEstado(analisePrompt) ?? string.Empty;

                var promptFinal = cadeiaRaciocinio.PromptGuiado;
                if (!string.IsNullOrWhiteSpace(cadeiaRaciocinio.PromptGuiado) &&
                    cadeiaRaciocinio.PromptGuiado != analisePrompt.PromptEnriquecido)
                {
                    promptFinal = InjetarContextoAprendizado(promptFinal, entradaUsuario);
                }

                textoResposta = TreinadorTokenizadorBpe.FormatarTextoParaExibicao(
                    GerarRespostaComRaciocinio(analisePrompt, respostaBaseadaEmEvidencias, respostaCognitiva, cadeiaRaciocinio));
                fontePredominante = DeterminarFonteResposta(textoResposta, respostaBaseadaEmEvidencias, respostaCognitiva);
            }

            var dominioAssociado = analisePrompt.DominioPredominante;
            _memoriaConversacional!.RegistrarTurnoUsuario(entradaUsuario, analisePrompt.EstadoCognitivo?.ResumoThink, dominioAssociado);
            _motorFerramentas?.RegistrarTurnoUsuario(entradaUsuario);

            _grafoConhecimento?.PopularDeTexto(entradaUsuario, "turno-usuario", dominioAssociado ?? "geral");
            RepositorioGrafoConhecimento.Persistir(_gravadorArtefatos.Raiz, _grafoConhecimento!);

            if (_matrizConceitual is not null && _categoriasEmergentes is not null)
            {
                var termos = entradaUsuario.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length > 2)
                    .ToList();
                foreach (var termo in termos)
                {
                    MatrizConceitual.EncontrarOuCriar(termo, _matrizConceitual);
                }

                var novaCategoria = MatrizConceitual.InferirCategoria(entradaUsuario, _matrizConceitual, _categoriasEmergentes);
                if (novaCategoria != "geral" && !_categoriasEmergentes.Contains(novaCategoria, StringComparer.OrdinalIgnoreCase))
                {
                    _categoriasEmergentes.Add(novaCategoria);
                }

                MatrizConceitual.Persistir(_gravadorArtefatos.Raiz, _matrizConceitual, _categoriasEmergentes);
            }

            var podePersistirResposta = RespondedorBaseadoEmEvidencias.PodePersistirResposta(
                    analisePrompt,
                    _memoriaEpistemica!,
                    textoResposta);
            if (podePersistirResposta)
            {
                _memoriaConversacional.RegistrarTurnoModelo(textoResposta);
                _motorFerramentas?.RegistrarTurnoModelo(textoResposta);
                _grafoConhecimento?.PopularDeTexto(textoResposta, "resposta-oria", dominioAssociado ?? "geral");
            }

            _cortexConversacional?.RegistrarResultadoTurno(
                analisePrompt.EstadoCognitivo,
                entradaUsuario,
                textoResposta,
                fontePredominante,
                podePersistirResposta ? "boa" : "baixa");

            _consolidacaoPendente = _memoriaConversacional.Estado.LembrancasLongoPrazo.Count > 64;

            RenderizadorConsole.Prompt($"{_memoriaConversacional.Estado.Identidade.NomeModelo}> ", TomConsole.Sucesso);
            RenderizadorConsole.Texto(textoResposta);
            RenderizadorConsole.LinhaEmBranco();
        }
    }

    private void ExecutarConsolidacaoMemoria()
    {
        RenderizadorConsole.Secao("Consolidando memorias (sleep mechanism)");
        var resultado = ConsolidadorMemoria.Consolidar(
            _memoriaConversacional!,
            _cortexConversacional!,
            _grafoConhecimento!,
            _vocabularioDinamico!);

        RenderizadorConsole.ChaveValor("Memorias mescladas", resultado.MemoriasMescladas.ToString());
        RenderizadorConsole.ChaveValor("Memorias decaidas", resultado.MemoriasDecaidas.ToString());
        RenderizadorConsole.ChaveValor("Memorias removidas", resultado.MemoriasRemovidas.ToString());
        RenderizadorConsole.ChaveValor("Fatos consolidados", resultado.FatosNovosConsolidados.ToString());

        if (resultado.Alteracoes.Count > 0)
        {
            RenderizadorConsole.Observacao("Alteracoes:");
            foreach (var alteracao in resultado.Alteracoes.Take(5))
            {
                RenderizadorConsole.Texto($"  - {alteracao}");
            }
        }

        _memoriaConversacional!.Persistir();
        _consolidacaoPendente = false;
        RenderizadorConsole.LinhaEmBranco();
    }

    private static void ExibirCadeiaRaciocinio(ResultadoCadeiaRaciocinio cadeia)
    {
        RenderizadorConsole.Secao("Cadeia de raciocinio");
        foreach (var elo in cadeia.Elos)
        {
            var status = elo.BloqueiaGeracao ? "[BLOQUEIA]" : elo.Confianca >= 0.7 ? "[OK]" : "[?]";
            RenderizadorConsole.ChaveValor(
                $"  {elo.Tipo} {status}",
                $"{elo.Pergunta} -> {elo.Resposta} (confianca: {elo.Confianca:0.00})");
        }
        RenderizadorConsole.ChaveValor("Decisao", cadeia.MotivoDecisao, TomConsole.Info);
        RenderizadorConsole.ChaveValor("Geracao neural", cadeia.DeveGerarNeural ? "sim" : "nao (resposta direta)", TomConsole.Sucesso);
        RenderizadorConsole.LinhaEmBranco();
    }

    private string InjetarContextoAprendizado(string promptBase, string entradaUsuario)
    {
        var fatosGrafo = _grafoConhecimento?.RecuperarFatosRelacionados(
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
            var resposta = _gerador!.Gerar(
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

    private MiniTransformador _modelo = null!;

    private void CarregarCerebroSalvo()
    {
        _diagnosticoCompatibilidadeAtual = PersistenciaCerebroTreinado.Diagnosticar(
            _gravadorArtefatos.Raiz,
            _corpus,
            _configuracao);

        if (!_diagnosticoCompatibilidadeAtual.ArquivoEncontrado)
        {
            throw new InvalidOperationException(
                $"Nenhum cerebro salvo foi encontrado em {_diagnosticoCompatibilidadeAtual.CaminhoArquivo}. Rode o fluxo completo ao menos uma vez antes de usar o chat direto.");
        }

        _estadoCerebro = _diagnosticoCompatibilidadeAtual.EstadoEncontrado
                         ?? PersistenciaCerebroTreinado.Carregar(_diagnosticoCompatibilidadeAtual.CaminhoArquivo);

        var relatorioHardware = AnalisadorAdaptacaoHardware.Analisar(
            new PerfilCargaNumerica
            {
                DimensaoEmbedding = _estadoCerebro.HiperParametros.DimensaoEmbedding,
                DimensaoFeedForward = _estadoCerebro.HiperParametros.DimensaoFeedForward,
                TipoCarga = "inferencia"
            },
            _configuracao.Execucao.Treino,
            _configuracao.Execucao.Inferencia);

        var (executorNumerico, contextoBackend) = FabricaExecutorNumerico.Criar(relatorioHardware.DecisaoInferencia);
        _modelo = new MiniTransformador(_estadoCerebro.HiperParametros, executorNumerico, contextoBackend);
        _modelo.AplicarEstado(_estadoCerebro.EstadoModelo);

        _tokenizador = new Tokenizador(_estadoCerebro.ModeloTokenizador);
        _gerador = new Gerador(
            _modelo,
            _tokenizador,
            _estadoCerebro.ModeloTokenizador,
            ParametrosAmostragemGeracao.DeConfiguracao(_configuracao.Geracao),
            _configuracao.SementeAleatoria);
        _memoriaEpistemica = ConstrutorMemoriaEpistemica.Construir(_corpus);
        var identidadePadrao = ExtratorIdentidadeCorpus.Extrair(_corpus);
        _memoriaConversacional = GestorMemoriaConversacional.CarregarOuCriar(
            _gravadorArtefatos.Raiz,
            _estadoCerebro.CriadoEmBrasilSp,
            identidadePadrao);
        _motorFerramentas = new MotorFerramentasContextuais(
            Path.GetDirectoryName(_gravadorArtefatos.Raiz) ?? AppContext.BaseDirectory,
            _gravadorArtefatos.Raiz,
            _corpus,
            _memoriaConversacional.Estado);
        _cortexConversacional = CortexConversacional.CarregarOuCriar(
            _gravadorArtefatos.Raiz,
            _configuracao.Cognicao);
        _grafoConhecimento = RepositorioGrafoConhecimento.CarregarOuReconstruir(
            _gravadorArtefatos.Raiz,
            _corpus,
            _memoriaConversacional.Estado);
        _vocabularioDinamico = VocabularioDinamico.Carregar(_gravadorArtefatos.Raiz);
        _matrizConceitual = MatrizConceitual.CarregarOuCriar(_gravadorArtefatos.Raiz);
        _categoriasEmergentes = new List<string>();
    }

    private void ExibirBannerCompatibilidade()
    {
        if (_diagnosticoCompatibilidadeAtual is null)
        {
            return;
        }

        if (_diagnosticoCompatibilidadeAtual.DeveCarregarCerebroSalvo &&
            _diagnosticoCompatibilidadeAtual.ContextoCognitivoCompativel)
        {
            RenderizadorConsole.Sucesso("Cerebro salvo alinhado com o texto treinado, o contexto cognitivo e a configuracao atuais.");
            return;
        }

        if (_diagnosticoCompatibilidadeAtual.MudancaSomenteContextoCognitivo)
        {
            RenderizadorConsole.Alerta("Os pesos ainda servem, mas o contexto cognitivo do corpus mudou. O chat seguira com o cerebro salvo e memoria/contexto atualizados.");
            RenderizadorConsole.Observacao(_diagnosticoCompatibilidadeAtual.Motivo);
            return;
        }

        RenderizadorConsole.Erro("Este chat esta usando um cerebro salvo potencialmente desatualizado em relacao ao treino atual.");
        RenderizadorConsole.Observacao(_diagnosticoCompatibilidadeAtual.Motivo);
        RenderizadorConsole.Observacao("O modo --chat continua permissivo: ele nao bloqueia a conversa, mas pode refletir pesos antigos.");
    }

    private AnalisePromptEpistemica CriarAnalisePrompt(string entradaUsuario)
    {
        var analisador = new AnalisadorEpistemicoPrompt(_memoriaEpistemica!, _tokenizador!);
        var analiseBase = analisador.Analisar(entradaUsuario);
        var contextoConversacional = CriarContextoConversacional(entradaUsuario);
        var contextoFerramentas = _motorFerramentas?.CriarContexto(entradaUsuario) ?? new ContextoFerramentasConversacionais();
        var estadoCognitivo = _cortexConversacional?.PrepararTurno(
            analiseBase,
            contextoConversacional,
            contextoFerramentas);

        return EnriquecerAnaliseComMemoriaConversacional(
            analiseBase,
            contextoConversacional,
            contextoFerramentas,
            estadoCognitivo,
            _tokenizador!,
            _estadoCerebro!.HiperParametros.MaximoComprimentoSequencia);
    }

    private ContextoMemoriaConversacional CriarContextoConversacional(string entradaUsuario)
    {
        return _memoriaConversacional!.CriarContexto(entradaUsuario);
    }

    private static AnalisePromptEpistemica EnriquecerAnaliseComMemoriaConversacional(
        AnalisePromptEpistemica analiseBase,
        ContextoMemoriaConversacional contextoConversacional,
        ContextoFerramentasConversacionais contextoFerramentas,
        EstadoCognitivoTurno? estadoCognitivo,
        Tokenizador tokenizador,
        int maximoComprimentoSequencia)
    {
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

        if (contextoFerramentas.Blocos.Count > 0)
        {
            blocosContexto.AddRange(contextoFerramentas.Blocos
                .Where(x => !string.IsNullOrWhiteSpace(x.Texto))
                .Select(x => TreinadorTokenizadorBpe.NormalizarTexto(x.Texto)));
        }

        blocosContexto.Add(analiseBase.PromptEnriquecido);

        var promptCompleto = string.Join(" ", blocosContexto.Where(x => !string.IsNullOrWhiteSpace(x)));
        var promptAjustado = AjustarPromptAoLimiteDoModelo(
            tokenizador,
            maximoComprimentoSequencia,
            analiseBase.PromptEnriquecido,
            blocosContexto.Take(blocosContexto.Count - 1));

        var correcoes = new List<string>(analiseBase.CorrecoesSugeridas);
        if (contextoConversacional.MemoriaCurta.Count > 0 ||
            contextoConversacional.MemoriaMedia.Count > 0 ||
            contextoConversacional.MemoriaLonga.Count > 0)
        {
            correcoes.Add("usar memoria curta, media e longa antes da resposta");
        }

        if (!string.Equals(promptCompleto, promptAjustado, StringComparison.Ordinal))
        {
            correcoes.Add("compactar o contexto para caber no limite do modelo sem perder o nucleo do pedido");
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
            TipoApoioPredominante = analiseBase.TipoApoioPredominante,
            DominioPredominante = analiseBase.DominioPredominante,
            NomeIdentidade = contextoConversacional.NomeModelo,
            AmbienteBase = contextoConversacional.AmbienteBase,
            TempoDeVidaTexto = contextoConversacional.TempoDeVidaTexto,
            PromptBase = analiseBase.PromptBase,
            PromptEnriquecido = promptAjustado,
            FormasAtivas = analiseBase.FormasAtivas,
            FormasForaDaMemoria = analiseBase.FormasForaDaMemoria,
            SimbolosDesconhecidos = analiseBase.SimbolosDesconhecidos,
            TrechosRecuperados = analiseBase.TrechosRecuperados,
            MemoriaCurta = contextoConversacional.MemoriaCurta,
            MemoriaMedia = contextoConversacional.MemoriaMedia,
            MemoriaLonga = contextoConversacional.MemoriaLonga,
            ContextosFerramenta = contextoFerramentas.Blocos
                .Select(x => $"{x.Rotulo}: {x.Texto}")
                .ToList(),
            EstadoCognitivo = estadoCognitivo,
            SignosRelevantes = analiseBase.SignosRelevantes,
            CorrecoesSugeridas = correcoes.Distinct(StringComparer.Ordinal).ToList(),
            PerguntaCuriosaSugerida = contextoConversacional.PerguntaCuriosaSugerida,
            MotivoReescrita = analiseBase.MotivoReescrita,
            ApoioNoCorpus = analiseBase.ApoioNoCorpus
        };
    }

    private static string AjustarPromptAoLimiteDoModelo(
        Tokenizador tokenizador,
        int maximoComprimentoSequencia,
        string blocoNucleo,
        IEnumerable<string> blocosContexto)
    {
        var promptAtual = AjustarTextoAoLimitePreservandoFinal(tokenizador, blocoNucleo, maximoComprimentoSequencia);

        foreach (var bloco in blocosContexto
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Reverse())
        {
            var candidato = $"{bloco} {promptAtual}".Trim();
            if (CabeNoLimiteDoModelo(tokenizador, candidato, maximoComprimentoSequencia))
            {
                promptAtual = candidato;
            }
        }

        return promptAtual;
    }

    private string GerarRespostaSemScript(
        AnalisePromptEpistemica analisePrompt,
        string respostaBaseadaEmEvidencias,
        string respostaCognitiva)
    {
        if (CortexConversacional.DevePriorizarRespostaCognitiva(analisePrompt, respostaCognitiva))
        {
            return respostaCognitiva;
        }

        if (DevePriorizarRespostaBaseadaEmEvidencias(analisePrompt, respostaBaseadaEmEvidencias))
        {
            return respostaBaseadaEmEvidencias;
        }

        var resposta = _gerador!.Gerar(
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

        if (!GestorMemoriaConversacional.TextoEstaDegradado(resposta))
        {
            return resposta;
        }

        var respostaBase = _gerador.Gerar(
            analisePrompt.PromptBase,
            _configuracao.Geracao.MaximoNovosTokens).TextoCompletoDecodificado.Trim();

        if (!GestorMemoriaConversacional.TextoEstaDegradado(respostaBase))
        {
            return respostaBase;
        }

        if (!string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias) &&
            !GestorMemoriaConversacional.TextoEstaDegradado(respostaBaseadaEmEvidencias))
        {
            return respostaBaseadaEmEvidencias;
        }

        if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
            !GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva))
        {
            return respostaCognitiva;
        }

        if (analisePrompt.ContextosFerramenta.Count > 0)
        {
            var promptFerramentas = string.Join(
                " ",
                analisePrompt.ContextosFerramenta
                    .Take(2)
                    .Select(TreinadorTokenizadorBpe.NormalizarTexto))
                + " " + analisePrompt.PromptBase;

            var respostaFerramentas = _gerador.Gerar(
                promptFerramentas.Trim(),
                _configuracao.Geracao.MaximoNovosTokens).TextoCompletoDecodificado.Trim();

            if (!GestorMemoriaConversacional.TextoEstaDegradado(respostaFerramentas))
            {
                return respostaFerramentas;
            }
        }

        return CriarRespostaSeguraQuandoGeracaoFalha(analisePrompt);
    }

    private static string DeterminarFonteResposta(
        string textoResposta,
        string respostaBaseadaEmEvidencias,
        string respostaCognitiva)
    {
        var respostaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(textoResposta);
        if (!string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias) &&
            respostaNormalizada == TreinadorTokenizadorBpe.NormalizarTexto(respostaBaseadaEmEvidencias))
        {
            return "evidencia";
        }

        if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
            respostaNormalizada == TreinadorTokenizadorBpe.NormalizarTexto(respostaCognitiva))
        {
            return "memoria experiencial";
        }

        return "geracao neural";
    }

    private static string CriarRespostaSeguraQuandoGeracaoFalha(AnalisePromptEpistemica analisePrompt)
    {
        return string.Equals(analisePrompt.TipoPragmatica, "pergunta", StringComparison.Ordinal)
            ? "ainda nao consegui responder bem a isso com o treino atual."
            : "ainda nao consegui formar uma resposta limpa com o treino atual.";
    }

    private static bool DevePriorizarRespostaBaseadaEmEvidencias(
        AnalisePromptEpistemica analisePrompt,
        string respostaBaseadaEmEvidencias)
    {
        if (string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias))
        {
            return false;
        }

        if (GestorMemoriaConversacional.TextoEstaDegradado(respostaBaseadaEmEvidencias))
        {
            return false;
        }

        if (!string.Equals(analisePrompt.TipoPragmatica, "pergunta", StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(analisePrompt.TipoApoioPredominante, "pergunta-resposta", StringComparison.Ordinal))
        {
            return false;
        }

        return analisePrompt.ApoioNoCorpus >= 0.95;
    }

    private static bool CabeNoLimiteDoModelo(
        Tokenizador tokenizador,
        string texto,
        int maximoComprimentoSequencia)
    {
        return tokenizador.Codificar(texto, adicionarInicio: true, adicionarFim: false).Count <= maximoComprimentoSequencia;
    }

    private static string AjustarTextoAoLimitePreservandoFinal(
        Tokenizador tokenizador,
        string texto,
        int maximoComprimentoSequencia)
    {
        var ids = tokenizador.Codificar(texto, adicionarInicio: true, adicionarFim: false);
        if (ids.Count <= maximoComprimentoSequencia)
        {
            return texto;
        }

        var quantidadeTokensTexto = Math.Max(1, maximoComprimentoSequencia - 1);
        var idsSemInicio = ids.Skip(1).ToList();
        var idsAjustados = idsSemInicio
            .Skip(Math.Max(0, idsSemInicio.Count - quantidadeTokensTexto))
            .ToList();

        return tokenizador.Decodificar(idsAjustados);
    }
}
