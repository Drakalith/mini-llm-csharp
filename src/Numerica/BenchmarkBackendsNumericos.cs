using System.Diagnostics;

namespace MiniLlmDidatico.Numerica;

public sealed class PerfilCargaNumerica
{
    public int DimensaoEmbedding { get; init; }
    public int DimensaoFeedForward { get; init; }
    public int IteracoesBenchmark { get; init; } = 12;
    public string TipoCarga { get; init; } = "treino";
}

public sealed class RelatorioBenchmarkBackendsNumericos
{
    public bool Executado { get; init; }
    public int DimensaoEmbedding { get; init; }
    public int DimensaoFeedForward { get; init; }
    public int IteracoesBenchmark { get; init; }
    public double TempoCpuMs { get; init; }
    public double TempoGpuMs { get; init; }
    public string BackendMaisRapido { get; init; } = "cpu";
    public string Motivo { get; init; } = string.Empty;
    public string ExplicacaoDesempenho { get; init; } = string.Empty;
    public string LimiarEstimadoGpu { get; init; } = string.Empty;
    public int? MultiplicadorPrimeiraVitoriaGpu { get; init; }
    public int? DimensaoEmbeddingPrimeiraVitoriaGpu { get; init; }
    public int? DimensaoFeedForwardPrimeiraVitoriaGpu { get; init; }
    public string TipoCarga { get; init; } = "treino";
    public List<ComparacaoEscalaBackendNumerico> ComparacoesEscala { get; init; } = new();
}

public sealed class ComparacaoEscalaBackendNumerico
{
    public int Multiplicador { get; init; }
    public int DimensaoEmbedding { get; init; }
    public int DimensaoFeedForward { get; init; }
    public int IteracoesBenchmark { get; init; }
    public string TipoCarga { get; init; } = "treino";
    public double TempoCpuMs { get; init; }
    public double TempoGpuMs { get; init; }
    public string BackendMaisRapido { get; init; } = "cpu";
}

public static class BenchmarkBackendsNumericos
{
    public static RelatorioBenchmarkBackendsNumericos Comparar(PerfilCargaNumerica perfil)
    {
        var cpu = new ExecutorNumericoCpu();
        using var gpu = new ExecutorNumericoGpuCuda();

        var medicaoAtual = MedirPerfil(cpu, gpu, perfil, 12345);
        var comparacoesEscala = MedirEscalas(cpu, gpu, perfil);
        var primeiraVitoriaGpu = comparacoesEscala.FirstOrDefault(x => x.BackendMaisRapido == "gpu");
        var melhor = medicaoAtual.BackendMaisRapido;
        var explicacao = CriarExplicacao(perfil, medicaoAtual, primeiraVitoriaGpu, comparacoesEscala[^1]);
        var limiar = primeiraVitoriaGpu is null
            ? $"GPU nao ganhou ate {comparacoesEscala[^1].Multiplicador}x (embedding={comparacoesEscala[^1].DimensaoEmbedding}, feed-forward={comparacoesEscala[^1].DimensaoFeedForward})"
            : $"GPU tende a compensar perto de {primeiraVitoriaGpu.Multiplicador}x (embedding={primeiraVitoriaGpu.DimensaoEmbedding}, feed-forward={primeiraVitoriaGpu.DimensaoFeedForward})";

        return new RelatorioBenchmarkBackendsNumericos
        {
            Executado = true,
            DimensaoEmbedding = perfil.DimensaoEmbedding,
            DimensaoFeedForward = perfil.DimensaoFeedForward,
            IteracoesBenchmark = perfil.IteracoesBenchmark,
            TipoCarga = perfil.TipoCarga,
            TempoCpuMs = medicaoAtual.TempoCpuMs,
            TempoGpuMs = medicaoAtual.TempoGpuMs,
            BackendMaisRapido = melhor,
            Motivo = melhor == "gpu"
                ? "o benchmark numerico da carga atual ficou mais rapido em GPU"
                : "o benchmark numerico da carga atual ficou mais rapido em CPU",
            ExplicacaoDesempenho = explicacao,
            LimiarEstimadoGpu = limiar,
            MultiplicadorPrimeiraVitoriaGpu = primeiraVitoriaGpu?.Multiplicador,
            DimensaoEmbeddingPrimeiraVitoriaGpu = primeiraVitoriaGpu?.DimensaoEmbedding,
            DimensaoFeedForwardPrimeiraVitoriaGpu = primeiraVitoriaGpu?.DimensaoFeedForward,
            ComparacoesEscala = comparacoesEscala
        };
    }

    private static ComparacaoEscalaBackendNumerico MedirPerfil(
        IExecutorNumerico cpu,
        IExecutorNumerico gpu,
        PerfilCargaNumerica perfil,
        int semente)
    {
        var gerador = new Random(semente);
        var entrada = CriarVetorAleatorio(perfil.DimensaoEmbedding, gerador);
        var saida = CriarVetorAleatorio(perfil.DimensaoFeedForward, gerador);
        var vies = CriarVetorAleatorio(perfil.DimensaoFeedForward, gerador);
        var pesos = CriarMatrizAleatoria(perfil.DimensaoEmbedding, perfil.DimensaoFeedForward, gerador);
        var destinoCpu = OperacoesMatematicas.Zeros(perfil.DimensaoEmbedding, perfil.DimensaoFeedForward);
        var destinoGpu = OperacoesMatematicas.Zeros(perfil.DimensaoEmbedding, perfil.DimensaoFeedForward);

        Aquecer(cpu, entrada, saida, vies, pesos, destinoCpu);
        Aquecer(gpu, entrada, saida, vies, pesos, destinoGpu);

        var tempoCpu = Medir(cpu, entrada, saida, vies, pesos, destinoCpu, perfil.IteracoesBenchmark);
        var tempoGpu = Medir(gpu, entrada, saida, vies, pesos, destinoGpu, perfil.IteracoesBenchmark);

        return new ComparacaoEscalaBackendNumerico
        {
            Multiplicador = 1,
            DimensaoEmbedding = perfil.DimensaoEmbedding,
            DimensaoFeedForward = perfil.DimensaoFeedForward,
            IteracoesBenchmark = perfil.IteracoesBenchmark,
            TipoCarga = perfil.TipoCarga,
            TempoCpuMs = tempoCpu,
            TempoGpuMs = tempoGpu,
            BackendMaisRapido = tempoGpu < tempoCpu ? "gpu" : "cpu"
        };
    }

