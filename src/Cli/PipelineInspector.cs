using System.Runtime.InteropServices;
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
using MiniLlmDidatico.Treinamento;

namespace MiniLlmDidatico.Cli;

public sealed class InspetorPipeline
{
    private const int TotalEtapasPipeline = 14;
    private readonly ConfiguracaoAplicacao _configuracao;
    private readonly IReadOnlyList<EntradaCorpus> _corpus;
    private readonly GravadorArtefatos _gravadorArtefatos;
    private readonly RelatorioAutoConfiguracaoCorpus _relatorioAutoConfiguracaoCorpus;
    private readonly List<MedicaoEtapa> _medicoes = new();
    private MonitorExecucaoTempoReal? _monitorExecucao;
    private MemoriaEpistemica? _memoriaEpistemica;
    private RelatorioAdaptacaoHardware? _relatorioAdaptacaoHardware;
    private RelatorioNuvemPalavrasCorpus? _relatorioNuvemPalavras;
    private DiagnosticoPersistenciaCerebro? _diagnosticoPersistenciaCerebro;
    private GestorMemoriaConversacional? _memoriaConversacional;
    private MotorFerramentasContextuais? _motorFerramentas;
    private CortexConversacional? _cortexConversacional;
    private bool _monitoramentoFinalizado;
    private int _larguraLinhaProgressoTreino;
    private int _larguraLinhaProgressoEtapaInterna;

    public InspetorPipeline(
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
        IniciarMonitoramentoTempoReal();

        try
        {
            ExecutorPassosConsole.Cabecalho("MINI LLM DIDATICO EM C#");
            RenderizadorConsole.Observacao("Objetivo: mostrar um pipeline real, pequeno, observavel e com escala explicita.");
            RenderizadorConsole.Observacao("Fluxo: hardware -> corpus -> tokenizacao BPE -> ids -> embeddings -> decoder transformer -> logits -> softmax -> geracao.");
            RenderizadorConsole.Observacao("O modelo usa multi-head attention, layer norm e blocos empilhados.");
            RenderizadorConsole.Observacao("Na inicializacao, o sistema analisa CPU e GPU, recomenda um componente e respeita configuracao manual quando existir.");
            RenderizadorConsole.ChaveValor("Fuso horario ativo", RelogioBrasil.NomeFuso);

            ExibirAutoConfiguracaoCorpus();
            ExibirAdaptacaoHardware();
            ExibirPersistenciaCerebro();

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para inspecionar o corpus.");
            ExibirCorpus();

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para ver a nuvem de palavras do corpus.");
            ExibirNuvemPalavrasCorpus();

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para construir a memoria epistemica.");
            ExibirMemoriaEpistemica();

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para treinar o tokenizador BPE.");
            var tokenizador = ExibirTokenizador();

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para converter o corpus em ids e montar exemplos de treino.");
            var exemplosTreino = ExibirExemplosTreino(tokenizador);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para inicializar o mini-transformador.");
            var modelo = ExibirModelo(tokenizador);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para treinar o modelo.");
            ExibirTreino(modelo, exemplosTreino);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para inspecionar uma passagem direta detalhada.");
            var entradaInspecao = SolicitarEntradaManual();
            var analiseInspecao = CriarAnalisePrompt(tokenizador, entradaInspecao);
            var idsInspecao = ExibirPassagemDireta(modelo, tokenizador, analiseInspecao);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para gerar texto token por token.");
            var gerador = new Gerador(
                modelo,
                tokenizador,
                _resultadoTreinoTokenizador!.Modelo,
                ParametrosAmostragemGeracao.DeConfiguracao(_configuracao.Geracao),
                _configuracao.SementeAleatoria);
            ExibirGeracaoExemplo(gerador, analiseInspecao);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para ver o relatorio de escala e recursos.");
            ExibirEscalaERecursos(modelo, idsInspecao.Count);

            ExecutorPassosConsole.AguardarEnter("Pressione ENTER para abrir o chat interativo.");
            ExibirChatInterativo(gerador);

            FinalizarMonitoramentoTempoReal("execucao concluida normalmente");
        }
        catch (Exception ex)
        {
            FinalizarMonitoramentoTempoReal($"execucao encerrada com excecao: {ex.GetType().Name}");
            throw;
        }
    }

    private ResultadoTreinoTokenizador? _resultadoTreinoTokenizador;
    private ResultadoTreino? _resultadoTreino;

    private void ExibirAutoConfiguracaoCorpus()
    {
        _monitorExecucao?.AtualizarEtapa("auto-configuracao a partir do corpus");
        ExibirCabecalhoEtapa("ETAPA 0 - AUTOCONFIGURACAO PELO CORPUS", 1);

        ExibirProgressoEtapaInterna("lendo perfil do corpus", 1, 3);
        ExibirProgressoEtapaInterna("comparando configuracao solicitada e efetiva", 2, 3);
        ExibirProgressoEtapaInterna("auto-configuracao concluida", 3, 3);
        FinalizarLinhaProgressoEtapaInterna();

        var estatisticas = _relatorioAutoConfiguracaoCorpus.Estatisticas;

        RenderizadorConsole.Secao("Configuracao efetiva derivada do corpus");
        RenderizadorConsole.Status("Auto-configuracao ativa", _relatorioAutoConfiguracaoCorpus.AutoConfiguracaoAtiva);
        RenderizadorConsole.ChaveValor("Perfil estimado", _relatorioAutoConfiguracaoCorpus.PerfilCorpus, TomConsole.Info);
        RenderizadorConsole.ChaveValor("Motivo geral", _relatorioAutoConfiguracaoCorpus.MotivoGeral, TomConsole.Muted);

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Leitura do corpus");
        RenderizadorConsole.ChaveValor("Entradas", estatisticas.TotalEntradas);
        RenderizadorConsole.ChaveValor("Dominios", estatisticas.TotalDominios);
        RenderizadorConsole.ChaveValor("Pergunta/resposta", estatisticas.EntradasPerguntaResposta);
        RenderizadorConsole.ChaveValor("Texto livre", estatisticas.EntradasTextoLivre);
        RenderizadorConsole.ChaveValor("Tokens brutos", estatisticas.TotalTokensBrutos);
        RenderizadorConsole.ChaveValor("Vocabulario bruto", estatisticas.VocabularioBruto);
        RenderizadorConsole.ChaveValor("Diversidade lexical", estatisticas.DiversidadeLexica.ToString("0.000"));
        RenderizadorConsole.ChaveValor("Duplicidade aproximada", estatisticas.TaxaDuplicidade.ToString("0.0%"));
        RenderizadorConsole.ChaveValor("Media de tokens por entrada", estatisticas.MediaTokensPorEntrada.ToString("0.0"));
        RenderizadorConsole.ChaveValor("P90 tokens por entrada", estatisticas.Percentil90TokensPorEntrada);
        RenderizadorConsole.ChaveValor("P95 tokens por entrada", estatisticas.Percentil95TokensPorEntrada);
        RenderizadorConsole.ChaveValor("P90 tokens por resposta", estatisticas.Percentil90TokensResposta);

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Ajustes aplicados");
        if (_relatorioAutoConfiguracaoCorpus.Ajustes.Count == 0)
        {
            RenderizadorConsole.Observacao("Nenhum ajuste automatico foi necessario; a configuracao solicitada ja estava alinhada ao corpus.");
        }
        else
        {
            foreach (var ajuste in _relatorioAutoConfiguracaoCorpus.Ajustes)
            {
                RenderizadorConsole.ItemLista(
                    $"{ajuste.Chave}: {ajuste.ValorSolicitado} -> {ajuste.ValorEfetivo} | {ajuste.Motivo}",
                    TomConsole.Padrao);
            }
        }
    }

