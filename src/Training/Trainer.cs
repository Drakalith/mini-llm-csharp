using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Diagnostico;

namespace MiniLlmDidatico.Treinamento;

public sealed class AtualizacaoProgressoTreino
{
    public int EpocaAtual { get; init; }
    public int TotalEpocas { get; init; }
    public int ExemploAtualNaEpoca { get; init; }
    public int TotalExemplosPorEpoca { get; init; }
    public long PassosConcluidos { get; init; }
    public long TotalPassos { get; init; }
    public double PerdaMediaAtual { get; init; }
    public double PercentualConclusao { get; init; }
    public TimeSpan TempoDecorrido { get; init; }
    public TimeSpan TempoRestanteEstimado { get; init; }
    public double EpocasPorSegundo { get; init; }
    public double ExemplosPorSegundo { get; init; }
}

public sealed class Treinador
{
    private readonly MiniTransformador _modelo;

    public Treinador(MiniTransformador modelo)
    {
        _modelo = modelo;
    }

    public ResultadoTreino Treinar(
        IReadOnlyList<ExemploTreino> exemplos,
        int epocas,
        int maximoExemplosPorEpoca = 0,
        int maximoPassosTreino = 0,
        bool embaralharExemplosPorEpoca = true,
        int pacienciaParadaAntecipada = 0,
        double melhoraMinimaPerda = 0.0005,
        int sementeAleatoria = 42,
        Action<AtualizacaoProgressoTreino>? aoAtualizarProgresso = null)
    {
        var iniciadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var cronometroUltimaAtualizacao = System.Diagnostics.Stopwatch.StartNew();
        var perdas = new List<PerdaEpoca>();
        var geradorAleatorio = new Random(sementeAleatoria);
        var totalExemplosDisponiveis = exemplos.Count;
        var exemplosUsadosPorEpoca = totalExemplosDisponiveis == 0
            ? 0
            : (maximoExemplosPorEpoca > 0
                ? System.Math.Min(maximoExemplosPorEpoca, totalExemplosDisponiveis)
                : totalExemplosDisponiveis);
        var totalPassosPlanejados = exemplosUsadosPorEpoca > 0
            ? (long)epocas * exemplosUsadosPorEpoca
            : 0L;

        if (maximoPassosTreino > 0)
        {
            totalPassosPlanejados = totalPassosPlanejados > 0
                ? System.Math.Min(totalPassosPlanejados, maximoPassosTreino)
                : maximoPassosTreino;
        }

        var totalPassos = System.Math.Max(totalPassosPlanejados, 1);
        var passosConcluidos = 0;
        var melhorPerda = double.PositiveInfinity;
        var epocasSemMelhora = 0;
        var epocasExecutadas = 0;
        var paradaAntecipada = false;
        var motivoParada = "treino concluido como planejado";

        aoAtualizarProgresso?.Invoke(new AtualizacaoProgressoTreino
        {
            EpocaAtual = 1,
            TotalEpocas = epocas,
            ExemploAtualNaEpoca = 0,
            TotalExemplosPorEpoca = exemplosUsadosPorEpoca,
            PassosConcluidos = 0,
            TotalPassos = totalPassos,
            PerdaMediaAtual = 0.0,
            PercentualConclusao = 0.0,
            TempoDecorrido = TimeSpan.Zero,
            TempoRestanteEstimado = TimeSpan.Zero,
            EpocasPorSegundo = 0.0,
            ExemplosPorSegundo = 0.0
        });

        for (var epoca = 1; epoca <= epocas; epoca++)
        {
            if (maximoPassosTreino > 0 && passosConcluidos >= maximoPassosTreino)
            {
                paradaAntecipada = true;
                motivoParada = $"limite total de passos atingido ({maximoPassosTreino})";
                break;
            }

            var indicesEpoca = SelecionarIndicesEpoca(
                exemplos,
                exemplosUsadosPorEpoca,
                embaralharExemplosPorEpoca,
                geradorAleatorio);

            if (indicesEpoca.Length == 0)
            {
                paradaAntecipada = true;
                motivoParada = "nenhum exemplo de treino ficou disponivel para esta epoca";
                break;
            }

            var perdaTotal = 0.0;
            var exemplosProcessadosNaEpoca = 0;

            for (var indiceExemplo = 0; indiceExemplo < indicesEpoca.Length; indiceExemplo++)
            {
                if (maximoPassosTreino > 0 && passosConcluidos >= maximoPassosTreino)
                {
                    paradaAntecipada = true;
                    motivoParada = $"limite total de passos atingido ({maximoPassosTreino})";
                    break;
                }

                var exemplo = exemplos[indicesEpoca[indiceExemplo]];
                var resultado = _modelo.TreinarPasso(exemplo.IdsEntrada, exemplo.IdsAlvo, exemplo.PesosPerda);
                perdaTotal += resultado.Perda;
                passosConcluidos++;
                exemplosProcessadosNaEpoca++;

                var deveAtualizarProgressoParcial =
                    cronometroUltimaAtualizacao.ElapsedMilliseconds >= 250 ||
                    indiceExemplo == indicesEpoca.Length - 1;

                if (deveAtualizarProgressoParcial)
                {
                    var segundosDecorridosParcial = System.Math.Max(cronometro.Elapsed.TotalSeconds, 0.0001);
                    var epocasConcluidasParciais = (epoca - 1) + ((indiceExemplo + 1) / (double)indicesEpoca.Length);
                    var epocasPorSegundoParcial = epocasConcluidasParciais / segundosDecorridosParcial;
                    var exemplosPorSegundoParcial = passosConcluidos / segundosDecorridosParcial;
                    var percentualConclusaoParcial = passosConcluidos / (double)totalPassos;
                    var segundosRestantesParcial = exemplosPorSegundoParcial > 0.0
                        ? (totalPassos - passosConcluidos) / exemplosPorSegundoParcial
                        : 0.0;

                    aoAtualizarProgresso?.Invoke(new AtualizacaoProgressoTreino
                    {
                        EpocaAtual = epoca,
                        TotalEpocas = epocas,
                        ExemploAtualNaEpoca = indiceExemplo + 1,
                        TotalExemplosPorEpoca = indicesEpoca.Length,
                        PassosConcluidos = passosConcluidos,
                        TotalPassos = totalPassos,
                        PerdaMediaAtual = perdaTotal / (indiceExemplo + 1),
                        PercentualConclusao = percentualConclusaoParcial,
                        TempoDecorrido = cronometro.Elapsed,
                        TempoRestanteEstimado = TimeSpan.FromSeconds(segundosRestantesParcial),
                        EpocasPorSegundo = epocasPorSegundoParcial,
                        ExemplosPorSegundo = exemplosPorSegundoParcial
                    });

                    cronometroUltimaAtualizacao.Restart();
                }
            }

            if (passosConcluidos == 0)
            {
                paradaAntecipada = true;
                motivoParada = "treino encerrado sem executar nenhum passo";
                break;
            }

            epocasExecutadas = epoca;
            var perdaMedia = perdaTotal / System.Math.Max(exemplosProcessadosNaEpoca, 1);
            var segundosDecorridos = System.Math.Max(cronometro.Elapsed.TotalSeconds, 0.0001);
            var epocasPorSegundo = epoca / segundosDecorridos;
            var exemplosPorSegundo = passosConcluidos / segundosDecorridos;
            var percentualConclusao = passosConcluidos / (double)totalPassos;
            var segundosRestantesEstimados = exemplosPorSegundo > 0.0
                ? (totalPassos - passosConcluidos) / exemplosPorSegundo
                : 0.0;

            perdas.Add(new PerdaEpoca
            {
                Epoca = epoca,
                PerdaMedia = perdaMedia,
                MomentoBrasilSp = RelogioBrasil.AgoraBrasilSp(),
                SegundosDecorridos = cronometro.Elapsed.TotalSeconds,
                EpocasPorSegundoMedio = epocasPorSegundo,
                ExemplosPorSegundoMedio = exemplosPorSegundo
            });

            aoAtualizarProgresso?.Invoke(new AtualizacaoProgressoTreino
            {
                EpocaAtual = epoca,
                TotalEpocas = epocas,
                ExemploAtualNaEpoca = indicesEpoca.Length,
                TotalExemplosPorEpoca = indicesEpoca.Length,
                PassosConcluidos = passosConcluidos,
                TotalPassos = totalPassos,
                PerdaMediaAtual = perdaMedia,
                PercentualConclusao = percentualConclusao,
                TempoDecorrido = cronometro.Elapsed,
                TempoRestanteEstimado = TimeSpan.FromSeconds(segundosRestantesEstimados),
                EpocasPorSegundo = epocasPorSegundo,
                ExemplosPorSegundo = exemplosPorSegundo
            });

            if (perdaMedia < melhorPerda - melhoraMinimaPerda)
            {
                melhorPerda = perdaMedia;
                epocasSemMelhora = 0;
            }
            else if (pacienciaParadaAntecipada > 0)
            {
                epocasSemMelhora++;
                if (epocasSemMelhora >= pacienciaParadaAntecipada)
                {
                    paradaAntecipada = true;
                    motivoParada = $"parada antecipada por falta de melhora relevante em {pacienciaParadaAntecipada} epocas";
                    break;
                }
            }

            if (paradaAntecipada)
            {
                break;
            }
        }

        cronometro.Stop();

        return new ResultadoTreino
        {
            IniciadoEmBrasilSp = iniciadoEmBrasilSp,
            ConcluidoEmBrasilSp = RelogioBrasil.AgoraBrasilSp(),
            DuracaoTotalSegundos = cronometro.Elapsed.TotalSeconds,
            EpocasPlanejadas = epocas,
            EpocasExecutadas = epocasExecutadas,
            TotalExemplosDisponiveis = totalExemplosDisponiveis,
            ExemplosUsadosPorEpoca = exemplosUsadosPorEpoca,
            PassosPlanejados = totalPassosPlanejados,
            PassosExecutados = passosConcluidos,
            ParadaAntecipada = paradaAntecipada,
            MotivoParada = motivoParada,
            EpocasPorSegundoMedio = epocasExecutadas / System.Math.Max(cronometro.Elapsed.TotalSeconds, 0.0001),
            ExemplosPorSegundoMedio = passosConcluidos / System.Math.Max(cronometro.Elapsed.TotalSeconds, 0.0001),
            PerdasEpoca = perdas
        };
    }

