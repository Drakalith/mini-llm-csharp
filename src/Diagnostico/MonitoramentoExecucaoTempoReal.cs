using System.Diagnostics;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Treinamento;

namespace MiniLlmDidatico.Diagnostico;

public sealed class EstadoProgressoTreinoTempoReal
{
    public int EpocaAtual { get; init; }
    public int TotalEpocas { get; init; }
    public double PercentualConclusao { get; init; }
    public double PerdaMediaAtual { get; init; }
    public double EpocasPorSegundo { get; init; }
    public double ExemplosPorSegundo { get; init; }
    public double SegundosDecorridos { get; init; }
    public double SegundosRestantesEstimados { get; init; }
}

public sealed class AmostraMonitoramentoExecucao
{
    public DateTimeOffset MomentoBrasilSp { get; init; }
    public string EtapaAtual { get; init; } = string.Empty;
    public int EtapaAtualIndice { get; init; }
    public int TotalEtapasPipeline { get; init; }
    public double PercentualPipeline { get; init; }
    public string DispositivoAtual { get; init; } = string.Empty;
    public double CpuProcessoPercentual { get; init; }
    public long MemoriaGerenciadaBytes { get; init; }
    public long WorkingSetBytes { get; init; }
    public long MemoriaPrivadaBytes { get; init; }
    public long? VramUsadaBytes { get; init; }
    public long? VramTotalBytes { get; init; }
    public double? UtilizacaoGpuPercentual { get; init; }
    public EstadoProgressoTreinoTempoReal? Treino { get; init; }
}

public sealed class ResumoMonitoramentoExecucao
{
    public int QuantidadeAmostras { get; init; }
    public double CpuMedioPercentual { get; init; }
    public double CpuPicoPercentual { get; init; }
    public long PicoMemoriaGerenciadaBytes { get; init; }
    public long PicoWorkingSetBytes { get; init; }
    public long PicoMemoriaPrivadaBytes { get; init; }
    public long? PicoVramUsadaBytes { get; init; }
    public long? VramTotalBytes { get; init; }
    public double? UtilizacaoGpuMediaPercentual { get; init; }
    public double? UtilizacaoGpuPicoPercentual { get; init; }
}

public sealed class DescritorMetricaMonitoramento
{
    public string Nome { get; init; } = string.Empty;
    public string OQueRepresenta { get; init; } = string.Empty;
    public string ComoEMedido { get; init; } = string.Empty;
    public string LeituraPratica { get; init; } = string.Empty;
}

public sealed class RelatorioMonitoramentoExecucao
{
    public string FusoHorarioReferencia { get; init; } = string.Empty;
    public DateTimeOffset IniciadoEmBrasilSp { get; init; }
    public DateTimeOffset AtualizadoEmBrasilSp { get; init; }
    public DateTimeOffset? EncerradoEmBrasilSp { get; init; }
    public string MotivoEncerramento { get; init; } = string.Empty;
    public string EtapaAtual { get; init; } = string.Empty;
    public int EtapaAtualIndice { get; init; }
    public int TotalEtapasPipeline { get; init; }
    public double PercentualPipeline { get; init; }
    public string DispositivoAtual { get; init; } = string.Empty;
    public RelatorioAdaptacaoHardware? Hardware { get; init; }
    public List<MedicaoEtapa> MedicoesEtapas { get; init; } = new();
    public ResultadoTreino? ResultadoTreino { get; init; }
    public ResumoMonitoramentoExecucao Resumo { get; init; } = new();
    public List<AmostraMonitoramentoExecucao> Amostras { get; init; } = new();
    public List<DescritorMetricaMonitoramento> MetricasExplicadas { get; init; } = new();
    public List<string> Recomendacoes { get; init; } = new();
}

internal readonly record struct InstantaneoProcessoTempoReal(
    DateTimeOffset MomentoBrasilSp,
    TimeSpan TempoCpu,
    long MemoriaGerenciadaBytes,
    long WorkingSetBytes,
    long MemoriaPrivadaBytes);

internal readonly record struct InstantaneoGpuTempoReal(
    long? VramUsadaBytes,
    long? VramTotalBytes,
    double? UtilizacaoGpuPercentual);