    private void ExibirPersistenciaCerebro()
    {
        _monitorExecucao?.AtualizarEtapa("persistencia do cerebro");
        ExibirCabecalhoEtapa("ETAPA 0B - CEREBRO SALVO", 3);

        ExibirProgressoEtapaInterna("diagnosticando cerebro salvo", 1, 3);

        _diagnosticoPersistenciaCerebro = PersistenciaCerebroTreinado.Diagnosticar(
            _gravadorArtefatos.Raiz,
            _corpus,
            _configuracao);
        ExibirProgressoEtapaInterna("consolidando diagnostico", 2, 3);
        ExibirProgressoEtapaInterna("diagnostico pronto", 3, 3);
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.Secao("Diagnostico de persistencia");
        RenderizadorConsole.ChaveValor("Arquivo esperado", _diagnosticoPersistenciaCerebro.CaminhoArquivo);
        RenderizadorConsole.Status("Cerebro salvo encontrado", _diagnosticoPersistenciaCerebro.ArquivoEncontrado);
        RenderizadorConsole.Status("Texto de treino compativel", _diagnosticoPersistenciaCerebro.TextoTreinoCompativel);
        RenderizadorConsole.Status("Contexto cognitivo compativel", _diagnosticoPersistenciaCerebro.ContextoCognitivoCompativel);
        RenderizadorConsole.Status("Configuracao de treino compativel", _diagnosticoPersistenciaCerebro.ConfiguracaoTreinoCompativel);
        RenderizadorConsole.Status("Mudanca so cognitiva", _diagnosticoPersistenciaCerebro.MudancaSomenteContextoCognitivo);
        RenderizadorConsole.ChaveValor(
            "Modo inicial",
            _diagnosticoPersistenciaCerebro.DeveCarregarCerebroSalvo ? "carregar cerebro salvo" : "treinar cerebro novo",
            _diagnosticoPersistenciaCerebro.DeveCarregarCerebroSalvo ? TomConsole.Sucesso : TomConsole.Aviso);
        RenderizadorConsole.ChaveValor("Motivo", _diagnosticoPersistenciaCerebro.Motivo, TomConsole.Muted);

        if (_diagnosticoPersistenciaCerebro.EstadoEncontrado is not null)
        {
            RenderizadorConsole.LinhaEmBranco();
            RenderizadorConsole.Subtitulo("Resumo do cerebro encontrado");
            RenderizadorConsole.ChaveValor("Epocas gravadas", _diagnosticoPersistenciaCerebro.EstadoEncontrado.EpocasTreinadas);
            RenderizadorConsole.ChaveValor("Perda final gravada", _diagnosticoPersistenciaCerebro.EstadoEncontrado.PerdaMediaFinal.ToString("0.000000"));
            var criadoEmBrasilSp = _diagnosticoPersistenciaCerebro.EstadoEncontrado.CriadoEmBrasilSp != default
                ? _diagnosticoPersistenciaCerebro.EstadoEncontrado.CriadoEmBrasilSp
                : RelogioBrasil.ConverterUtcParaBrasilSp(_diagnosticoPersistenciaCerebro.EstadoEncontrado.CriadoEmUtc);
            RenderizadorConsole.ChaveValor("Criado em Brasil/SP", criadoEmBrasilSp.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        }

        _gravadorArtefatos.GravarJson("00b_persistencia_cerebro.json", _diagnosticoPersistenciaCerebro);
    }

    private void ExibirAdaptacaoHardware()
    {
        _monitorExecucao?.AtualizarEtapa("adaptacao de hardware");
        ExibirCabecalhoEtapa("ETAPA 0A - ADAPTACAO DE HARDWARE", 2);

        ExibirProgressoEtapaInterna("analisando hardware", 1, 3);

        _relatorioAdaptacaoHardware = AnalisadorAdaptacaoHardware.Analisar(new PerfilCargaNumerica
        {
            DimensaoEmbedding = _configuracao.Modelo.DimensaoEmbedding,
            DimensaoFeedForward = _configuracao.Modelo.DimensaoFeedForward,
            TipoCarga = "treino"
        },
        _configuracao.Execucao.Treino,
        _configuracao.Execucao.Inferencia);
        ExibirProgressoEtapaInterna("comparando treino e inferencia", 2, 3);
        ExibirProgressoEtapaInterna("adaptacao concluida", 3, 3);
        FinalizarLinhaProgressoEtapaInterna();
        var hardware = _relatorioAdaptacaoHardware.Hardware;
        var decisaoTreino = _relatorioAdaptacaoHardware.DecisaoTreino;
        var decisaoInferencia = _relatorioAdaptacaoHardware.DecisaoInferencia;
        var melhorGpu = hardware.Gpus.OrderByDescending(x => x.MemoriaBytes).FirstOrDefault();

        RenderizadorConsole.Secao("Hardware detectado");
        RenderizadorConsole.ChaveValor("CPU detectada", hardware.Processador.Nome);
        RenderizadorConsole.ChaveValor("Nucleos logicos", hardware.Processador.NumeroNucleosLogicos);
        RenderizadorConsole.ChaveValor("Clock maximo", $"{hardware.Processador.ClockMaximoMhz} MHz");
        RenderizadorConsole.ChaveValor("Memoria fisica", FormatarBytes(hardware.MemoriaFisicaBytes));

        if (melhorGpu is null)
        {
            RenderizadorConsole.ChaveValor("GPU detectada", "nenhuma", TomConsole.Aviso);
        }
        else
        {
            RenderizadorConsole.ChaveValor("GPU detectada", melhorGpu.Nome);
            RenderizadorConsole.ChaveValor("VRAM estimada", FormatarBytes(melhorGpu.MemoriaBytes));
            RenderizadorConsole.ChaveValor("Driver GPU", melhorGpu.VersaoDriver);
        }

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Pontuacao heuristica");
        RenderizadorConsole.ChaveValor("Pontuacao CPU", decisaoTreino.PontuacaoCpu.ToString("0.00"));
        RenderizadorConsole.ChaveValor("Pontuacao GPU", decisaoTreino.PontuacaoGpu.ToString("0.00"));

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Decisao de treino");
        if (decisaoTreino.TempoBenchmarkCpuMs.HasValue && decisaoTreino.TempoBenchmarkGpuMs.HasValue)
        {
            RenderizadorConsole.ChaveValor("Benchmark CPU", $"{decisaoTreino.TempoBenchmarkCpuMs.Value:0.00} ms");
            RenderizadorConsole.ChaveValor("Benchmark GPU", $"{decisaoTreino.TempoBenchmarkGpuMs.Value:0.00} ms");
            RenderizadorConsole.ChaveValor("Explicacao", decisaoTreino.ExplicacaoDesempenho, TomConsole.Muted);
            RenderizadorConsole.ChaveValor("Limiar GPU", decisaoTreino.LimiarEstimadoGpu, TomConsole.Muted);
            if (decisaoTreino.ComparacoesEscala.Count > 0)
            {
                RenderizadorConsole.Subtitulo("Varredura de escala");
                foreach (var comparacao in decisaoTreino.ComparacoesEscala)
                {
                    RenderizadorConsole.ItemLista(
                        $"{comparacao.Multiplicador,2}x -> emb={comparacao.DimensaoEmbedding}, ff={comparacao.DimensaoFeedForward}, cpu={comparacao.TempoCpuMs:0.00} ms, gpu={comparacao.TempoGpuMs:0.00} ms, melhor={comparacao.BackendMaisRapido}",
                        TomConsole.Padrao);
                }
            }
        }
        RenderizadorConsole.ChaveValor("Melhor pelo hardware", decisaoTreino.ComponenteMaisAdequadoPeloHardware.ToUpperInvariant());
        RenderizadorConsole.ChaveValor("Modo configurado", decisaoTreino.ModoSelecao.ToUpperInvariant());
        RenderizadorConsole.ChaveValor(
            "Componente escolhido",
            decisaoTreino.ComponenteUsadoAgora.ToUpperInvariant(),
            decisaoTreino.ComponenteUsadoAgora == "gpu" ? TomConsole.Sucesso : TomConsole.Info);
        RenderizadorConsole.ChaveValor("Motivo", decisaoTreino.Motivo, TomConsole.Muted);

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Decisao de inferencia");
        if (decisaoInferencia.TempoBenchmarkCpuMs.HasValue && decisaoInferencia.TempoBenchmarkGpuMs.HasValue)
        {
            RenderizadorConsole.ChaveValor("Benchmark CPU", $"{decisaoInferencia.TempoBenchmarkCpuMs.Value:0.00} ms");
            RenderizadorConsole.ChaveValor("Benchmark GPU", $"{decisaoInferencia.TempoBenchmarkGpuMs.Value:0.00} ms");
            RenderizadorConsole.ChaveValor("Explicacao", decisaoInferencia.ExplicacaoDesempenho, TomConsole.Muted);
            RenderizadorConsole.ChaveValor("Limiar GPU", decisaoInferencia.LimiarEstimadoGpu, TomConsole.Muted);
        }
        RenderizadorConsole.ChaveValor("Melhor pelo hardware", decisaoInferencia.ComponenteMaisAdequadoPeloHardware.ToUpperInvariant());
        RenderizadorConsole.ChaveValor("Modo configurado", decisaoInferencia.ModoSelecao.ToUpperInvariant());
        RenderizadorConsole.ChaveValor(
            "Componente escolhido",
            decisaoInferencia.ComponenteUsadoAgora.ToUpperInvariant(),
            decisaoInferencia.ComponenteUsadoAgora == "gpu" ? TomConsole.Sucesso : TomConsole.Info);
        RenderizadorConsole.ChaveValor("Motivo", decisaoInferencia.Motivo, TomConsole.Muted);

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Status("Backend GPU neste build", decisaoTreino.BackendGpuDisponivelNoBuild, "disponivel", "indisponivel");

        _gravadorArtefatos.GravarJson("00_adaptacao_hardware.json", _relatorioAdaptacaoHardware);
        _monitorExecucao?.AtualizarHardware(_relatorioAdaptacaoHardware);
    }

    private void ExibirCorpus()
    {
        _monitorExecucao?.AtualizarEtapa("inspecao do corpus");
        ExibirCabecalhoEtapa("ETAPA 1 - CORPUS", 4);
        RenderizadorConsole.Observacao("Fonte: o programa usa corpus.txt quando ele existe.");
        RenderizadorConsole.Observacao("Secoes [dominio: ...] mudam o contexto cognitivo das linhas seguintes sem exigir codigo novo.");

        var dominios = new Dictionary<string, int>(StringComparer.Ordinal);
        var maiorEntradaNormalizada = 0;
        var cronometroAtualizacao = System.Diagnostics.Stopwatch.StartNew();

        for (var indice = 0; indice < _corpus.Count; indice++)
        {
            var entrada = _corpus[indice];

            if (!dominios.TryAdd(entrada.Dominio, 1))
            {
                dominios[entrada.Dominio]++;
            }

            var tamanhoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(entrada.Texto).Length;
            if (tamanhoNormalizado > maiorEntradaNormalizada)
            {
                maiorEntradaNormalizada = tamanhoNormalizado;
            }

            if (cronometroAtualizacao.ElapsedMilliseconds >= 100 || indice == _corpus.Count - 1)
            {
                ExibirProgressoEtapaInterna("varrendo corpus", indice + 1, _corpus.Count);
                cronometroAtualizacao.Restart();
            }
        }

        FinalizarLinhaProgressoEtapaInterna();
        RenderizadorConsole.Secao("Resumo do corpus");
        RenderizadorConsole.ChaveValor("Entradas lidas", _corpus.Count);
        RenderizadorConsole.ChaveValor("Dominios detectados", dominios.Count);
        RenderizadorConsole.ChaveValor("Maior entrada normalizada", $"{maiorEntradaNormalizada} caracteres");
        RenderizadorConsole.Subtitulo("Dominios mais carregados");
        foreach (var dominio in dominios
                     .OrderByDescending(x => x.Value)
                     .ThenBy(x => x.Key, StringComparer.Ordinal)
                     .Take(12))
        {
            RenderizadorConsole.ItemLista($"{dominio.Key}: entradas={dominio.Value}");
        }

        _gravadorArtefatos.GravarJson("01_corpus.json", _corpus);
    }

    private void ExibirNuvemPalavrasCorpus()
    {
        _monitorExecucao?.AtualizarEtapa("nuvem de palavras do corpus");
        ExibirCabecalhoEtapa("ETAPA 1A - NUVEM DE PALAVRAS", 5);

        _relatorioNuvemPalavras = AnalisadorNuvemPalavrasCorpus.Analisar(
            _corpus,
            (concluido, total, rotulo) => ExibirProgressoEtapaInterna(rotulo, concluido, total));
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.Secao("Resumo da nuvem de palavras");
        RenderizadorConsole.ChaveValor("Palavras de conteudo consideradas", _relatorioNuvemPalavras.TotalPalavrasConsideradas);
        RenderizadorConsole.ChaveValor("Vocabulario de conteudo", _relatorioNuvemPalavras.VocabularioConteudo);
        RenderizadorConsole.Subtitulo("Nuvem textual");
        foreach (var linha in _relatorioNuvemPalavras.LinhasNuvem)
        {
            RenderizadorConsole.Texto("  " + linha, TomConsole.Padrao);
        }

        RenderizadorConsole.Subtitulo("Assuntos mais dominantes");
        foreach (var palavra in _relatorioNuvemPalavras.AssuntosDominantes)
        {
            RenderizadorConsole.ItemLista($"{palavra.Texto}: freq={palavra.Frequencia} | entradas={palavra.EntradasDistintas} | variacao={palavra.VariacaoContexto}");
        }

        RenderizadorConsole.Subtitulo("Palavras com mais variacao de contexto");
        foreach (var palavra in _relatorioNuvemPalavras.PalavrasMaisVariadas)
        {
            RenderizadorConsole.ItemLista($"{palavra.Texto}: variacao={palavra.VariacaoContexto} | freq={palavra.Frequencia} | entradas={palavra.EntradasDistintas}");
        }

        _gravadorArtefatos.GravarJson("01a_nuvem_palavras.json", _relatorioNuvemPalavras);
    }

    private void ExibirMemoriaEpistemica()
    {
        _monitorExecucao?.AtualizarEtapa("memoria epistemica");
        ExibirCabecalhoEtapa("ETAPA 1B - MEMORIA EPISTEMICA", 6);

        _memoriaEpistemica = ConstrutorMemoriaEpistemica.Construir(
            _corpus,
            (concluido, total, rotulo) => ExibirProgressoEtapaInterna(rotulo, concluido, total));
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.Secao("Memoria epistemica");
        RenderizadorConsole.Subtitulo("Formas mais frequentes");
        foreach (var forma in _memoriaEpistemica.Formas.Take(12))
        {
            RenderizadorConsole.ItemLista($"{forma.Texto}: freq={forma.Frequencia}");
        }

        RenderizadorConsole.Subtitulo("Dominios mais ativos");
        foreach (var dominio in _memoriaEpistemica.Entradas
                     .GroupBy(x => x.Dominio, StringComparer.Ordinal)
                     .OrderByDescending(x => x.Count())
                     .ThenBy(x => x.Key, StringComparer.Ordinal)
                     .Take(8))
        {
            RenderizadorConsole.ItemLista($"{dominio.Key}: entradas={dominio.Count()}");
        }

        RenderizadorConsole.Subtitulo("Signos mais fortes");
        foreach (var ligacao in _memoriaEpistemica.Ligacoes.Take(12))
        {
            RenderizadorConsole.ItemLista($"{ligacao.Origem} -> {ligacao.Destino} | tipo={ligacao.Tipo} | forca={ligacao.Forca}");
        }

        RenderizadorConsole.Subtitulo("Quatro pecas da camada cognitiva");
        RenderizadorConsole.ItemLista("memoria: formas, entradas e ligacoes acumuladas do corpus");
        RenderizadorConsole.ItemLista("erro e correcao: formas fora da memoria e tokens desconhecidos no prompt");
        RenderizadorConsole.ItemLista("teste entre representacao e realidade: busca de apoio no corpus para o prompt atual");
        RenderizadorConsole.ItemLista("pragmatica social: classificacao do prompt como pergunta, texto livre ou estrutura explicita");

        _gravadorArtefatos.GravarJson("01b_memoria_epistemica.json", _memoriaEpistemica);
    }

    private Tokenizador ExibirTokenizador()
    {
        _monitorExecucao?.AtualizarEtapa("treino do tokenizador bpe");
        ExibirCabecalhoEtapa("ETAPA 2 - TOKENIZADOR", 7);

        if (_diagnosticoPersistenciaCerebro?.EstadoCompativel is not null)
        {
            ExibirProgressoEtapaInterna("carregando tokenizador salvo", 1, 3);
            _resultadoTreinoTokenizador = new ResultadoTreinoTokenizador(
                _diagnosticoPersistenciaCerebro.EstadoCompativel.ModeloTokenizador,
                Array.Empty<IReadOnlyList<string>>());
            ExibirProgressoEtapaInterna("consolidando vocabulario", 2, 3);
            ExibirProgressoEtapaInterna("tokenizador pronto", 3, 3);
            FinalizarLinhaProgressoEtapaInterna();

            RenderizadorConsole.Sucesso("Tokenizador carregado do cerebro salvo.");
            RenderizadorConsole.ChaveValor("Mesclas aprendidas", _resultadoTreinoTokenizador.Modelo.Mesclas.Count);
            RenderizadorConsole.ChaveValor("Vocabulario final", $"{_resultadoTreinoTokenizador.Modelo.TamanhoVocabulario} tokens");
        }
        else
        {
            var (resultadoTreinoTokenizador, medicaoTokenizador) = MedidorRecursos.Medir(
                "treino do tokenizador bpe",
                () =>
                {
                    var treinadorTokenizador = new TreinadorTokenizadorBpe();
                    return treinadorTokenizador.Treinar(
                        _corpus.Select(ConstrutorExemplosTreino.ComporTextoTreino),
                        _configuracao.MaximoMesclasTokenizador,
                        (concluido, total, rotulo) => ExibirProgressoEtapaInterna(rotulo, concluido, total));
                });
            RegistrarMedicao(medicaoTokenizador);
            _resultadoTreinoTokenizador = resultadoTreinoTokenizador;
            FinalizarLinhaProgressoEtapaInterna();

            RenderizadorConsole.ChaveValor("Mesclas aprendidas", resultadoTreinoTokenizador.Modelo.Mesclas.Count);
            RenderizadorConsole.ChaveValor("Vocabulario final", $"{resultadoTreinoTokenizador.Modelo.TamanhoVocabulario} tokens");
        }

        RenderizadorConsole.Subtitulo("Principais mesclas");
        foreach (var mescla in _resultadoTreinoTokenizador.Modelo.Mesclas.Take(20))
        {
            RenderizadorConsole.ItemLista($"[{TreinadorTokenizadorBpe.RenderizarSimbolo(mescla.Esquerda)}] + [{TreinadorTokenizadorBpe.RenderizarSimbolo(mescla.Direita)}] -> [{TreinadorTokenizadorBpe.RenderizarSimbolo(mescla.Mesclado)}] (freq={mescla.FrequenciaNoTreino})");
        }

        var tokenizador = new Tokenizador(_resultadoTreinoTokenizador.Modelo);
        RenderizadorConsole.Subtitulo("Exemplos tokenizados");
        foreach (var entrada in _corpus.Take(3))
        {
            var textoTreino = ConstrutorExemplosTreino.ComporTextoTreino(entrada);
            var simbolos = tokenizador.CodificarParaSimbolos(textoTreino).Select(tokenizador.RenderizarSimbolo);
            RenderizadorConsole.Texto($"{entrada.Identificador}: {string.Join(" | ", simbolos)}", TomConsole.Muted);
        }

        _gravadorArtefatos.GravarJson("02_modelo_tokenizador.json", _resultadoTreinoTokenizador.Modelo);
        _gravadorArtefatos.GravarJson("02_sequencias_tokenizadas.json", _corpus.Select(x => new
        {
            x.Identificador,
            x.Dominio,
            TextoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(x.Texto),
            TextoTreino = ConstrutorExemplosTreino.ComporTextoTreino(x),
            Simbolos = tokenizador.CodificarParaSimbolos(ConstrutorExemplosTreino.ComporTextoTreino(x)).Select(tokenizador.RenderizarSimbolo).ToList()
        }).ToList());

        return tokenizador;
    }

    private IReadOnlyList<ExemploTreino> ExibirExemplosTreino(Tokenizador tokenizador)
    {
        _monitorExecucao?.AtualizarEtapa("montagem dos exemplos de treino");
        ExibirCabecalhoEtapa("ETAPA 3 - IDS E EXEMPLOS", 8);

        var (exemplosTreino, medicaoExemplos) = MedidorRecursos.Medir(
            "montagem dos exemplos de treino",
            () => ConstrutorExemplosTreino.Construir(
                _corpus,
                tokenizador,
                _configuracao.MaximoComprimentoSequencia,
                (concluido, total, rotulo) => ExibirProgressoEtapaInterna(rotulo, concluido, total)));
        RegistrarMedicao(medicaoExemplos);
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.ChaveValor("Exemplos montados", exemplosTreino.Count);
        if (exemplosTreino.Count > _corpus.Count)
        {
            RenderizadorConsole.Alerta($"Linhas longas foram divididas em {exemplosTreino.Count - _corpus.Count} segmentos extras para caber no limite de {_configuracao.MaximoComprimentoSequencia} tokens.");
        }

        foreach (var exemplo in exemplosTreino.Take(3))
        {
            RenderizadorConsole.Subtitulo($"Exemplo {exemplo.Identificador}");
            RenderizadorConsole.ChaveValor("Dominio", exemplo.Dominio);
            RenderizadorConsole.ChaveValor("Texto", exemplo.TextoOriginal);
            RenderizadorConsole.ChaveValor("Ids de entrada", string.Join(", ", exemplo.IdsEntrada), TomConsole.Muted);
            RenderizadorConsole.ChaveValor("Ids alvo", string.Join(", ", exemplo.IdsAlvo), TomConsole.Muted);
            RenderizadorConsole.ChaveValor("Pesos perda", string.Join(", ", exemplo.PesosPerda.Select(x => x.ToString("0.0"))), TomConsole.Muted);
        }

        _gravadorArtefatos.GravarJson("03_exemplos_treino.json", exemplosTreino.Select(x => new
        {
            x.Identificador,
            x.Dominio,
            x.TextoOriginal,
            x.IdsCompletos,
            x.PesosPerda,
            TextoDecodificado = tokenizador.Decodificar(x.IdsCompletos)
        }).ToList());

        return exemplosTreino;
    }

    private MiniTransformador ExibirModelo(Tokenizador tokenizador)
    {
        _monitorExecucao?.AtualizarEtapa("inicializacao do modelo");
        ExibirCabecalhoEtapa("ETAPA 4 - MODELO", 9);

        ExibirProgressoEtapaInterna("preparando hiperparametros", 1, 4);

        var hiperParametros = new HiperParametrosTransformador
        {
            TamanhoVocabulario = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.TamanhoVocabulario ?? _resultadoTreinoTokenizador!.Modelo.TamanhoVocabulario,
            MaximoComprimentoSequencia = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.MaximoComprimentoSequencia ?? _configuracao.MaximoComprimentoSequencia,
            DimensaoEmbedding = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.DimensaoEmbedding ?? _configuracao.Modelo.DimensaoEmbedding,
            DimensaoFeedForward = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.DimensaoFeedForward ?? _configuracao.Modelo.DimensaoFeedForward,
            NumeroCamadas = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.NumeroCamadas ?? _configuracao.Modelo.NumeroCamadas,
            NumeroCabecasAtencao = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.NumeroCabecasAtencao ?? _configuracao.Modelo.NumeroCabecasAtencao,
            EpsilonLayerNorm = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.EpsilonLayerNorm ?? _configuracao.Modelo.EpsilonLayerNorm,
            TaxaAprendizado = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.TaxaAprendizado ?? _configuracao.Modelo.TaxaAprendizado,
            SementeAleatoria = _diagnosticoPersistenciaCerebro?.EstadoCompativel?.HiperParametros.SementeAleatoria ?? _configuracao.SementeAleatoria
        };
        ExibirProgressoEtapaInterna("selecionando backend", 2, 4);

        var contextoHardware = _diagnosticoPersistenciaCerebro?.DeveCarregarCerebroSalvo == true
            ? _relatorioAdaptacaoHardware?.DecisaoInferencia
            : _relatorioAdaptacaoHardware?.DecisaoTreino;

        contextoHardware ??= new DecisaoExecucaoHardware
        {
            TipoCarga = "inferencia",
            ModoSelecao = "auto",
            ComponenteMaisAdequadoPeloHardware = "cpu",
            ComponenteUsadoAgora = "cpu",
            BackendGpuDisponivelNoBuild = false,
            Motivo = "fallback padrao em CPU"
        };
        var (executorNumerico, contextoBackend) = FabricaExecutorNumerico.Criar(contextoHardware);
        ExibirProgressoEtapaInterna(
            _diagnosticoPersistenciaCerebro?.EstadoCompativel is not null ? "carregando modelo salvo" : "instanciando modelo",
            3,
            4);

        MiniTransformador modelo;
        if (_diagnosticoPersistenciaCerebro?.EstadoCompativel is not null)
        {
            var (modeloCarregado, medicaoModelo) = MedidorRecursos.Medir(
                "carregamento do modelo salvo",
                () =>
                {
                    var modeloNovo = new MiniTransformador(hiperParametros, executorNumerico, contextoBackend);
                    modeloNovo.AplicarEstado(_diagnosticoPersistenciaCerebro.EstadoCompativel.EstadoModelo);
                    return modeloNovo;
                });
            RegistrarMedicao(medicaoModelo);
            modelo = modeloCarregado;
        }
        else
        {
            var (modeloNovo, medicaoModelo) = MedidorRecursos.Medir("inicializacao do modelo", () => new MiniTransformador(hiperParametros, executorNumerico, contextoBackend));
            RegistrarMedicao(medicaoModelo);
            modelo = modeloNovo;
        }
        ExibirProgressoEtapaInterna("modelo pronto", 4, 4);
        FinalizarLinhaProgressoEtapaInterna();

        var retratoModelo = modelo.Descrever();
        _monitorExecucao?.AtualizarEtapa("modelo pronto", retratoModelo.DispositivoExecucao);
        RenderizadorConsole.Secao("Resumo do modelo");
        RenderizadorConsole.ChaveValor("Backend numerico", retratoModelo.NomeBackendNumerico);
        RenderizadorConsole.ChaveValor("Dispositivo em uso", retratoModelo.DispositivoExecucao);
        RenderizadorConsole.ChaveValor("Dispositivo preferido", retratoModelo.DispositivoPreferidoPeloHardware);
        RenderizadorConsole.ChaveValor("Vocabulario", retratoModelo.TamanhoVocabulario);
        RenderizadorConsole.ChaveValor("Comprimento maximo", retratoModelo.MaximoComprimentoSequencia);
        RenderizadorConsole.ChaveValor("Dimensao embedding", retratoModelo.DimensaoEmbedding);
        RenderizadorConsole.ChaveValor("Dimensao feed-forward", retratoModelo.DimensaoFeedForward);
        RenderizadorConsole.ChaveValor("Numero de camadas", retratoModelo.NumeroCamadas);
        RenderizadorConsole.ChaveValor("Numero de cabecas", retratoModelo.NumeroCabecasAtencao);
        RenderizadorConsole.ChaveValor("Dimensao por cabeca", retratoModelo.DimensaoCabeca);
        RenderizadorConsole.ChaveValor("Epsilon layer norm", retratoModelo.EpsilonLayerNorm.ToString("0.0#####"));
        RenderizadorConsole.ChaveValor("Taxa de aprendizado", retratoModelo.TaxaAprendizado);
        RenderizadorConsole.ChaveValor("Origem do cerebro", _diagnosticoPersistenciaCerebro?.EstadoCompativel is not null ? "arquivo salvo" : "novo treino");

        foreach (var forma in retratoModelo.Formas)
        {
            RenderizadorConsole.ItemLista($"{forma.Key} -> {forma.Value}", TomConsole.Muted);
        }

        _gravadorArtefatos.GravarJson("04_retrato_modelo.json", retratoModelo);
        return modelo;
    }

    private void ExibirTreino(MiniTransformador modelo, IReadOnlyList<ExemploTreino> exemplosTreino)
    {
        _monitorExecucao?.AtualizarEtapa("treino do modelo", modelo.ContextoBackend.DispositivoUsado);
        ExibirCabecalhoEtapa("ETAPA 5 - TREINO", 10);

        if (_diagnosticoPersistenciaCerebro?.EstadoCompativel is not null)
        {
            ExibirProgressoEtapaInterna("reaproveitando cerebro salvo", 1, 3);
            _resultadoTreino = new ResultadoTreino
            {
                IniciadoEmBrasilSp = _diagnosticoPersistenciaCerebro.EstadoCompativel.CriadoEmBrasilSp != default
                    ? _diagnosticoPersistenciaCerebro.EstadoCompativel.CriadoEmBrasilSp
                    : RelogioBrasil.ConverterUtcParaBrasilSp(_diagnosticoPersistenciaCerebro.EstadoCompativel.CriadoEmUtc),
                ConcluidoEmBrasilSp = RelogioBrasil.AgoraBrasilSp(),
                PerdasEpoca = new List<PerdaEpoca>
                {
                    new()
                    {
                        Epoca = _diagnosticoPersistenciaCerebro.EstadoCompativel.EpocasTreinadas,
                        PerdaMedia = _diagnosticoPersistenciaCerebro.EstadoCompativel.PerdaMediaFinal,
                        MomentoBrasilSp = RelogioBrasil.AgoraBrasilSp()
                    }
                }
            };
            ExibirProgressoEtapaInterna("consolidando historico de treino", 2, 3);
            _monitorExecucao?.AtualizarResultadoTreino(_resultadoTreino);
            ExibirProgressoEtapaInterna("treino concluido por carga", 3, 3);
            FinalizarLinhaProgressoEtapaInterna();

            RenderizadorConsole.Sucesso("Treino pulado: cerebro salvo compativel foi carregado.");
            RenderizadorConsole.ChaveValor("Epocas gravadas", _diagnosticoPersistenciaCerebro.EstadoCompativel.EpocasTreinadas);
            RenderizadorConsole.ChaveValor("Perda final gravada", _diagnosticoPersistenciaCerebro.EstadoCompativel.PerdaMediaFinal.ToString("0.000000"));

            if (_diagnosticoPersistenciaCerebro.MudancaSomenteContextoCognitivo)
            {
                RenderizadorConsole.Alerta("Os pesos continuam validos, mas houve mudanca de contexto cognitivo no corpus atual.");
            }
            return;
        }

        var planoTreino = PlanejadorTreinoAdaptativo.Planejar(modelo, exemplosTreino, _configuracao);

        var paradaAntecipadaAtivada = _configuracao.Modelo.EstaParadaAntecipadaAtivada();
        var pacienciaParadaAntecipada = _configuracao.Modelo.ObterPacienciaParadaAntecipadaEfetiva();

        RenderizadorConsole.Secao("Plano de treino");
        RenderizadorConsole.ChaveValor("Exemplos disponiveis", planoTreino.TotalExemplosDisponiveis);
        RenderizadorConsole.ChaveValor("Dispositivo de treino", planoTreino.DispositivoPlanejado);
        RenderizadorConsole.ChaveValor(
            "Plano bruto",
            $"{planoTreino.EpocasSolicitadas} epocas x {planoTreino.ExemplosPorEpocaSolicitados} exemplos/epoca = {planoTreino.PassosSolicitados} passos");
        RenderizadorConsole.ChaveValor(
            "Plano efetivo",
            $"{planoTreino.EpocasEfetivas} epocas x {planoTreino.ExemplosPorEpocaEfetivos} exemplos/epoca = {planoTreino.PassosEfetivos} passos");
        RenderizadorConsole.Status("Treino adaptativo automatico", planoTreino.PlanejamentoAutomaticoAtivo);
        RenderizadorConsole.Status("Plano adaptado", planoTreino.PlanoAdaptado);
        RenderizadorConsole.Status("Embaralhar exemplos por epoca", _configuracao.Modelo.EmbaralharExemplosPorEpoca);
        RenderizadorConsole.ChaveValor(
            "Parada antecipada",
            paradaAntecipadaAtivada
                ? $"sim, paciencia={pacienciaParadaAntecipada}, melhora minima={_configuracao.Modelo.MelhoraMinimaPerda:0.######}"
                : "nao",
            paradaAntecipadaAtivada ? TomConsole.Info : TomConsole.Muted);
        RenderizadorConsole.ChaveValor("Benchmark de planejamento", $"{planoTreino.PassosAmostraBenchmark} passos de amostra");
        RenderizadorConsole.ChaveValor("Velocidade estimada", $"{planoTreino.ExemplosPorSegundoEstimados:0.0} ex/s");
        RenderizadorConsole.ChaveValor("Orcamento de tempo", FormatarDuracaoLonga(TimeSpan.FromSeconds(planoTreino.SegundosOrcamento)));
        RenderizadorConsole.ChaveValor("Estimativa bruta", FormatarDuracaoLonga(TimeSpan.FromSeconds(planoTreino.SegundosEstimadosPlanoSolicitado)));
        RenderizadorConsole.ChaveValor("Estimativa efetiva", FormatarDuracaoLonga(TimeSpan.FromSeconds(planoTreino.SegundosEstimadosPlanoEfetivo)));
        RenderizadorConsole.ChaveValor("Motivo do plano", planoTreino.Motivo, TomConsole.Muted);

        var medicaoTreinoComResultado = MedidorRecursos.Medir(
            "treino do modelo",
            () =>
            {
                var treinador = new Treinador(modelo);
                return treinador.Treinar(
                    exemplosTreino,
                    planoTreino.EpocasEfetivas,
                    planoTreino.ExemplosPorEpocaEfetivos,
                    planoTreino.MaximoPassosTreinoEfetivo,
                    _configuracao.Modelo.EmbaralharExemplosPorEpoca,
                    pacienciaParadaAntecipada,
                    _configuracao.Modelo.MelhoraMinimaPerda,
                    _configuracao.SementeAleatoria,
                    atualizacao =>
                {
                    ExibirProgressoTreino(atualizacao);
                    _monitorExecucao?.AtualizarProgressoTreino(new EstadoProgressoTreinoTempoReal
                    {
                        EpocaAtual = atualizacao.EpocaAtual,
                        TotalEpocas = atualizacao.TotalEpocas,
                        PercentualConclusao = atualizacao.PercentualConclusao,
                        PerdaMediaAtual = atualizacao.PerdaMediaAtual,
                        EpocasPorSegundo = atualizacao.EpocasPorSegundo,
                        ExemplosPorSegundo = atualizacao.ExemplosPorSegundo,
                        SegundosDecorridos = atualizacao.TempoDecorrido.TotalSeconds,
                        SegundosRestantesEstimados = atualizacao.TempoRestanteEstimado.TotalSeconds
                    });
                });
            });
        var resultadoTreino = medicaoTreinoComResultado.Resultado;
        var medicaoTreino = medicaoTreinoComResultado.Medicao;
        FinalizarLinhaProgressoTreino();
        RegistrarMedicao(medicaoTreino);
        _resultadoTreino = resultadoTreino;
        _monitorExecucao?.AtualizarResultadoTreino(_resultadoTreino);

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Perda por epoca");
        foreach (var perdaEpoca in resultadoTreino.PerdasEpoca.Where(x => x.Epoca == 1 || x.Epoca % 25 == 0 || x.Epoca == resultadoTreino.EpocasExecutadas))
        {
            RenderizadorConsole.ItemLista($"Epoca {perdaEpoca.Epoca,4}: perda media = {perdaEpoca.PerdaMedia:0.000000}");
        }

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Resumo do treino");
        RenderizadorConsole.ChaveValor("Epocas executadas", $"{resultadoTreino.EpocasExecutadas}/{resultadoTreino.EpocasPlanejadas}");
        RenderizadorConsole.ChaveValor("Passos executados", $"{resultadoTreino.PassosExecutados}/{resultadoTreino.PassosPlanejados}");
        RenderizadorConsole.ChaveValor("Motivo de parada", resultadoTreino.MotivoParada, TomConsole.Muted);

        _gravadorArtefatos.GravarJson("05a_plano_treino.json", planoTreino);
        _gravadorArtefatos.GravarJson("05_log_treino.json", resultadoTreino);
        PersistenciaCerebroTreinado.Salvar(
            _gravadorArtefatos.Raiz,
            _corpus,
            _configuracao,
            _resultadoTreinoTokenizador!.Modelo,
            modelo,
            resultadoTreino);
        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Sucesso($"Cerebro salvo em: {Path.Combine(_gravadorArtefatos.Raiz, "10_cerebro_treinado.json")}");
    }

    private IReadOnlyList<int> ExibirPassagemDireta(MiniTransformador modelo, Tokenizador tokenizador, AnalisePromptEpistemica analisePrompt)
    {
        _monitorExecucao?.AtualizarEtapa("passagem direta", modelo.ContextoBackend.DispositivoUsado);
        ExibirCabecalhoEtapa("ETAPA 6 - PASSAGEM DIRETA", 11);

        ExibirProgressoEtapaInterna("preparando prompt", 1, 4);

        var idsInspecao = tokenizador.Codificar(analisePrompt.PromptEnriquecido, adicionarInicio: true, adicionarFim: false);
        ExibirProgressoEtapaInterna("executando passagem direta", 2, 4);
        var (cacheInspecao, medicaoPassagemDireta) = MedidorRecursos.Medir(
            "passagem direta de inspecao",
            () => modelo.PassagemDireta(idsInspecao));
        RegistrarMedicao(medicaoPassagemDireta);
        ExibirProgressoEtapaInterna("consolidando estados e atencao", 3, 4);

        var ultimoBloco = cacheInspecao.Blocos[^1];
        var ultimaCabeca = ultimoBloco.CabecasAtencao[^1];
        ExibirProgressoEtapaInterna("passagem direta pronta", 4, 4);
        FinalizarLinhaProgressoEtapaInterna();

        ExibirAnalisePrompt(analisePrompt);
        RenderizadorConsole.ChaveValor("Prompt base", analisePrompt.PromptBase, TomConsole.Muted);
        RenderizadorConsole.ChaveValor("Prompt usado", analisePrompt.PromptEnriquecido);
        RenderizadorConsole.ChaveValor("Ids", string.Join(", ", idsInspecao), TomConsole.Muted);
        RenderizadorConsole.LinhaEmBranco();

        for (var i = 0; i < idsInspecao.Count; i++)
        {
            RenderizadorConsole.Texto(
                $"Posicao {i,2} | token={tokenizador.DecodificarUnico(idsInspecao[i]),-18} | embedding={OperacoesMatematicas.FormatarVetor(cacheInspecao.EmbeddingEntrada[i])}",
                TomConsole.Padrao);
            RenderizadorConsole.Observacao($"            | estado final={OperacoesMatematicas.FormatarVetor(cacheInspecao.EstadoFinal[i])}");
        }

        RenderizadorConsole.LinhaEmBranco();
        RenderizadorConsole.Subtitulo("Ultima linha da autoatencao causal do ultimo bloco e ultima cabeca");
        RenderizadorConsole.Texto(OperacoesMatematicas.FormatarVetor(ultimaCabeca.Probabilidades[^1], idsInspecao.Count), TomConsole.Muted);

        RenderizadorConsole.Subtitulo("Top 5 probabilidades para o proximo token");
        var ultimasProbabilidades = cacheInspecao.Probabilidades[^1];
        foreach (var top in OperacoesMatematicas.TopK(ultimasProbabilidades, 5))
        {
            RenderizadorConsole.ItemLista($"token={tokenizador.DecodificarUnico(top.Indice),-18} id={top.Indice,-4} p={top.Valor:0.00%}");
        }

        _gravadorArtefatos.GravarJson("06a_analise_prompt_epistemica.json", analisePrompt);
        _gravadorArtefatos.GravarJson("06_passagem_direta.json", new
        {
            PromptBase = analisePrompt.PromptBase,
            PromptUsado = analisePrompt.PromptEnriquecido,
            IdsEntrada = idsInspecao,
            Tokens = idsInspecao.Select(tokenizador.DecodificarUnico).ToArray(),
            EmbeddingEntrada = cacheInspecao.EmbeddingEntrada,
            EstadoFinal = cacheInspecao.EstadoFinal,
            AtencaoUltimoBlocoUltimaCabeca = ultimaCabeca.Probabilidades,
            Top5UltimaPosicao = OperacoesMatematicas.TopK(ultimasProbabilidades, 5).Select(x => new
            {
                x.Indice,
                Token = tokenizador.DecodificarUnico(x.Indice),
                x.Valor
            }).ToList()
        });

        return idsInspecao;
    }

    private void ExibirGeracaoExemplo(Gerador gerador, AnalisePromptEpistemica analisePrompt)
    {
        _monitorExecucao?.AtualizarEtapa("geracao de exemplo");
        ExibirCabecalhoEtapa("ETAPA 7 - GERACAO", 12);

        var (rastroGeracao, medicaoGeracao) = MedidorRecursos.Medir(
            "geracao de exemplo",
            () =>
            {
                var rastro = new RastroGeracao
                {
                    PromptInterno = analisePrompt.PromptEnriquecido,
                    ModoSelecaoToken = gerador.DescreverModoSelecao(),
                    Temperatura = gerador.ParametrosAmostragem.Temperatura > 0.0 ? gerador.ParametrosAmostragem.Temperatura : 1.0,
                    TopK = Math.Max(0, gerador.ParametrosAmostragem.TopK),
                    TopP = gerador.ParametrosAmostragem.TopP <= 0.0 || gerador.ParametrosAmostragem.TopP >= 1.0
                        ? 1.0
                        : gerador.ParametrosAmostragem.TopP
                };

                foreach (var passo in gerador.GerarEmFluxo(analisePrompt.PromptEnriquecido, _configuracao.Geracao.MaximoNovosTokens))
                {
                    rastro.Passos.Add(passo);
                    ExibirProgressoEtapaInterna("gerando texto", passo.Passo, _configuracao.Geracao.MaximoNovosTokens);
                }

                rastro.TextoCompletoDecodificado = rastro.Passos.LastOrDefault()?.TextoParcialDecodificado ?? string.Empty;
                var totalPassos = Math.Max(rastro.Passos.Count, 1);
                ExibirProgressoEtapaInterna("geracao concluida", totalPassos, totalPassos);
                return rastro;
            });
        RegistrarMedicao(medicaoGeracao);
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.ChaveValor("Prompt usado", analisePrompt.PromptEnriquecido);
        RenderizadorConsole.ChaveValor("Modo de geracao", gerador.DescreverModoSelecao(), TomConsole.Info);
        RenderizadorConsole.LinhaEmBranco();

        foreach (var passo in rastroGeracao.Passos)
        {
            ExibirPassoGeracao(passo);
        }

        RenderizadorConsole.ChaveValor("Resposta final", rastroGeracao.TextoCompletoDecodificado, TomConsole.Sucesso);
        _gravadorArtefatos.GravarJson("07_rastro_geracao.json", rastroGeracao);
    }

    private void ExibirEscalaERecursos(MiniTransformador modelo, int comprimentoSequenciaAtiva)
    {
        _monitorExecucao?.AtualizarEtapa("relatorio de escala e recursos");
        ExibirCabecalhoEtapa("ETAPA 8 - ESCALA E RECURSOS", 13);

        ExibirProgressoEtapaInterna("medindo escala do modelo", 1, 4);
        var relatorioEscala = modelo.GerarRelatorioEscala(comprimentoSequenciaAtiva);
        ExibirProgressoEtapaInterna("consolidando parametros ativos", 2, 4);
        ExibirProgressoEtapaInterna("preparando relatorio", 3, 4);
        ExibirProgressoEtapaInterna("relatorio pronto", 4, 4);
        FinalizarLinhaProgressoEtapaInterna();

        RenderizadorConsole.Secao("Resumo de escala");
        RenderizadorConsole.ChaveValor("Processador detectado", ObterDescricaoProcessador());
        RenderizadorConsole.ChaveValor("Arquitetura do processo", RuntimeInformation.ProcessArchitecture);
        RenderizadorConsole.ChaveValor("Nucleos logicos visiveis", Environment.ProcessorCount);
        RenderizadorConsole.ChaveValor("Comprimento de sequencia usado", relatorioEscala.ComprimentoSequenciaAtiva);
        RenderizadorConsole.ChaveValor("Total de neuronios ativos", relatorioEscala.TotalNeuroniosAtivos);
        RenderizadorConsole.ChaveValor("Total de pesos/sinapses", relatorioEscala.TotalPesosTreinaveis);
        RenderizadorConsole.ChaveValor("Total de vieses", relatorioEscala.TotalVieses);
        RenderizadorConsole.ChaveValor("Total de parametros treinaveis", relatorioEscala.TotalParametrosTreinaveis);
        RenderizadorConsole.ChaveValor("Memoria dos parametros", FormatarBytes(relatorioEscala.MemoriaParametrosBytes));

        RenderizadorConsole.Subtitulo("Pesos, sinapses e memoria por componente");
        foreach (var componente in relatorioEscala.ComponentesParametros)
        {
            RenderizadorConsole.ItemLista($"{componente.Nome}: tipo={componente.Tipo} | forma={componente.Forma} | pesos/sinapses={componente.QuantidadePesos} | vieses={componente.QuantidadeVieses} | memoria={FormatarBytes(componente.MemoriaBytes)}");
        }

        RenderizadorConsole.Subtitulo("Neuronios ativos por camada no exemplo");
        foreach (var camada in relatorioEscala.CamadasAtivas)
        {
            RenderizadorConsole.ItemLista($"{camada.Nome}: tipo={camada.Tipo} | forma={camada.Forma} | neuronios={camada.QuantidadeNeuronios}");
        }

        RenderizadorConsole.Subtitulo("Uso real do processo por etapa");
        foreach (var medicao in _medicoes)
        {
            var cpuMedioTexto = medicao.CpuMedioConfiavel
                ? $"{medicao.PercentualCpuMedio:0.0}%"
                : "n/d (amostra curta)";

            RenderizadorConsole.ItemLista($"{medicao.NomeEtapa}: inicio={medicao.IniciadoEmBrasilSp:yyyy-MM-dd HH:mm:ss} | fim={medicao.ConcluidoEmBrasilSp:yyyy-MM-dd HH:mm:ss} | tempo={medicao.MilissegundosParede} ms | cpu={medicao.MilissegundosCpu} ms | cpu medio={cpuMedioTexto} | heap={FormatarBytes(medicao.MemoriaGerenciadaDepoisBytes)} | working set={FormatarBytes(medicao.WorkingSetDepoisBytes)} | privado={FormatarBytes(medicao.MemoriaPrivadaDepoisBytes)}");
        }

        _gravadorArtefatos.GravarJson("08_relatorio_escala.json", new
        {
            Processador = ObterDescricaoProcessador(),
            Arquitetura = RuntimeInformation.ProcessArchitecture.ToString(),
            NucleosLogicos = Environment.ProcessorCount,
            RelatorioEscala = relatorioEscala
        });
        _gravadorArtefatos.GravarJson("09_medicoes_recursos.json", _medicoes);
    }

    private void ExibirChatInterativo(Gerador gerador)
    {
        _monitorExecucao?.AtualizarEtapa("chat interativo");
        ExibirCabecalhoEtapa("ETAPA 9 - CHAT INTERATIVO", 14);
        ExibirProgressoEtapaInterna("preparando chat interativo", 1, 1);
        FinalizarLinhaProgressoEtapaInterna();
        var memoriaConversacional = ObterMemoriaConversacional();
        RenderizadorConsole.Observacao("Digite uma pergunta para usar o formato pergunta/resposta.");
        RenderizadorConsole.Observacao("Para texto livre, comece com texto:");
        RenderizadorConsole.Observacao("Exemplo: texto: o gato gosta");
        RenderizadorConsole.Observacao("A camada cognitiva tenta recuperar memoria e medir alteridade antes de gerar.");
        RenderizadorConsole.Observacao("Digite vazio para sair.");
        RenderizadorConsole.LinhaEmBranco();

        while (true)
        {
            var entradaUsuario = RenderizadorConsole.LerPrompt("Voce> ");

            if (string.IsNullOrWhiteSpace(entradaUsuario))
            {
                RenderizadorConsole.Observacao("Encerrando.");
                break;
            }

            var analisePrompt = CriarAnalisePrompt(gerador.Tokenizador, entradaUsuario);
            ExibirAnalisePrompt(analisePrompt);
            RenderizadorCortexConversacional.ExibirThink(analisePrompt.EstadoCognitivo, _configuracao.Cognicao);
            RenderizadorConsole.ChaveValor("Prompt interno", analisePrompt.PromptEnriquecido, TomConsole.Muted);

            string textoAtual;
            var respostaCognitiva = ObterCortexConversacional()?.TentarResponderComEstado(analisePrompt) ?? string.Empty;

            if (CortexConversacional.DevePriorizarRespostaCognitiva(analisePrompt, respostaCognitiva))
            {
                RenderizadorConsole.Subtitulo("Resposta cognitiva segura");
                textoAtual = respostaCognitiva;
                RenderizadorConsole.ChaveValor("Texto", textoAtual, TomConsole.Sucesso);
            }
            else
            {
                RenderizadorConsole.Subtitulo("Geracao ao vivo");
                textoAtual = string.Empty;

                foreach (var passo in gerador.GerarEmFluxo(analisePrompt.PromptEnriquecido, _configuracao.Geracao.MaximoNovosTokens))
                {
                    textoAtual = passo.TextoParcialDecodificado;
                    ExibirPassoGeracao(passo);

                    if (_configuracao.Geracao.AtrasoFluxoMs > 0)
                    {
                        Thread.Sleep(_configuracao.Geracao.AtrasoFluxoMs);
                    }
                }
            }

            var respostaBaseadaEmEvidencias = RespondedorBaseadoEmEvidencias.TentarResponder(
                analisePrompt,
                _memoriaEpistemica!);
            if (!string.IsNullOrWhiteSpace(respostaBaseadaEmEvidencias) &&
                !GestorMemoriaConversacional.TextoEstaDegradado(respostaBaseadaEmEvidencias) &&
                (GestorMemoriaConversacional.TextoEstaDegradado(textoAtual) ||
                 SintetizadorRespostaContextual.RespostaPareceDesalinhada(analisePrompt.EntradaOriginal, textoAtual)))
            {
                textoAtual = respostaBaseadaEmEvidencias;
            }

            if (!string.IsNullOrWhiteSpace(respostaCognitiva) &&
                !GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva) &&
                (GestorMemoriaConversacional.TextoEstaDegradado(textoAtual) ||
                 SintetizadorRespostaContextual.RespostaPareceDesalinhada(analisePrompt.EntradaOriginal, textoAtual)))
            {
                textoAtual = respostaCognitiva;
            }

            if (GestorMemoriaConversacional.TextoEstaDegradado(textoAtual))
            {
                textoAtual = "ainda nao consegui formar uma resposta limpa com o treino atual.";
            }

            textoAtual = TreinadorTokenizadorBpe.FormatarTextoParaExibicao(textoAtual);
            RenderizadorConsole.Prompt("Modelo> ", TomConsole.Sucesso);
            RenderizadorConsole.Texto(textoAtual);
            RenderizadorConsole.LinhaEmBranco();
            memoriaConversacional.RegistrarTurnoUsuario(entradaUsuario);
            ObterMotorFerramentas().RegistrarTurnoUsuario(entradaUsuario);
            var podePersistirResposta = RespondedorBaseadoEmEvidencias.PodePersistirResposta(
                    analisePrompt,
                    _memoriaEpistemica!,
                    textoAtual);
            if (podePersistirResposta)
            {
                memoriaConversacional.RegistrarTurnoModelo(textoAtual);
                ObterMotorFerramentas().RegistrarTurnoModelo(textoAtual);
            }

            ObterCortexConversacional()?.RegistrarResultadoTurno(
                analisePrompt.EstadoCognitivo,
                entradaUsuario,
                textoAtual,
                DeterminarFonteResposta(textoAtual, respostaBaseadaEmEvidencias, respostaCognitiva),
                podePersistirResposta ? "boa" : "baixa");
        }
    }

