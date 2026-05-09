using System.Diagnostics;
using System.Runtime.InteropServices;
using MiniLlmDidatico.Numerica;

namespace MiniLlmDidatico.Diagnostico;

public sealed class RetratoProcessador
{
    public string Nome { get; init; } = string.Empty;
    public int NumeroNucleosFisicos { get; init; }
    public int NumeroNucleosLogicos { get; init; }
    public int ClockMaximoMhz { get; init; }
}

public sealed class RetratoGpu
{
    public string Nome { get; init; } = string.Empty;
    public long MemoriaBytes { get; init; }
    public string VersaoDriver { get; init; } = string.Empty;
}

public sealed class RetratoHardwareSistema
{
    public string SistemaOperacional { get; init; } = string.Empty;
    public string ArquiteturaProcesso { get; init; } = string.Empty;
    public RetratoProcessador Processador { get; init; } = new();
    public List<RetratoGpu> Gpus { get; init; } = new();
    public long MemoriaFisicaBytes { get; init; }
}

public sealed class DecisaoExecucaoHardware
{
    public string TipoCarga { get; init; } = string.Empty;
    public string ModoSelecao { get; init; } = "auto";
    public string ComponenteMaisAdequadoPeloHardware { get; init; } = string.Empty;
    public string ComponenteUsadoAgora { get; init; } = string.Empty;
    public bool BackendGpuDisponivelNoBuild { get; init; }
    public double PontuacaoCpu { get; init; }
    public double PontuacaoGpu { get; init; }
    public double? TempoBenchmarkCpuMs { get; init; }
    public double? TempoBenchmarkGpuMs { get; init; }
    public string ExplicacaoDesempenho { get; init; } = string.Empty;
    public string LimiarEstimadoGpu { get; init; } = string.Empty;
    public List<ComparacaoEscalaBackendNumerico> ComparacoesEscala { get; init; } = new();
    public string Motivo { get; init; } = string.Empty;
}

public sealed class RelatorioAdaptacaoHardware
{
    public RetratoHardwareSistema Hardware { get; init; } = new();
    public DecisaoExecucaoHardware DecisaoTreino { get; init; } = new();
    public DecisaoExecucaoHardware DecisaoInferencia { get; init; } = new();
}

public static class AnalisadorAdaptacaoHardware
{
    public static RelatorioAdaptacaoHardware Analisar(
        PerfilCargaNumerica? perfilCarga = null,
        string? preferenciaTreino = null,
        string? preferenciaInferencia = null)
    {
        var hardware = DetectarHardware();
        var melhorGpu = hardware.Gpus.OrderByDescending(x => x.MemoriaBytes).FirstOrDefault();
        var sondaCuda = SondaGpuCuda.Analisar();

        var pontuacaoCpu = CalcularPontuacaoCpu(hardware.Processador);
        var pontuacaoGpu = CalcularPontuacaoGpu(melhorGpu);
        var gpuExiste = melhorGpu is not null;
        var backendGpuDisponivelNoBuild = sondaCuda.Disponivel;

        var componenteMaisAdequadoPeloHardware = gpuExiste && pontuacaoGpu > pontuacaoCpu * 1.15 ? "gpu" : "cpu";

        var perfilTreino = perfilCarga is null
            ? null
            : new PerfilCargaNumerica
            {
                DimensaoEmbedding = perfilCarga.DimensaoEmbedding,
                DimensaoFeedForward = perfilCarga.DimensaoFeedForward,
                IteracoesBenchmark = perfilCarga.IteracoesBenchmark,
                TipoCarga = "treino"
            };
        var perfilInferencia = perfilCarga is null
            ? null
            : new PerfilCargaNumerica
            {
                DimensaoEmbedding = System.Math.Max(8, perfilCarga.DimensaoEmbedding / 2),
                DimensaoFeedForward = System.Math.Max(16, perfilCarga.DimensaoFeedForward / 2),
                IteracoesBenchmark = System.Math.Max(4, perfilCarga.IteracoesBenchmark / 2),
                TipoCarga = "inferencia"
            };

        var decisaoTreino = CriarDecisao(
            "treino",
            perfilTreino,
            melhorGpu,
            gpuExiste,
            pontuacaoCpu,
            pontuacaoGpu,
            componenteMaisAdequadoPeloHardware,
            backendGpuDisponivelNoBuild,
            sondaCuda.Motivo,
            preferenciaTreino);

        var decisaoInferencia = CriarDecisao(
            "inferencia",
            perfilInferencia,
            melhorGpu,
            gpuExiste,
            pontuacaoCpu,
            pontuacaoGpu,
            componenteMaisAdequadoPeloHardware,
            backendGpuDisponivelNoBuild,
            sondaCuda.Motivo,
            preferenciaInferencia);

        return new RelatorioAdaptacaoHardware
        {
            Hardware = hardware,
            DecisaoTreino = decisaoTreino,
            DecisaoInferencia = decisaoInferencia
        };
    }