    private static int[] SelecionarIndicesEpoca(
        IReadOnlyList<ExemploTreino> exemplos,
        int exemplosUsadosPorEpoca,
        bool embaralharExemplosPorEpoca,
        Random geradorAleatorio)
    {
        var totalExemplosDisponiveis = exemplos.Count;
        if (totalExemplosDisponiveis <= 0 || exemplosUsadosPorEpoca <= 0)
        {
            return Array.Empty<int>();
        }

        if (exemplosUsadosPorEpoca >= totalExemplosDisponiveis)
        {
            var indicesCompletos = Enumerable.Range(0, totalExemplosDisponiveis).ToArray();

            if (embaralharExemplosPorEpoca)
            {
                Embaralhar(indicesCompletos, geradorAleatorio);
            }

            return indicesCompletos;
        }

        if (!embaralharExemplosPorEpoca)
        {
            return Enumerable.Range(0, exemplosUsadosPorEpoca).ToArray();
        }

        var gruposPorDominio = exemplos
            .Select((exemplo, indice) => new { exemplo.Dominio, Indice = indice })
            .GroupBy(x => x.Dominio, StringComparer.Ordinal)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.Select(y => y.Indice).ToList())
            .ToList();

        foreach (var grupo in gruposPorDominio)
        {
            Embaralhar(grupo, geradorAleatorio);
        }