    private void ExibirPassoGeracao(PassoTokenGerado passo)
    {
        RenderizadorConsole.Subtitulo($"Passo {passo.Passo}");
        RenderizadorConsole.ChaveValor("Fragmento", passo.TextoTokenPrevisto);
        RenderizadorConsole.ChaveValor("Escolha", passo.ProbabilidadeTokenPrevisto.ToString("0.00%"), TomConsole.Info);
        RenderizadorConsole.ChaveValor("Top", FormatarMelhoresEscolhas(passo.MelhoresEscolhas), TomConsole.Muted);
        RenderizadorConsole.ChaveValor("Texto", passo.TextoParcialDecodificado, TomConsole.Padrao);
    }

    private string FormatarMelhoresEscolhas(IEnumerable<ProbabilidadeToken> melhoresEscolhas)
    {
        var maximoAlternativas = System.Math.Max(1, _configuracao.Geracao.MaximoAlternativasExibidas);

        return string.Join(
            " | ",
            melhoresEscolhas
                .Take(maximoAlternativas)
                .Select(escolha => $"{escolha.TextoToken} ({escolha.Probabilidade:0.00%})"));
    }

    private string SolicitarEntradaManual()
    {
        RenderizadorConsole.Observacao("Digite a pergunta ou texto para a inspecao.");
        RenderizadorConsole.Observacao("Se quiser continuacao livre, comece com texto:");

        while (true)
        {
            var entradaUsuario = RenderizadorConsole.LerPrompt("Prompt> ");

            if (!string.IsNullOrWhiteSpace(entradaUsuario))
            {
                RenderizadorConsole.LinhaEmBranco();
                return entradaUsuario;
            }
        }
    }