    private static DecisaoExecucaoHardware CriarDecisao(
        string tipoCarga,
        PerfilCargaNumerica? perfilCarga,
        RetratoGpu? melhorGpu,
        bool gpuExiste,
        double pontuacaoCpu,
        double pontuacaoGpu,
        string componenteMaisAdequadoPeloHardware,
        bool backendGpuDisponivelNoBuild,
        string motivoSondaGpu,
        string? preferenciaManual)
    {
        var modoSelecao = NormalizarPreferenciaExecucao(preferenciaManual);
        RelatorioBenchmarkBackendsNumericos? benchmark = null;
        if (backendGpuDisponivelNoBuild && perfilCarga is not null)
        {
            try
            {
                benchmark = BenchmarkBackendsNumericos.Comparar(perfilCarga);
            }
            catch (Exception ex)
            {
                benchmark = new RelatorioBenchmarkBackendsNumericos
                {
                    Executado = false,
                    TipoCarga = tipoCarga,
                    BackendMaisRapido = "cpu",
                    Motivo = $"benchmark dos backends falhou: {ex.Message}",
                    ExplicacaoDesempenho = $"o benchmark falhou antes de estimar a virada CPU/GPU: {ex.Message}",
                    LimiarEstimadoGpu = "nao foi possivel estimar"
                };
            }
        }

        var componenteEscolhidoAutomaticamente = benchmark is not null && benchmark.Executado
            ? benchmark.BackendMaisRapido
            : componenteMaisAdequadoPeloHardware == "gpu" && backendGpuDisponivelNoBuild
                ? "gpu"
                : "cpu";

        var componenteUsadoAgora = modoSelecao switch
        {
            "cpu" => "cpu",
            "gpu" when gpuExiste && backendGpuDisponivelNoBuild => "gpu",
            "gpu" => "cpu",
            _ => componenteEscolhidoAutomaticamente
        };

        var recomendacaoAutomatica = benchmark is not null && benchmark.Executado
            ? $"recomendacao automatica para {tipoCarga}: cpu={benchmark.TempoCpuMs:0.00} ms gpu={benchmark.TempoGpuMs:0.00} ms; melhor={componenteEscolhidoAutomaticamente}"
            : componenteMaisAdequadoPeloHardware switch
            {
                "gpu" when backendGpuDisponivelNoBuild => $"recomendacao automatica para {tipoCarga}: GPU parece mais adequada pelo hardware",
                "gpu" => $"recomendacao automatica para {tipoCarga}: GPU parece melhor pelo hardware, mas o backend CUDA nao ficou utilizavel ({motivoSondaGpu})",
                _ when gpuExiste => $"recomendacao automatica para {tipoCarga}: CPU parece mais adequada que a GPU {melhorGpu!.Nome}",
                _ => $"recomendacao automatica para {tipoCarga}: nenhuma GPU detectada; usar CPU"
            };

        var motivo = modoSelecao switch
        {
            "cpu" => $"modo manual configurado para CPU; {recomendacaoAutomatica}",
            "gpu" when gpuExiste && backendGpuDisponivelNoBuild => $"modo manual configurado para GPU; {recomendacaoAutomatica}",
            "gpu" when !gpuExiste => $"modo manual configurado para GPU, mas nenhuma GPU foi detectada; fallback para CPU. {recomendacaoAutomatica}",
            "gpu" => $"modo manual configurado para GPU, mas o backend GPU nao ficou utilizavel ({motivoSondaGpu}); fallback para CPU. {recomendacaoAutomatica}",
            _ => recomendacaoAutomatica
        };

        return new DecisaoExecucaoHardware
        {
            TipoCarga = tipoCarga,
            ModoSelecao = modoSelecao,
            ComponenteMaisAdequadoPeloHardware = componenteMaisAdequadoPeloHardware,
            ComponenteUsadoAgora = componenteUsadoAgora,
            BackendGpuDisponivelNoBuild = backendGpuDisponivelNoBuild,
            PontuacaoCpu = pontuacaoCpu,
            PontuacaoGpu = pontuacaoGpu,
            TempoBenchmarkCpuMs = benchmark?.Executado == true ? benchmark.TempoCpuMs : null,
            TempoBenchmarkGpuMs = benchmark?.Executado == true ? benchmark.TempoGpuMs : null,
            ExplicacaoDesempenho = benchmark?.ExplicacaoDesempenho ?? string.Empty,
            LimiarEstimadoGpu = benchmark?.LimiarEstimadoGpu ?? string.Empty,
            ComparacoesEscala = benchmark?.ComparacoesEscala ?? new List<ComparacaoEscalaBackendNumerico>(),
            Motivo = motivo
        };
    }

