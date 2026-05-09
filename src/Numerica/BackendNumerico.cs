using MiniLlmDidatico.Diagnostico;

namespace MiniLlmDidatico.Numerica;

public interface IExecutorNumerico
{
    string NomeBackend { get; }
    string Dispositivo { get; }

    double[] Zeros(int tamanho);
    double[][] Zeros(int linhas, int colunas);
    double[][] MatrizAleatoria(int linhas, int colunas, Random geradorAleatorio, double escala = 0.02);
    double[][] EmbeddingsAleatorios(int linhas, int colunas, Random geradorAleatorio, double escala = 0.1);
    double ProdutoEscalar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB);
    double[] Somar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB);
    void SomarEm(double[] destino, IReadOnlyList<double> origem);
    double[] MatrizVezesVetor(IReadOnlyList<double> entrada, double[][] pesos, IReadOnlyList<double>? vies = null);
    void SomarProdutoExternoEm(double[][] destino, IReadOnlyList<double> esquerda, IReadOnlyList<double> direita);
    double[] RetropropagarParaEntrada(IReadOnlyList<double> gradienteSaida, double[][] pesos);
    double[] Softmax(IReadOnlyList<double> logits);
    int IndiceDoMaior(IReadOnlyList<double> valores);
    List<(int Indice, double Valor)> TopK(IReadOnlyList<double> valores, int quantidade);
    double[] ClonarVetor(IReadOnlyList<double> origem);
    double[][] ClonarMatriz(IReadOnlyList<IReadOnlyList<double>> origem);
    double[] Relu(IReadOnlyList<double> entrada);
    double[] ReluReversa(IReadOnlyList<double> entradaAntesRelu, IReadOnlyList<double> gradienteSaida);
    double PerdaEntropiaCruzada(IReadOnlyList<double> probabilidades, int indiceAlvo);
}

public sealed class ExecutorNumericoCpu : IExecutorNumerico
{
    public string NomeBackend => "cpu-padrao";
    public string Dispositivo => "cpu";

    public double[] Zeros(int tamanho) => OperacoesMatematicas.Zeros(tamanho);
    public double[][] Zeros(int linhas, int colunas) => OperacoesMatematicas.Zeros(linhas, colunas);
    public double[][] MatrizAleatoria(int linhas, int colunas, Random geradorAleatorio, double escala = 0.02) => OperacoesMatematicas.MatrizAleatoria(linhas, colunas, geradorAleatorio, escala);
    public double[][] EmbeddingsAleatorios(int linhas, int colunas, Random geradorAleatorio, double escala = 0.1) => OperacoesMatematicas.EmbeddingsAleatorios(linhas, colunas, geradorAleatorio, escala);
    public double ProdutoEscalar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB) => OperacoesMatematicas.ProdutoEscalar(vetorA, vetorB);
    public double[] Somar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB) => OperacoesMatematicas.Somar(vetorA, vetorB);
    public void SomarEm(double[] destino, IReadOnlyList<double> origem) => OperacoesMatematicas.SomarEm(destino, origem);
    public double[] MatrizVezesVetor(IReadOnlyList<double> entrada, double[][] pesos, IReadOnlyList<double>? vies = null) => OperacoesMatematicas.MatrizVezesVetor(entrada, pesos, vies);
    public void SomarProdutoExternoEm(double[][] destino, IReadOnlyList<double> esquerda, IReadOnlyList<double> direita) => OperacoesMatematicas.SomarProdutoExternoEm(destino, esquerda, direita);
    public double[] RetropropagarParaEntrada(IReadOnlyList<double> gradienteSaida, double[][] pesos) => OperacoesMatematicas.RetropropagarParaEntrada(gradienteSaida, pesos);
    public double[] Softmax(IReadOnlyList<double> logits) => OperacoesMatematicas.Softmax(logits);
    public int IndiceDoMaior(IReadOnlyList<double> valores) => OperacoesMatematicas.IndiceDoMaior(valores);
    public List<(int Indice, double Valor)> TopK(IReadOnlyList<double> valores, int quantidade) => OperacoesMatematicas.TopK(valores, quantidade);
    public double[] ClonarVetor(IReadOnlyList<double> origem) => OperacoesMatematicas.ClonarVetor(origem);
    public double[][] ClonarMatriz(IReadOnlyList<IReadOnlyList<double>> origem) => OperacoesMatematicas.ClonarMatriz(origem);
    public double[] Relu(IReadOnlyList<double> entrada) => OperacoesMatematicas.Relu(entrada);
    public double[] ReluReversa(IReadOnlyList<double> entradaAntesRelu, IReadOnlyList<double> gradienteSaida) => OperacoesMatematicas.ReluReversa(entradaAntesRelu, gradienteSaida);
    public double PerdaEntropiaCruzada(IReadOnlyList<double> probabilidades, int indiceAlvo) => OperacoesMatematicas.PerdaEntropiaCruzada(probabilidades, indiceAlvo);
}

public sealed class ContextoBackendNumerico
{
    public string NomeBackend { get; init; } = string.Empty;
    public string DispositivoUsado { get; init; } = string.Empty;
    public string DispositivoPreferidoPeloHardware { get; init; } = string.Empty;
    public bool BackendGpuDisponivel { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

public static class FabricaExecutorNumerico
{
    public static (IExecutorNumerico Executor, ContextoBackendNumerico Contexto) Criar(DecisaoExecucaoHardware decisaoHardware)
    {
        if (decisaoHardware.ComponenteUsadoAgora == "gpu" && decisaoHardware.BackendGpuDisponivelNoBuild)
        {
            try
            {
                var executorGpu = new ExecutorNumericoGpuCuda();
                return (executorGpu, new ContextoBackendNumerico
                {
                    NomeBackend = executorGpu.NomeBackend,
                    DispositivoUsado = executorGpu.Dispositivo,
                    DispositivoPreferidoPeloHardware = decisaoHardware.ComponenteMaisAdequadoPeloHardware,
                    BackendGpuDisponivel = true,
                    Motivo = $"backend GPU inicializado em {executorGpu.NomeDispositivo}"
                });
            }
            catch (Exception ex)
            {
                var executorCpuFallback = new ExecutorNumericoCpu();
                return (executorCpuFallback, new ContextoBackendNumerico
                {
                    NomeBackend = executorCpuFallback.NomeBackend,
                    DispositivoUsado = executorCpuFallback.Dispositivo,
                    DispositivoPreferidoPeloHardware = decisaoHardware.ComponenteMaisAdequadoPeloHardware,
                    BackendGpuDisponivel = false,
                    Motivo = $"fallback para CPU: falha ao criar backend GPU ({ex.Message})"
                });
            }
        }

        var executor = new ExecutorNumericoCpu();
        return (executor, new ContextoBackendNumerico
        {
            NomeBackend = executor.NomeBackend,
            DispositivoUsado = executor.Dispositivo,
            DispositivoPreferidoPeloHardware = decisaoHardware.ComponenteMaisAdequadoPeloHardware,
            BackendGpuDisponivel = decisaoHardware.BackendGpuDisponivelNoBuild,
            Motivo = decisaoHardware.Motivo
        });
    }
}
