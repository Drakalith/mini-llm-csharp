using System.Diagnostics;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Modelo;

namespace MiniLlmDidatico.Treinamento;

public sealed class PlanoTreinoAdaptativo
{
    public bool PlanejamentoAutomaticoAtivo { get; init; }
    public bool PlanoAdaptado { get; init; }
    public int TotalExemplosDisponiveis { get; init; }
    public int ExemplosPorEpocaSolicitados { get; init; }
    public int ExemplosPorEpocaEfetivos { get; init; }
    public int EpocasSolicitadas { get; init; }
    public int EpocasEfetivas { get; init; }
    public long PassosSolicitados { get; init; }
    public long PassosEfetivos { get; init; }
    public int MaximoPassosTreinoEfetivo { get; init; }
    public int PassosAmostraBenchmark { get; init; }
    public double ExemplosPorSegundoEstimados { get; init; }
    public double SegundosEstimadosPlanoSolicitado { get; init; }
    public double SegundosEstimadosPlanoEfetivo { get; init; }
    public double SegundosOrcamento { get; init; }
    public string DispositivoPlanejado { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public static class PlanejadorTreinoAdaptativo
{
    public static PlanoTreinoAdaptativo Planejar(
        MiniTransformador modelo,
        IReadOnlyList<ExemploTreino> exemplos,
        ConfiguracaoAplicacao configuracao)
    {
        var totalExemplosDisponiveis = exemplos.Count;
        var exemplosPorEpocaSolicitados = totalExemplosDisponiveis == 0
            ? 0
            : configuracao.Modelo.MaximoExemplosPorEpoca > 0
                ? Math.Min(configuracao.Modelo.MaximoExemplosPorEpoca, totalExemplosDisponiveis)
                : totalExemplosDisponiveis;

        var passosSolicitados = configuracao.Modelo.MaximoPassosTreino > 0
            ? Math.Min((long)configuracao.Modelo.Epocas * exemplosPorEpocaSolicitados, configuracao.Modelo.MaximoPassosTreino)
            : (long)configuracao.Modelo.Epocas * exemplosPorEpocaSolicitados;

        if (totalExemplosDisponiveis == 0 || exemplosPorEpocaSolicitados == 0 || passosSolicitados == 0)
        {
            return new PlanoTreinoAdaptativo
            {
                PlanejamentoAutomaticoAtivo = configuracao.Modelo.TreinoAdaptativoAutomatico,
                PlanoAdaptado = false,
                TotalExemplosDisponiveis = totalExemplosDisponiveis,
                ExemplosPorEpocaSolicitados = exemplosPorEpocaSolicitados,
                ExemplosPorEpocaEfetivos = 0,
                EpocasSolicitadas = configuracao.Modelo.Epocas,
                EpocasEfetivas = 0,
                PassosSolicitados = passosSolicitados,
                PassosEfetivos = 0,
                MaximoPassosTreinoEfetivo = 0,
                PassosAmostraBenchmark = 0,
                ExemplosPorSegundoEstimados = 0.0,
                SegundosEstimadosPlanoSolicitado = 0.0,
                SegundosEstimadosPlanoEfetivo = 0.0,
                SegundosOrcamento = configuracao.Modelo.TempoMaximoTreinoMinutos * 60.0,
                DispositivoPlanejado = modelo.ContextoBackend.DispositivoUsado,
                Motivo = "nenhum exemplo de treino disponivel"
            };
        }

        var passosAmostra = Math.Clamp(configuracao.Modelo.PassosAmostraPlanejamento, 4, 64);
        passosAmostra = Math.Min(passosAmostra, exemplosPorEpocaSolicitados);
        var exemplosPorSegundoEstimados = MedirVelocidadeTreino(modelo, exemplos, passosAmostra);
        var segundosOrcamento = Math.Max(60.0, configuracao.Modelo.TempoMaximoTreinoMinutos * 60.0);
        var passosOrcados = exemplosPorSegundoEstimados > 0.0
            ? Math.Max(1L, (long)Math.Floor(exemplosPorSegundoEstimados * segundosOrcamento * 0.85))
            : passosSolicitados;

        if (!configuracao.Modelo.TreinoAdaptativoAutomatico || passosSolicitados <= passosOrcados)
        {
            var segundosPlanoSolicitado = exemplosPorSegundoEstimados > 0.0
                ? passosSolicitados / exemplosPorSegundoEstimados
                : 0.0;

            return new PlanoTreinoAdaptativo
            {
                PlanejamentoAutomaticoAtivo = configuracao.Modelo.TreinoAdaptativoAutomatico,
                PlanoAdaptado = false,
                TotalExemplosDisponiveis = totalExemplosDisponiveis,
                ExemplosPorEpocaSolicitados = exemplosPorEpocaSolicitados,
                ExemplosPorEpocaEfetivos = exemplosPorEpocaSolicitados,
                EpocasSolicitadas = configuracao.Modelo.Epocas,
                EpocasEfetivas = configuracao.Modelo.Epocas,
                PassosSolicitados = passosSolicitados,
                PassosEfetivos = passosSolicitados,
                MaximoPassosTreinoEfetivo = configuracao.Modelo.MaximoPassosTreino > 0
                    ? configuracao.Modelo.MaximoPassosTreino
                    : (int)Math.Min(passosSolicitados, int.MaxValue),
                PassosAmostraBenchmark = passosAmostra,
                ExemplosPorSegundoEstimados = exemplosPorSegundoEstimados,
                SegundosEstimadosPlanoSolicitado = segundosPlanoSolicitado,
                SegundosEstimadosPlanoEfetivo = segundosPlanoSolicitado,
                SegundosOrcamento = segundosOrcamento,
                DispositivoPlanejado = modelo.ContextoBackend.DispositivoUsado,
                Motivo = configuracao.Modelo.TreinoAdaptativoAutomatico
                    ? "o plano solicitado ja cabe no orcamento de tempo estimado para este hardware"
                    : "treino adaptativo automatico desativado; usando o plano solicitado"
            };
        }

        var minimoExemplosPorEpoca = Math.Clamp(
            configuracao.Modelo.MinimoExemplosPorEpocaAdaptado,
            1,
            exemplosPorEpocaSolicitados);

        int epocasEfetivas;
        int exemplosPorEpocaEfetivos;

        if (passosOrcados / Math.Max(configuracao.Modelo.Epocas, 1) >= minimoExemplosPorEpoca)
        {
            epocasEfetivas = configuracao.Modelo.Epocas;
            exemplosPorEpocaEfetivos = (int)Math.Max(1L, Math.Min(exemplosPorEpocaSolicitados, passosOrcados / epocasEfetivas));
        }
        else
        {
            exemplosPorEpocaEfetivos = minimoExemplosPorEpoca;
            epocasEfetivas = (int)Math.Max(1L, Math.Min(configuracao.Modelo.Epocas, passosOrcados / exemplosPorEpocaEfetivos));

            if (epocasEfetivas == 1)
            {
                exemplosPorEpocaEfetivos = (int)Math.Max(1L, Math.Min(exemplosPorEpocaSolicitados, passosOrcados));
            }
        }

        var passosEfetivos = Math.Max(1L, Math.Min(passosSolicitados, (long)epocasEfetivas * exemplosPorEpocaEfetivos));
        var segundosPlanoBruto = passosSolicitados / exemplosPorSegundoEstimados;
        var segundosPlanoEfetivo = passosEfetivos / exemplosPorSegundoEstimados;

        return new PlanoTreinoAdaptativo
        {
            PlanejamentoAutomaticoAtivo = true,
            PlanoAdaptado = true,
            TotalExemplosDisponiveis = totalExemplosDisponiveis,
            ExemplosPorEpocaSolicitados = exemplosPorEpocaSolicitados,
            ExemplosPorEpocaEfetivos = exemplosPorEpocaEfetivos,
            EpocasSolicitadas = configuracao.Modelo.Epocas,
            EpocasEfetivas = epocasEfetivas,
            PassosSolicitados = passosSolicitados,
            PassosEfetivos = passosEfetivos,
            MaximoPassosTreinoEfetivo = (int)Math.Min(passosEfetivos, int.MaxValue),
            PassosAmostraBenchmark = passosAmostra,
            ExemplosPorSegundoEstimados = exemplosPorSegundoEstimados,
            SegundosEstimadosPlanoSolicitado = segundosPlanoBruto,
            SegundosEstimadosPlanoEfetivo = segundosPlanoEfetivo,
            SegundosOrcamento = segundosOrcamento,
            DispositivoPlanejado = modelo.ContextoBackend.DispositivoUsado,
            Motivo = $"plano bruto excede o orcamento de tempo deste hardware; reduzindo para {epocasEfetivas} epocas com {exemplosPorEpocaEfetivos} exemplos por epoca"
        };
    }

    private static double MedirVelocidadeTreino(
        MiniTransformador modelo,
        IReadOnlyList<ExemploTreino> exemplos,
        int passosAmostra)
    {
        var estadoBase = modelo.ExportarEstado();
        var modeloSombra = new MiniTransformador(modelo.HiperParametros, modelo.ExecutorNumerico, modelo.ContextoBackend);
        modeloSombra.AplicarEstado(estadoBase);

        var indices = SelecionarIndicesAmostra(exemplos.Count, passosAmostra);
        var cronometro = Stopwatch.StartNew();

        foreach (var indice in indices)
        {
            var exemplo = exemplos[indice];
            modeloSombra.TreinarPasso(exemplo.IdsEntrada, exemplo.IdsAlvo, exemplo.PesosPerda);
        }

        cronometro.Stop();

        return indices.Length / Math.Max(cronometro.Elapsed.TotalSeconds, 0.0001);
    }

    private static int[] SelecionarIndicesAmostra(int totalExemplos, int passosAmostra)
    {
        if (totalExemplos <= 0 || passosAmostra <= 0)
        {
            return Array.Empty<int>();
        }

        if (passosAmostra >= totalExemplos)
        {
            return Enumerable.Range(0, totalExemplos).ToArray();
        }

        var gerador = new Random(123);
        var usados = new HashSet<int>();
        var indices = new int[passosAmostra];
        var posicao = 0;

        while (posicao < indices.Length)
        {
            var indice = gerador.Next(totalExemplos);
            if (usados.Add(indice))
            {
                indices[posicao++] = indice;
            }
        }

        return indices;
    }
}