public sealed class MonitorExecucaoTempoReal : IDisposable
{
    private const string NomeArquivo = "11_monitoramento_execucao.json";

    private readonly GravadorArtefatos _gravadorArtefatos;
    private readonly object _trava = new();
    private readonly List<AmostraMonitoramentoExecucao> _amostras = new();
    private readonly Timer _timer;
    private readonly DateTimeOffset _iniciadoEmBrasilSp;

    private InstantaneoProcessoTempoReal? _ultimoInstantaneoProcesso;
    private EstadoProgressoTreinoTempoReal? _progressoTreinoAtual;
    private RelatorioAdaptacaoHardware? _hardware;
    private IReadOnlyList<MedicaoEtapa> _medicoesEtapas = Array.Empty<MedicaoEtapa>();
    private ResultadoTreino? _resultadoTreino;
    private string _etapaAtual = "inicializacao";
    private int _etapaAtualIndice;
    private int _totalEtapasPipeline;
    private double _percentualPipeline;
    private string _dispositivoAtual = "indefinido";
    private string _motivoEncerramento = "monitoramento em andamento";
    private DateTimeOffset? _encerradoEmBrasilSp;
    private bool _encerrado;

    public MonitorExecucaoTempoReal(GravadorArtefatos gravadorArtefatos)
    {
        _gravadorArtefatos = gravadorArtefatos;
        _iniciadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
        _timer = new Timer(_ => Amostrar(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        PersistirMarco();
    }

    public void AtualizarHardware(RelatorioAdaptacaoHardware hardware)
    {
        lock (_trava)
        {
            _hardware = hardware;
        }

        PersistirMarco();
    }

    public void AtualizarEtapa(string etapa, string? dispositivoAtual = null)
    {
        lock (_trava)
        {
            _etapaAtual = etapa;
            if (!string.IsNullOrWhiteSpace(dispositivoAtual))
            {
                _dispositivoAtual = dispositivoAtual;
            }
        }
    }

    public void AtualizarProgressoPipeline(int etapaAtualIndice, int totalEtapasPipeline)
    {
        lock (_trava)
        {
            _etapaAtualIndice = etapaAtualIndice;
            _totalEtapasPipeline = totalEtapasPipeline;
            _percentualPipeline = totalEtapasPipeline <= 0
                ? 0.0
                : etapaAtualIndice / (double)totalEtapasPipeline;
        }

        PersistirMarco();
    }

    public void AtualizarProgressoTreino(EstadoProgressoTreinoTempoReal progresso)
    {
        lock (_trava)
        {
            _progressoTreinoAtual = progresso;
        }
    }

    public void AtualizarResultadoTreino(ResultadoTreino? resultadoTreino)
    {
        lock (_trava)
        {
            _resultadoTreino = resultadoTreino;
        }

        PersistirMarco();
    }

    public void AtualizarMedicoesEtapas(IReadOnlyList<MedicaoEtapa> medicoesEtapas)
    {
        lock (_trava)
        {
            _medicoesEtapas = medicoesEtapas.ToList();
        }

        PersistirMarco();
    }

    public void Encerrar(string motivoEncerramento)
    {
        lock (_trava)
        {
            if (_encerrado)
            {
                return;
            }

            _encerrado = true;
            _motivoEncerramento = motivoEncerramento;
            _encerradoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
        }

        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        Amostrar();
        PersistirRelatorio();
    }

    public void Dispose()
    {
        Encerrar("monitor descartado");
        _timer.Dispose();
    }

    private void Amostrar()
    {
        lock (_trava)
        {
            AdicionarAmostraAtual();
        }
    }

    private void PersistirMarco()
    {
        lock (_trava)
        {
            AdicionarAmostraAtual();
            PersistirRelatorio();
        }
    }

    private void AdicionarAmostraAtual()
    {
        var instantaneo = CapturarInstantaneoProcesso();
        var gpu = CapturarInstantaneoGpu();
        var cpuPercentual = CalcularCpuPercentual(instantaneo);

        _amostras.Add(new AmostraMonitoramentoExecucao
        {
            MomentoBrasilSp = instantaneo.MomentoBrasilSp,
            EtapaAtual = _etapaAtual,
            EtapaAtualIndice = _etapaAtualIndice,
            TotalEtapasPipeline = _totalEtapasPipeline,
            PercentualPipeline = _percentualPipeline,
            DispositivoAtual = _dispositivoAtual,
            CpuProcessoPercentual = cpuPercentual,
            MemoriaGerenciadaBytes = instantaneo.MemoriaGerenciadaBytes,
            WorkingSetBytes = instantaneo.WorkingSetBytes,
            MemoriaPrivadaBytes = instantaneo.MemoriaPrivadaBytes,
            VramUsadaBytes = gpu.VramUsadaBytes,
            VramTotalBytes = gpu.VramTotalBytes,
            UtilizacaoGpuPercentual = gpu.UtilizacaoGpuPercentual,
            Treino = _progressoTreinoAtual
        });
    }

    private void PersistirRelatorio()
    {
        var relatorio = ConstruirRelatorio();
        _gravadorArtefatos.GravarJson(NomeArquivo, relatorio);
    }

    private RelatorioMonitoramentoExecucao ConstruirRelatorio()
    {
        var amostras = _amostras.ToList();
        var resumo = ConstruirResumo(amostras);

        return new RelatorioMonitoramentoExecucao
        {
            FusoHorarioReferencia = RelogioBrasil.NomeFuso,
            IniciadoEmBrasilSp = _iniciadoEmBrasilSp,
            AtualizadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp(),
            EncerradoEmBrasilSp = _encerradoEmBrasilSp,
            MotivoEncerramento = _motivoEncerramento,
            EtapaAtual = _etapaAtual,
            EtapaAtualIndice = _etapaAtualIndice,
            TotalEtapasPipeline = _totalEtapasPipeline,
            PercentualPipeline = _percentualPipeline,
            DispositivoAtual = _dispositivoAtual,
            Hardware = _hardware,
            MedicoesEtapas = _medicoesEtapas.ToList(),
            ResultadoTreino = _resultadoTreino,
            Resumo = resumo,
            Amostras = amostras,
            MetricasExplicadas = CriarDescricoesMetricas(),
            Recomendacoes = GerarRecomendacoes(resumo, _hardware, _resultadoTreino)
        };
    }

    private static ResumoMonitoramentoExecucao ConstruirResumo(IReadOnlyList<AmostraMonitoramentoExecucao> amostras)
    {
        if (amostras.Count == 0)
        {
            return new ResumoMonitoramentoExecucao();
        }

        var amostrasComGpu = amostras.Where(x => x.UtilizacaoGpuPercentual.HasValue).ToList();
        var picoVram = amostras.Where(x => x.VramUsadaBytes.HasValue).Select(x => x.VramUsadaBytes!.Value).DefaultIfEmpty().Max();
        var vramTotal = amostras.Where(x => x.VramTotalBytes.HasValue).Select(x => x.VramTotalBytes!.Value).DefaultIfEmpty().Max();

        return new ResumoMonitoramentoExecucao
        {
            QuantidadeAmostras = amostras.Count,
            CpuMedioPercentual = amostras.Average(x => x.CpuProcessoPercentual),
            CpuPicoPercentual = amostras.Max(x => x.CpuProcessoPercentual),
            PicoMemoriaGerenciadaBytes = amostras.Max(x => x.MemoriaGerenciadaBytes),
            PicoWorkingSetBytes = amostras.Max(x => x.WorkingSetBytes),
            PicoMemoriaPrivadaBytes = amostras.Max(x => x.MemoriaPrivadaBytes),
            PicoVramUsadaBytes = picoVram > 0 ? picoVram : null,
            VramTotalBytes = vramTotal > 0 ? vramTotal : null,
            UtilizacaoGpuMediaPercentual = amostrasComGpu.Count > 0 ? amostrasComGpu.Average(x => x.UtilizacaoGpuPercentual!.Value) : null,
            UtilizacaoGpuPicoPercentual = amostrasComGpu.Count > 0 ? amostrasComGpu.Max(x => x.UtilizacaoGpuPercentual!.Value) : null
        };
    }

    private static List<DescritorMetricaMonitoramento> CriarDescricoesMetricas()
    {
        return new List<DescritorMetricaMonitoramento>
        {
            new()
            {
                Nome = "PercentualPipeline",
                OQueRepresenta = "Quanto do fluxo geral da execucao ja entrou em andamento.",
                ComoEMedido = "Indice da etapa atual dividido pelo total de etapas visiveis do pipeline.",
                LeituraPratica = "Ajuda a saber em que ponto macro o sistema esta, mesmo quando uma etapa demora bastante."
            },
            new()
            {
                Nome = "CpuProcessoPercentual",
                OQueRepresenta = "Quanto do processador o processo atual esta consumindo.",
                ComoEMedido = "Diferenca de TotalProcessorTime entre amostras, dividida pelo tempo de parede e pelos nucleos logicos visiveis.",
                LeituraPratica = "Quanto mais perto de 100%, mais o processo esta pressionando a CPU disponivel."
            },
            new()
            {
                Nome = "WorkingSetBytes",
                OQueRepresenta = "Memoria RAM fisica que o processo esta ocupando no momento.",
                ComoEMedido = "Leitura de Process.WorkingSet64 a cada amostragem.",
                LeituraPratica = "Se sobe demais perto da RAM fisica total, o sistema pode comecar a degradar."
            },
            new()
            {
                Nome = "MemoriaPrivadaBytes",
                OQueRepresenta = "Memoria comprometida so deste processo.",
                ComoEMedido = "Leitura de Process.PrivateMemorySize64.",
                LeituraPratica = "Ajuda a ver quanto o programa realmente reservou para si."
            },
            new()
            {
                Nome = "MemoriaGerenciadaBytes",
                OQueRepresenta = "Heap gerenciado do .NET em uso.",
                ComoEMedido = "Leitura de GC.GetTotalMemory(false).",
                LeituraPratica = "Mostra a pressao de objetos gerenciados e ajuda a identificar excesso de alocacao."
            },
            new()
            {
                Nome = "VramUsadaBytes",
                OQueRepresenta = "Memoria de video usada pela GPU principal detectada.",
                ComoEMedido = "Consulta periodica via nvidia-smi, quando disponivel.",
                LeituraPratica = "Se chega muito perto do total, o backend GPU pode ficar apertado ou falhar."
            },
            new()
            {
                Nome = "UtilizacaoGpuPercentual",
                OQueRepresenta = "Nivel de ocupacao da GPU durante a execucao.",
                ComoEMedido = "Campo utilization.gpu retornado pelo nvidia-smi.",
                LeituraPratica = "Se a GPU esta baixa e a CPU esta alta, pode haver gargalo fora da GPU."
            },
            new()
            {
                Nome = "Treino.PercentualConclusao",
                OQueRepresenta = "Quanto do treino total ja foi concluido.",
                ComoEMedido = "EpocaAtual dividida por TotalEpocas.",
                LeituraPratica = "Serve como barra objetiva de progresso do treinamento."
            },
            new()
            {
                Nome = "Treino.EpocasPorSegundo",
                OQueRepresenta = "Velocidade media do treino.",
                ComoEMedido = "EpocaAtual dividida pelos segundos decorridos desde o inicio do treino.",
                LeituraPratica = "Quanto maior, mais rapido o treinamento esta andando."
            }
        };
    }

    private static List<string> GerarRecomendacoes(
        ResumoMonitoramentoExecucao resumo,
        RelatorioAdaptacaoHardware? hardware,
        ResultadoTreino? resultadoTreino)
    {
        var recomendacoes = new List<string>();
        var memoriaFisica = hardware?.Hardware.MemoriaFisicaBytes ?? 0;

        if (memoriaFisica > 0 && resumo.PicoWorkingSetBytes > memoriaFisica * 0.75)
        {
            recomendacoes.Add("RAM alta: considere diminuir maximoComprimentoSequencia, dimensaoEmbedding, dimensaoFeedForward ou numeroCamadas.");
        }

        if (resumo.VramTotalBytes.HasValue &&
            resumo.PicoVramUsadaBytes.HasValue &&
            resumo.PicoVramUsadaBytes.Value > resumo.VramTotalBytes.Value * 0.80)
        {
            recomendacoes.Add("VRAM alta: considere diminuir dimensaoEmbedding, dimensaoFeedForward, numeroCamadas ou mover parte da carga para CPU.");
        }

        if (resumo.CpuMedioPercentual > 80.0 &&
            hardware?.DecisaoTreino.BackendGpuDisponivelNoBuild == true &&
            (resumo.UtilizacaoGpuMediaPercentual ?? 0.0) < 25.0)
        {
            recomendacoes.Add("CPU muito pressionada e GPU pouco usada: vale testar treino=gpu ou inferencia=gpu no appsettings.json.");
        }

        if (resultadoTreino?.PerdasEpoca.Count >= 20)
        {
            var ultimas = resultadoTreino.PerdasEpoca.TakeLast(20).ToList();
            var perdaMinima = resultadoTreino.PerdasEpoca.Min(x => x.PerdaMedia);
            var perdaFinal = ultimas.Last().PerdaMedia;

            if (perdaFinal > perdaMinima * 1.03)
            {
                recomendacoes.Add("A perda final subiu em relacao ao melhor ponto: talvez reduzir taxaAprendizado ou parar mais cedo.");
            }
            else if (Math.Abs(ultimas.First().PerdaMedia - perdaFinal) < 0.005)
            {
                recomendacoes.Add("A perda ficou quase parada no final: talvez aumentar capacidade do modelo ou variar melhor o corpus.");
            }
        }

        if (recomendacoes.Count == 0)
        {
            recomendacoes.Add("A execucao nao mostrou gargalo forte de RAM, VRAM ou CPU; o proximo ajuste pode ser guiado mais pela perda e pela qualidade das respostas.");
        }

        return recomendacoes;
    }

    private InstantaneoProcessoTempoReal CapturarInstantaneoProcesso()
    {
        using var processo = Process.GetCurrentProcess();
        processo.Refresh();

        return new InstantaneoProcessoTempoReal(
            RelogioBrasil.AgoraBrasilSp(),
            processo.TotalProcessorTime,
            GC.GetTotalMemory(false),
            processo.WorkingSet64,
            processo.PrivateMemorySize64);
    }

    private double CalcularCpuPercentual(InstantaneoProcessoTempoReal atual)
    {
        var cpuPercentual = 0.0;

        if (_ultimoInstantaneoProcesso is { } anterior)
        {
            var deltaParedeMs = (atual.MomentoBrasilSp - anterior.MomentoBrasilSp).TotalMilliseconds;
            var deltaCpuMs = (atual.TempoCpu - anterior.TempoCpu).TotalMilliseconds;

            if (deltaParedeMs > 0.0)
            {
                cpuPercentual = deltaCpuMs / (deltaParedeMs * Environment.ProcessorCount) * 100.0;
            }
        }

        _ultimoInstantaneoProcesso = atual;
        return System.Math.Max(0.0, cpuPercentual);
    }

    private static InstantaneoGpuTempoReal CapturarInstantaneoGpu()
    {
        try
        {
            var inicio = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.used,memory.total,utilization.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(inicio);
            if (processo is null)
            {
                return default;
            }

            if (!processo.WaitForExit(1500))
            {
                try
                {
                    processo.Kill(true);
                }
                catch
                {
                }

                return default;
            }

            var saida = processo.StandardOutput.ReadToEnd().Trim();
            if (processo.ExitCode != 0 || string.IsNullOrWhiteSpace(saida))
            {
                return default;
            }

            var primeiraLinha = saida
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(primeiraLinha))
            {
                return default;
            }

            var partes = primeiraLinha.Split(',', StringSplitOptions.TrimEntries);
            if (partes.Length < 3)
            {
                return default;
            }

            var memoriaUsadaMb = long.TryParse(partes[0], out var usada) ? usada : 0;
            var memoriaTotalMb = long.TryParse(partes[1], out var total) ? total : 0;
            var usoGpuPercentual = double.TryParse(partes[2], out var usoGpu) ? usoGpu : 0.0;

            return new InstantaneoGpuTempoReal(
                memoriaUsadaMb > 0 ? memoriaUsadaMb * 1024L * 1024L : null,
                memoriaTotalMb > 0 ? memoriaTotalMb * 1024L * 1024L : null,
                usoGpuPercentual > 0 ? usoGpuPercentual : null);
        }
        catch
        {
            return default;
        }
    }
}
