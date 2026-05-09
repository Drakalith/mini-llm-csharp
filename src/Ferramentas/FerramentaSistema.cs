using System.Diagnostics;
using System.Globalization;
using MiniLlmDidatico.Diagnostico;

namespace MiniLlmDidatico.Ferramentas;

public sealed class BlocoContextoFerramenta
{
    public string Origem { get; init; } = string.Empty;
    public string Rotulo { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
}

public sealed class ContextoFerramentasConversacionais
{
    public List<BlocoContextoFerramenta> Blocos { get; init; } = new();
    public List<string> Evidencias { get; init; } = new();
}

public sealed class EstadoSistemaConversacional
{
    public DateTimeOffset AgoraBrasilSp { get; init; }
    public string DataTexto { get; init; } = string.Empty;
    public string DataExtensoTexto { get; init; } = string.Empty;
    public string DiaSemanaTexto { get; init; } = string.Empty;
    public string HoraTexto { get; init; } = string.Empty;
    public string FusoHorario { get; init; } = RelogioBrasil.NomeFuso;
    public string MemoriaProcessoTexto { get; init; } = string.Empty;
    public string WorkingSetTexto { get; init; } = string.Empty;
    public string MemoriaTotalDisponivelTexto { get; init; } = string.Empty;
    public string NomeMaquina { get; init; } = string.Empty;
    public string SistemaOperacional { get; init; } = string.Empty;
}

public sealed class FerramentaSistema
{
    public EstadoSistemaConversacional CapturarEstadoAtual()
    {
        var agora = RelogioBrasil.AgoraBrasilSp();
        var cultura = new CultureInfo("pt-BR");
        var processo = Process.GetCurrentProcess();
        var memoriaGc = GC.GetGCMemoryInfo();

        return new EstadoSistemaConversacional
        {
            AgoraBrasilSp = agora,
            DataTexto = agora.ToString("yyyy-MM-dd"),
            DataExtensoTexto = agora.ToString("dd 'de' MMMM 'de' yyyy", cultura),
            DiaSemanaTexto = agora.ToString("dddd", cultura),
            HoraTexto = agora.ToString("HH:mm:ss zzz"),
            FusoHorario = RelogioBrasil.NomeFuso,
            MemoriaProcessoTexto = FormatarBytes(processo.PrivateMemorySize64),
            WorkingSetTexto = FormatarBytes(processo.WorkingSet64),
            MemoriaTotalDisponivelTexto = memoriaGc.TotalAvailableMemoryBytes > 0
                ? FormatarBytes(memoriaGc.TotalAvailableMemoryBytes)
                : "indisponível",
            NomeMaquina = Environment.MachineName,
            SistemaOperacional = System.Runtime.InteropServices.RuntimeInformation.OSDescription
        };
    }

    public IReadOnlyList<BlocoContextoFerramenta> CriarBlocosTemporalESistema(
        string entradaNormalizada,
        IReadOnlyCollection<string> formasAtivas)
    {
        var blocos = new List<BlocoContextoFerramenta>();
        if (!EhConsultaTemporal(entradaNormalizada, formasAtivas) &&
            !EhConsultaDeSistema(entradaNormalizada, formasAtivas))
        {
            return blocos;
        }

        var estado = CapturarEstadoAtual();

        if (EhConsultaTemporal(entradaNormalizada, formasAtivas))
        {
            blocos.Add(new BlocoContextoFerramenta
            {
                Origem = "sistema",
                Rotulo = "tempo atual",
                Texto = $"fato atual confirmado pelo sistema : o dia de hoje é {estado.DiaSemanaTexto} . a data atual é {estado.DataTexto} . a data por extenso é {estado.DataExtensoTexto} . a hora atual é {estado.HoraTexto} . o fuso horario ativo é {estado.FusoHorario} ."
            });
        }

        if (EhConsultaDeSistema(entradaNormalizada, formasAtivas))
        {
            blocos.Add(new BlocoContextoFerramenta
            {
                Origem = "sistema",
                Rotulo = "estado do computador",
                Texto = $"fato atual confirmado pelo sistema : a maquina atual é {estado.NomeMaquina} . o sistema operacional é {estado.SistemaOperacional} . a memoria privada do processo é {estado.MemoriaProcessoTexto} . o working set é {estado.WorkingSetTexto} . a memoria total disponivel aproximada é {estado.MemoriaTotalDisponivelTexto} ."
            });
        }

        return blocos;
    }

    private static bool EhConsultaTemporal(string entradaNormalizada, IReadOnlyCollection<string> formasAtivas)
    {
        if (entradaNormalizada.Contains("que horas", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("que dia", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("qual dia", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("qual o dia", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("qual a data", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("qual data", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("data de hoje", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("dia de hoje", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("hoje é que dia", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("hoje e que dia", StringComparison.Ordinal))
        {
            return true;
        }

        var parecePerguntaTemporal =
            entradaNormalizada.Contains("?", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("qual", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("quando", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("informe", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("me diga", StringComparison.Ordinal);

        if (!parecePerguntaTemporal)
        {
            return false;
        }

        return formasAtivas.Any(forma =>
            forma is "hora" or "horas" or "data" or "hoje" or "agora" or "mes" or "ano");
    }

    private static bool EhConsultaDeSistema(string entradaNormalizada, IReadOnlyCollection<string> formasAtivas)
    {
        if (entradaNormalizada.Contains("computador", StringComparison.Ordinal) ||
            entradaNormalizada.Contains("sistema", StringComparison.Ordinal))
        {
            return true;
        }

        return formasAtivas.Any(forma =>
            forma is "cpu" or "gpu" or "ram" or "vram" or "memoria" or "processador" or "pc" or "maquina");
    }

    private static string FormatarBytes(long bytes)
    {
        string[] unidades = ["B", "KB", "MB", "GB", "TB"];
        double valor = bytes;
        var indice = 0;

        while (valor >= 1024 && indice < unidades.Length - 1)
        {
            valor /= 1024;
            indice++;
        }

        return $"{valor:0.00} {unidades[indice]}";
    }
}