    private static List<ComparacaoEscalaBackendNumerico> MedirEscalas(
        IExecutorNumerico cpu,
        IExecutorNumerico gpu,
        PerfilCargaNumerica perfilBase)
    {
        var comparacoes = new List<ComparacaoEscalaBackendNumerico>();
        int[] multiplicadores = [1, 2, 4, 8, 16];

        foreach (var multiplicador in multiplicadores)
        {
            var perfilEscala = new PerfilCargaNumerica
            {
                DimensaoEmbedding = perfilBase.DimensaoEmbedding * multiplicador,
                DimensaoFeedForward = perfilBase.DimensaoFeedForward * multiplicador,
                IteracoesBenchmark = CalcularIteracoesEscala(perfilBase.IteracoesBenchmark, multiplicador)
            };

            var comparacao = MedirPerfil(cpu, gpu, perfilEscala, 12345 + multiplicador);
            comparacao = new ComparacaoEscalaBackendNumerico
            {
                Multiplicador = multiplicador,
                DimensaoEmbedding = comparacao.DimensaoEmbedding,
                DimensaoFeedForward = comparacao.DimensaoFeedForward,
                IteracoesBenchmark = comparacao.IteracoesBenchmark,
                TipoCarga = comparacao.TipoCarga,
                TempoCpuMs = comparacao.TempoCpuMs,
                TempoGpuMs = comparacao.TempoGpuMs,
                BackendMaisRapido = comparacao.BackendMaisRapido
            };
            comparacoes.Add(comparacao);
        }

        return comparacoes;
    }

    private static int CalcularIteracoesEscala(int iteracoesBase, int multiplicador)
    {
        return System.Math.Max(2, iteracoesBase / multiplicador);
    }

    private static string CriarExplicacao(
        PerfilCargaNumerica perfil,
        ComparacaoEscalaBackendNumerico medicaoAtual,
        ComparacaoEscalaBackendNumerico? primeiraVitoriaGpu,
        ComparacaoEscalaBackendNumerico maiorEscalaTestada)
    {
        if (medicaoAtual.BackendMaisRapido == "gpu")
        {
            return $"na carga atual de {perfil.TipoCarga} (embedding={perfil.DimensaoEmbedding}, feed-forward={perfil.DimensaoFeedForward}), a GPU ja compensa o custo de disparo e copia";
        }

        if (primeiraVitoriaGpu is not null)
        {
            return $"na carga atual de {perfil.TipoCarga}, o backend GPU perde porque o custo de disparo de kernel e copia de dados ainda supera o ganho de paralelismo; ele so comeca a compensar perto de {primeiraVitoriaGpu.Multiplicador}x";
        }

        return $"na carga atual de {perfil.TipoCarga}, o backend GPU perde porque o custo de disparo de kernel e copia de dados domina; mesmo ate {maiorEscalaTestada.Multiplicador}x o CPU ainda ficou melhor neste backend experimental";
    }

    private static void Aquecer(
        IExecutorNumerico executor,
        double[] entrada,
        double[] saida,
        double[] vies,
        double[][] pesos,
        double[][] destino)
    {
        executor.MatrizVezesVetor(entrada, pesos, vies);
        executor.RetropropagarParaEntrada(saida, pesos);
        executor.SomarProdutoExternoEm(destino, entrada, saida);
    }

    private static double Medir(
        IExecutorNumerico executor,
        double[] entrada,
        double[] saida,
        double[] vies,
        double[][] pesos,
        double[][] destino,
        int iteracoes)
    {
        var cronometro = Stopwatch.StartNew();
        for (var indice = 0; indice < iteracoes; indice++)
        {
            executor.MatrizVezesVetor(entrada, pesos, vies);
            executor.RetropropagarParaEntrada(saida, pesos);
            executor.SomarProdutoExternoEm(destino, entrada, saida);
        }

        cronometro.Stop();
        return cronometro.Elapsed.TotalMilliseconds;
    }

    private static double[] CriarVetorAleatorio(int tamanho, Random gerador)
    {
        var vetor = new double[tamanho];
        for (var indice = 0; indice < tamanho; indice++)
        {
            vetor[indice] = gerador.NextDouble() * 2.0 - 1.0;
        }

        return vetor;
    }

    private static double[][] CriarMatrizAleatoria(int linhas, int colunas, Random gerador)
    {
        var matriz = OperacoesMatematicas.Zeros(linhas, colunas);
        for (var linha = 0; linha < linhas; linha++)
        {
            for (var coluna = 0; coluna < colunas; coluna++)
            {
                matriz[linha][coluna] = gerador.NextDouble() * 2.0 - 1.0;
            }
        }

        return matriz;
    }
}