    private static string NormalizarPreferenciaExecucao(string? preferencia)
    {
        if (string.IsNullOrWhiteSpace(preferencia))
        {
            return "auto";
        }

        var normalizada = preferencia.Trim().ToLowerInvariant();
        return normalizada is "cpu" or "gpu" ? normalizada : "auto";
    }

    private static RetratoHardwareSistema DetectarHardware()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && TentarConsultarHardwareWindows(out var hardwareWindows))
        {
            return hardwareWindows;
        }

        return new RetratoHardwareSistema
        {
            SistemaOperacional = RuntimeInformation.OSDescription,
            ArquiteturaProcesso = RuntimeInformation.ProcessArchitecture.ToString(),
            Processador = new RetratoProcessador
            {
                Nome = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU nao identificada",
                NumeroNucleosFisicos = 0,
                NumeroNucleosLogicos = Environment.ProcessorCount,
                ClockMaximoMhz = 0
            },
            MemoriaFisicaBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
        };
    }

    private static bool TentarConsultarHardwareWindows(out RetratoHardwareSistema hardware)
    {
        var processador = TentarDetectarCpuWindows();
        var gpus = TentarDetectarGpuWindows();
        var memoriaFisicaBytes = TentarDetectarMemoriaFisicaWindows();

        if (string.IsNullOrWhiteSpace(processador.Nome) && gpus.Count == 0 && memoriaFisicaBytes <= 0)
        {
            hardware = new RetratoHardwareSistema();
            return false;
        }

        hardware = new RetratoHardwareSistema
        {
            SistemaOperacional = RuntimeInformation.OSDescription,
            ArquiteturaProcesso = RuntimeInformation.ProcessArchitecture.ToString(),
            Processador = string.IsNullOrWhiteSpace(processador.Nome)
                ? new RetratoProcessador
                {
                    Nome = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU nao identificada",
                    NumeroNucleosFisicos = 0,
                    NumeroNucleosLogicos = Environment.ProcessorCount,
                    ClockMaximoMhz = 0
                }
                : processador,
            Gpus = gpus,
            MemoriaFisicaBytes = memoriaFisicaBytes > 0
                ? memoriaFisicaBytes
                : GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
        };

        return true;
    }

    private static RetratoProcessador TentarDetectarCpuWindows()
    {
        var saida = ExecutarPowerShell(
            "Get-CimInstance Win32_Processor | Select-Object -First 1 | ForEach-Object { \"$($_.Name)|$($_.NumberOfCores)|$($_.NumberOfLogicalProcessors)|$($_.MaxClockSpeed)\" }");

        if (string.IsNullOrWhiteSpace(saida))
        {
            return new RetratoProcessador();
        }

        var partes = saida.Split('|');
        return new RetratoProcessador
        {
            Nome = partes.ElementAtOrDefault(0)?.Trim() ?? string.Empty,
            NumeroNucleosFisicos = TentarConverterInt(partes.ElementAtOrDefault(1)),
            NumeroNucleosLogicos = TentarConverterInt(partes.ElementAtOrDefault(2)),
            ClockMaximoMhz = TentarConverterInt(partes.ElementAtOrDefault(3))
        };
    }

    private static List<RetratoGpu> TentarDetectarGpuWindows()
    {
        var gpus = TentarDetectarGpuViaNvidiaSmi();
        if (gpus.Count > 0)
        {
            return gpus;
        }

        var saida = ExecutarPowerShell(
            "Get-CimInstance Win32_VideoController | ForEach-Object { \"$($_.Name)|$($_.AdapterRAM)|$($_.DriverVersion)\" }");

        if (string.IsNullOrWhiteSpace(saida))
        {
            return new List<RetratoGpu>();
        }

        return saida
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(linha => linha.Split('|'))
            .Where(partes => partes.Length >= 3)
            .Select(partes => new RetratoGpu
            {
                Nome = partes[0].Trim(),
                MemoriaBytes = TentarConverterLong(partes[1]),
                VersaoDriver = partes[2].Trim()
            })
            .Where(gpu => !string.IsNullOrWhiteSpace(gpu.Nome))
            .ToList();
    }

    private static List<RetratoGpu> TentarDetectarGpuViaNvidiaSmi()
    {
        try
        {
            var inicio = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(inicio);
            if (processo is null)
            {
                return new List<RetratoGpu>();
            }

            if (!processo.WaitForExit(4000))
            {
                processo.Kill(true);
                return new List<RetratoGpu>();
            }

            var saida = processo.StandardOutput.ReadToEnd().Trim();
            if (processo.ExitCode != 0 || string.IsNullOrWhiteSpace(saida))
            {
                return new List<RetratoGpu>();
            }

            return saida
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(linha => linha.Split(','))
                .Where(partes => partes.Length >= 3)
                .Select(partes => new RetratoGpu
                {
                    Nome = partes[0].Trim(),
                    MemoriaBytes = TentarConverterLong(partes[1]) * 1024 * 1024,
                    VersaoDriver = partes[2].Trim()
                })
                .ToList();
        }
        catch
        {
            return new List<RetratoGpu>();
        }
    }

    private static long TentarDetectarMemoriaFisicaWindows()
    {
        var saida = ExecutarPowerShell("Get-CimInstance Win32_ComputerSystem | Select-Object -ExpandProperty TotalPhysicalMemory");
        return TentarConverterLong(saida);
    }

    private static string ExecutarPowerShell(string script)
    {
        try
        {
            var inicio = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var processo = Process.Start(inicio);
            if (processo is null)
            {
                return string.Empty;
            }

            processo.StandardInput.WriteLine(script);
            processo.StandardInput.Close();

            if (!processo.WaitForExit(5000))
            {
                processo.Kill(true);
                return string.Empty;
            }

            var saida = processo.StandardOutput.ReadToEnd().Trim();
            return processo.ExitCode == 0 ? saida : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int TentarConverterInt(string? texto)
    {
        return int.TryParse(texto?.Trim(), out var valor) ? valor : 0;
    }

    private static long TentarConverterLong(string? texto)
    {
        return long.TryParse(texto?.Trim(), out var valor) ? valor : 0L;
    }

    private static double CalcularPontuacaoCpu(RetratoProcessador processador)
    {
        var clockGhz = processador.ClockMaximoMhz > 0 ? processador.ClockMaximoMhz / 1000.0 : 1.0;
        var nucleos = processador.NumeroNucleosLogicos > 0 ? processador.NumeroNucleosLogicos : 1;
        return nucleos * clockGhz;
    }

    private static double CalcularPontuacaoGpu(RetratoGpu? gpu)
    {
        if (gpu is null || gpu.MemoriaBytes <= 0)
        {
            return 0.0;
        }

        var memoriaGb = gpu.MemoriaBytes / 1024d / 1024d / 1024d;
        return memoriaGb * 10.0;
    }
}
