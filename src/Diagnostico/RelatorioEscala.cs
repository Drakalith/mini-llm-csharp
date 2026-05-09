using System.Diagnostics;

namespace MiniLlmDidatico.Diagnostico;

public sealed class ComponenteParametroModelo
{
    public required string Nome { get; init; }
    public required string Tipo { get; init; }
    public required string Forma { get; init; }
    public long QuantidadePesos { get; init; }
    public long QuantidadeVieses { get; init; }
    public long QuantidadeParametros { get; init; }
    public long MemoriaBytes { get; init; }
}

public sealed class CamadaAtivaModelo
{
    public required string Nome { get; init; }
    public required string Tipo { get; init; }
    public required string Forma { get; init; }
    public long QuantidadeNeuronios { get; init; }
}

public sealed class RelatorioEscalaModelo
{
    public int ComprimentoSequenciaAtiva { get; init; }
    public long TotalNeuroniosAtivos { get; init; }
    public long TotalPesosTreinaveis { get; init; }
    public long TotalVieses { get; init; }
    public long TotalParametrosTreinaveis { get; init; }
    public long MemoriaParametrosBytes { get; init; }
    public List<ComponenteParametroModelo> ComponentesParametros { get; init; } = new();
    public List<CamadaAtivaModelo> CamadasAtivas { get; init; } = new();
}

public sealed class MedicaoEtapa
{
    public required string NomeEtapa { get; init; }
    public DateTimeOffset IniciadoEmBrasilSp { get; init; }
    public DateTimeOffset ConcluidoEmBrasilSp { get; init; }
    public long MilissegundosParede { get; init; }
    public long MilissegundosCpu { get; init; }
    public double PercentualCpuMedio { get; init; }
    public bool CpuMedioConfiavel { get; init; }
    public long MemoriaGerenciadaAntesBytes { get; init; }
    public long MemoriaGerenciadaDepoisBytes { get; init; }
    public long WorkingSetAntesBytes { get; init; }
    public long WorkingSetDepoisBytes { get; init; }
    public long MemoriaPrivadaAntesBytes { get; init; }
    public long MemoriaPrivadaDepoisBytes { get; init; }
}

internal readonly record struct AmostraProcesso(
    long MemoriaGerenciadaBytes,
    long WorkingSetBytes,
    long MemoriaPrivadaBytes,
    TimeSpan TempoCpu);

public static class MedidorRecursos
{
    public static (T Resultado, MedicaoEtapa Medicao) Medir<T>(string nomeEtapa, Func<T> acao)
    {
        var iniciadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
        var antes = CapturarAmostra();
        var cronometro = Stopwatch.StartNew();
        var resultado = acao();
        cronometro.Stop();
        var depois = CapturarAmostra();
        var concluidoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();

        return (resultado, CriarMedicao(nomeEtapa, antes, depois, cronometro.Elapsed, iniciadoEmBrasilSp, concluidoEmBrasilSp));
    }

    public static MedicaoEtapa Medir(string nomeEtapa, Action acao)
    {
        var iniciadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
        var antes = CapturarAmostra();
        var cronometro = Stopwatch.StartNew();
        acao();
        cronometro.Stop();
        var depois = CapturarAmostra();
        var concluidoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();

        return CriarMedicao(nomeEtapa, antes, depois, cronometro.Elapsed, iniciadoEmBrasilSp, concluidoEmBrasilSp);
    }

    private static AmostraProcesso CapturarAmostra()
    {
        using var processo = Process.GetCurrentProcess();
        processo.Refresh();

        return new AmostraProcesso(
            GC.GetTotalMemory(false),
            processo.WorkingSet64,
            processo.PrivateMemorySize64,
            processo.TotalProcessorTime);
    }

    private static MedicaoEtapa CriarMedicao(
        string nomeEtapa,
        AmostraProcesso antes,
        AmostraProcesso depois,
        TimeSpan tempoParede,
        DateTimeOffset iniciadoEmBrasilSp,
        DateTimeOffset concluidoEmBrasilSp)
    {
        var milissegundosParede = System.Math.Max(tempoParede.TotalMilliseconds, 1.0);
        var milissegundosCpu = System.Math.Max(0.0, (depois.TempoCpu - antes.TempoCpu).TotalMilliseconds);
        var cpuMedioConfiavel = tempoParede.TotalMilliseconds >= 50.0;
        var percentualCpuMedio = cpuMedioConfiavel
            ? milissegundosCpu / (milissegundosParede * Environment.ProcessorCount) * 100.0
            : 0.0;

        return new MedicaoEtapa
        {
            NomeEtapa = nomeEtapa,
            IniciadoEmBrasilSp = iniciadoEmBrasilSp,
            ConcluidoEmBrasilSp = concluidoEmBrasilSp,
            MilissegundosParede = (long)tempoParede.TotalMilliseconds,
            MilissegundosCpu = (long)milissegundosCpu,
            PercentualCpuMedio = percentualCpuMedio,
            CpuMedioConfiavel = cpuMedioConfiavel,
            MemoriaGerenciadaAntesBytes = antes.MemoriaGerenciadaBytes,
            MemoriaGerenciadaDepoisBytes = depois.MemoriaGerenciadaBytes,
            WorkingSetAntesBytes = antes.WorkingSetBytes,
            WorkingSetDepoisBytes = depois.WorkingSetBytes,
            MemoriaPrivadaAntesBytes = antes.MemoriaPrivadaBytes,
            MemoriaPrivadaDepoisBytes = depois.MemoriaPrivadaBytes
        };
    }
}