        var indicesSelecionados = new List<int>(exemplosUsadosPorEpoca);
        var espacosRestantes = exemplosUsadosPorEpoca;

        foreach (var grupo in gruposPorDominio)
        {
            if (espacosRestantes <= 0)
            {
                break;
            }

            if (grupo.Count == 0)
            {
                continue;
            }

            indicesSelecionados.Add(grupo[0]);
            espacosRestantes--;
        }

        if (espacosRestantes > 0)
        {
            var restantes = gruposPorDominio
                .SelectMany(grupo => grupo.Skip(1))
                .Where(indice => !indicesSelecionados.Contains(indice))
                .ToList();

            Embaralhar(restantes, geradorAleatorio);

            foreach (var indice in restantes)
            {
                if (espacosRestantes <= 0)
                {
                    break;
                }

                indicesSelecionados.Add(indice);
                espacosRestantes--;
            }
        }

        if (indicesSelecionados.Count < exemplosUsadosPorEpoca)
        {
            var faltantes = Enumerable.Range(0, totalExemplosDisponiveis)
                .Where(indice => !indicesSelecionados.Contains(indice))
                .ToList();
            Embaralhar(faltantes, geradorAleatorio);

            foreach (var indice in faltantes)
            {
                if (indicesSelecionados.Count >= exemplosUsadosPorEpoca)
                {
                    break;
                }

                indicesSelecionados.Add(indice);
            }
        }

        var indices = indicesSelecionados.Take(exemplosUsadosPorEpoca).ToArray();
        Embaralhar(indices, geradorAleatorio);
        return indices;
    }

    private static void Embaralhar(int[] valores, Random geradorAleatorio)
    {
        for (var i = valores.Length - 1; i > 0; i--)
        {
            var j = geradorAleatorio.Next(i + 1);
            (valores[i], valores[j]) = (valores[j], valores[i]);
        }
    }

    private static void Embaralhar(List<int> valores, Random geradorAleatorio)
    {
        for (var i = valores.Count - 1; i > 0; i--)
        {
            var j = geradorAleatorio.Next(i + 1);
            (valores[i], valores[j]) = (valores[j], valores[i]);
        }
    }
}

public sealed class ResultadoTreino
{
    public DateTimeOffset IniciadoEmBrasilSp { get; set; }
    public DateTimeOffset ConcluidoEmBrasilSp { get; set; }
    public double DuracaoTotalSegundos { get; set; }
    public int EpocasPlanejadas { get; set; }
    public int EpocasExecutadas { get; set; }
    public int TotalExemplosDisponiveis { get; set; }
    public int ExemplosUsadosPorEpoca { get; set; }
    public long PassosPlanejados { get; set; }
    public long PassosExecutados { get; set; }
    public bool ParadaAntecipada { get; set; }
    public string MotivoParada { get; set; } = string.Empty;
    public double EpocasPorSegundoMedio { get; set; }
    public double ExemplosPorSegundoMedio { get; set; }
    public List<PerdaEpoca> PerdasEpoca { get; set; } = new();
}

public sealed class PerdaEpoca
{
    public int Epoca { get; set; }
    public double PerdaMedia { get; set; }
    public DateTimeOffset MomentoBrasilSp { get; set; }
    public double SegundosDecorridos { get; set; }
    public double EpocasPorSegundoMedio { get; set; }
    public double ExemplosPorSegundoMedio { get; set; }
}