    private AnalisePromptEpistemica CriarAnalisePrompt(Tokenizador tokenizador, string entradaUsuario)
    {
        if (_memoriaEpistemica is null)
        {
            throw new InvalidOperationException("A memoria epistemica precisa existir antes da analise do prompt.");
        }

        var analisador = new AnalisadorEpistemicoPrompt(_memoriaEpistemica, tokenizador);
        var analiseBase = analisador.Analisar(entradaUsuario);
        var contextoConversacional = ObterMemoriaConversacional().CriarContexto(entradaUsuario);
        var contextoFerramentas = ObterMotorFerramentas().CriarContexto(entradaUsuario);
        var estadoCognitivo = ObterCortexConversacional()?.PrepararTurno(
            analiseBase,
            contextoConversacional,
            contextoFerramentas);
        return EnriquecerAnaliseComMemoriaConversacional(
            analiseBase,
            contextoConversacional,
            contextoFerramentas,
            estadoCognitivo,
            tokenizador,
            _configuracao.MaximoComprimentoSequencia);
    }

    private static void ExibirAnalisePrompt(AnalisePromptEpistemica analisePrompt)
    {
        RenderizadorConsole.Secao("Analise do prompt");
        RenderizadorConsole.ChaveValor("Pragmatica", analisePrompt.TipoPragmatica);
        RenderizadorConsole.ChaveValor("Apoio", analisePrompt.TipoApoioPredominante);
        if (!string.IsNullOrWhiteSpace(analisePrompt.NomeIdentidade))
        {
            RenderizadorConsole.ChaveValor("Identidade", analisePrompt.NomeIdentidade, TomConsole.Sucesso);
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.TempoDeVidaTexto))
        {
            RenderizadorConsole.ChaveValor("Tempo de vida", analisePrompt.TempoDeVidaTexto);
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.DominioPredominante))
        {
            RenderizadorConsole.ChaveValor("Dominio cognitivo", analisePrompt.DominioPredominante);
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.AmbienteBase))
        {
            RenderizadorConsole.ChaveValor("Ambiente", analisePrompt.AmbienteBase);
        }

        if (analisePrompt.FormasAtivas.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Formas", string.Join(", ", analisePrompt.FormasAtivas));
        }

        if (analisePrompt.FormasForaDaMemoria.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Alteridade formas", string.Join(", ", analisePrompt.FormasForaDaMemoria), TomConsole.Aviso);
        }

        if (analisePrompt.SimbolosDesconhecidos.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Alteridade tokens", string.Join(", ", analisePrompt.SimbolosDesconhecidos), TomConsole.Aviso);
        }

        if (analisePrompt.TrechosRecuperados.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Memoria recuperada", string.Join(" | ", analisePrompt.TrechosRecuperados), TomConsole.Muted);
        }

        if (analisePrompt.MemoriaCurta.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Memoria curta", string.Join(" | ", analisePrompt.MemoriaCurta.Take(3)), TomConsole.Muted);
        }

        if (analisePrompt.MemoriaMedia.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Memoria media", string.Join(" | ", analisePrompt.MemoriaMedia.Take(2)), TomConsole.Muted);
        }

        if (analisePrompt.MemoriaLonga.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Memoria longa", string.Join(" | ", analisePrompt.MemoriaLonga.Take(4)), TomConsole.Muted);
        }

        if (analisePrompt.ContextosFerramenta.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Contexto de ferramentas", string.Join(" | ", analisePrompt.ContextosFerramenta.Take(4)), TomConsole.Info);
        }

        if (analisePrompt.SignosRelevantes.Count > 0)
        {
            var resumoSignos = string.Join(
                " | ",
                analisePrompt.SignosRelevantes.Select(x => $"{x.Origem}->{x.Destino} [{x.Tipo}:{x.Forca}]"));
            RenderizadorConsole.ChaveValor("Signos", resumoSignos, TomConsole.Muted);
        }

        if (analisePrompt.CorrecoesSugeridas.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Correcao", string.Join(" | ", analisePrompt.CorrecoesSugeridas), TomConsole.Aviso);
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.MotivoReescrita))
        {
            RenderizadorConsole.ChaveValor("Reescrita", analisePrompt.MotivoReescrita, TomConsole.Muted);
        }

        if (!string.IsNullOrWhiteSpace(analisePrompt.PerguntaCuriosaSugerida))
        {
            RenderizadorConsole.ChaveValor("Curiosidade", analisePrompt.PerguntaCuriosaSugerida, TomConsole.Info);
        }

        RenderizadorConsole.ChaveValor("Apoio no corpus", analisePrompt.ApoioNoCorpus.ToString("0.00"));
        RenderizadorConsole.LinhaEmBranco();
    }

    private static string FormatarBytes(long bytes)
    {
        string[] unidades = ["B", "KB", "MB", "GB"];
        double valor = bytes;
        var indice = 0;

        while (valor >= 1024 && indice < unidades.Length - 1)
        {
            valor /= 1024;
            indice++;
        }

        return $"{valor:0.00} {unidades[indice]}";
    }

    private void ExibirCabecalhoEtapa(string titulo, int indiceEtapa)
    {
        ExecutorPassosConsole.Cabecalho(titulo);
        RenderizadorConsole.LinhaEmBranco();

        _monitorExecucao?.AtualizarProgressoPipeline(indiceEtapa, TotalEtapasPipeline);
    }

    private void IniciarMonitoramentoTempoReal()
    {
        if (_monitorExecucao is not null)
        {
            return;
        }

        _monitorExecucao = new MonitorExecucaoTempoReal(_gravadorArtefatos);
        _monitorExecucao.AtualizarEtapa("inicializacao");
        _monitorExecucao.AtualizarProgressoPipeline(0, TotalEtapasPipeline);
        _monitorExecucao.AtualizarMedicoesEtapas(_medicoes);

        Console.CancelKeyPress += (_, _) => FinalizarMonitoramentoTempoReal("encerrado pelo usuario");
        AppDomain.CurrentDomain.ProcessExit += (_, _) => FinalizarMonitoramentoTempoReal("processo encerrado");
    }

    private void FinalizarMonitoramentoTempoReal(string motivo)
    {
        if (_monitoramentoFinalizado)
        {
            return;
        }

        _monitoramentoFinalizado = true;
        _monitorExecucao?.AtualizarResultadoTreino(_resultadoTreino);
        _monitorExecucao?.AtualizarMedicoesEtapas(_medicoes);
        _monitorExecucao?.Encerrar(motivo);
    }

    private void RegistrarMedicao(MedicaoEtapa medicao)
    {
        _medicoes.Add(medicao);
        _monitorExecucao?.AtualizarMedicoesEtapas(_medicoes);
    }

    private void ExibirProgressoTreino(AtualizacaoProgressoTreino atualizacao)
    {
        const int larguraBarra = 28;
        var preenchidos = Math.Clamp((int)Math.Round(atualizacao.PercentualConclusao * larguraBarra), 0, larguraBarra);
        var barra = $"[{new string('#', preenchidos)}{new string('-', larguraBarra - preenchidos)}]";
        var etaTexto =
            atualizacao.PassosConcluidos >= 50 && atualizacao.TempoDecorrido.TotalSeconds >= 10
                ? FormatarDuracaoLonga(atualizacao.TempoRestanteEstimado)
                : "calculando...";
        var linha = $"{barra} {atualizacao.PercentualConclusao * 100:0.0}% | epoca {atualizacao.EpocaAtual}/{atualizacao.TotalEpocas} | ex {atualizacao.ExemploAtualNaEpoca}/{atualizacao.TotalExemplosPorEpoca} | perda {atualizacao.PerdaMediaAtual:0.000000} | tempo {FormatarDuracaoLonga(atualizacao.TempoDecorrido)} | {atualizacao.ExemplosPorSegundo:0.0} ex/s | ETA {etaTexto}";

        _larguraLinhaProgressoTreino = Math.Max(_larguraLinhaProgressoTreino, linha.Length);
        RenderizadorConsole.LinhaProgresso(linha.PadRight(_larguraLinhaProgressoTreino), TomConsole.Info);
    }

    private void FinalizarLinhaProgressoTreino()
    {
        if (_larguraLinhaProgressoTreino <= 0)
        {
            return;
        }

        RenderizadorConsole.LinhaEmBranco();
        _larguraLinhaProgressoTreino = 0;
    }

    private void ExibirProgressoEtapaInterna(string rotulo, int concluido, int total)
    {
        const int larguraBarra = 28;
        var percentual = total <= 0 ? 0.0 : concluido / (double)total;
        var preenchidos = Math.Clamp((int)Math.Round(percentual * larguraBarra), 0, larguraBarra);
        var barra = $"[{new string('#', preenchidos)}{new string('-', larguraBarra - preenchidos)}]";
        var linha = $"{barra} {percentual * 100:0.0}% | {rotulo} | {concluido}/{total}";

        _larguraLinhaProgressoEtapaInterna = Math.Max(_larguraLinhaProgressoEtapaInterna, linha.Length);
        RenderizadorConsole.LinhaProgresso(linha.PadRight(_larguraLinhaProgressoEtapaInterna), TomConsole.Muted);
    }

    private void FinalizarLinhaProgressoEtapaInterna()
    {
        if (_larguraLinhaProgressoEtapaInterna <= 0)
        {
            return;
        }

        RenderizadorConsole.LinhaEmBranco();
        _larguraLinhaProgressoEtapaInterna = 0;
    }

    private static string FormatarDuracao(TimeSpan duracao)
    {
        var totalHoras = (int)duracao.TotalHours;
        return $"{totalHoras:00}:{duracao.Minutes:00}:{duracao.Seconds:00}";
    }

    private static string FormatarDuracaoLonga(TimeSpan duracao)
    {
        if (duracao.TotalHours < 100)
        {
            return FormatarDuracao(duracao);
        }

        var totalDias = (int)duracao.TotalDays;
        var horas = duracao.Hours;
        var minutos = duracao.Minutes;
        var segundos = duracao.Seconds;
        return $"{totalDias}d {horas:00}:{minutos:00}:{segundos:00}";
    }

    private static string ObterDescricaoProcessador()
    {
        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
               ?? $"{RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}";
    }

    private GestorMemoriaConversacional ObterMemoriaConversacional()
    {
        if (_memoriaConversacional is not null)
        {
            return _memoriaConversacional;
        }

        DateTimeOffset? criadoEmBrasilSp = null;
        var caminhoCerebro = Path.Combine(_gravadorArtefatos.Raiz, "10_cerebro_treinado.json");
        if (File.Exists(caminhoCerebro))
        {
            try
            {
                criadoEmBrasilSp = PersistenciaCerebroTreinado.Carregar(caminhoCerebro).CriadoEmBrasilSp;
            }
            catch
            {
                criadoEmBrasilSp = null;
            }
        }

        var identidadePadrao = ExtratorIdentidadeCorpus.Extrair(_corpus);
        _memoriaConversacional = GestorMemoriaConversacional.CarregarOuCriar(
            _gravadorArtefatos.Raiz,
            criadoEmBrasilSp,
            identidadePadrao);
        return _memoriaConversacional;
    }

    private MotorFerramentasContextuais ObterMotorFerramentas()
    {
        if (_motorFerramentas is not null)
        {
            return _motorFerramentas;
        }

        _motorFerramentas = new MotorFerramentasContextuais(
            Path.GetDirectoryName(_gravadorArtefatos.Raiz) ?? AppContext.BaseDirectory,
            _gravadorArtefatos.Raiz,
            _corpus,
            ObterMemoriaConversacional().Estado);
        return _motorFerramentas;
    }

    private CortexConversacional? ObterCortexConversacional()
    {
        if (!_configuracao.Cognicao.Ativar)
        {
            return null;
        }

        _cortexConversacional ??= CortexConversacional.CarregarOuCriar(
            _gravadorArtefatos.Raiz,
            _configuracao.Cognicao);
        return _cortexConversacional;
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
}
