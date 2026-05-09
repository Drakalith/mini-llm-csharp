using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace MiniLlmDidatico.Numerica;

public sealed class RelatorioDisponibilidadeGpuCuda
{
    public bool Disponivel { get; init; }
    public string NomeDispositivo { get; init; } = string.Empty;
    public long MemoriaBytes { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

public static class SondaGpuCuda
{
    private static readonly Lazy<RelatorioDisponibilidadeGpuCuda> _sonda = new(AnalisarInternamente);

    public static RelatorioDisponibilidadeGpuCuda Analisar() => _sonda.Value;

    private static RelatorioDisponibilidadeGpuCuda AnalisarInternamente()
    {
        try
        {
            using var contexto = Context.Create(builder => builder.Cuda());
            var dispositivo = ObterMelhorDispositivoCuda(contexto);

            if (dispositivo is null)
            {
                return new RelatorioDisponibilidadeGpuCuda
                {
                    Disponivel = false,
                    Motivo = "nenhum dispositivo CUDA foi encontrado pelo ILGPU"
                };
            }

            return new RelatorioDisponibilidadeGpuCuda
            {
                Disponivel = true,
                NomeDispositivo = dispositivo.Name,
                MemoriaBytes = ConverterMemoria(dispositivo.MemorySize),
                Motivo = "backend CUDA inicializavel"
            };
        }
        catch (Exception ex)
        {
            return new RelatorioDisponibilidadeGpuCuda
            {
                Disponivel = false,
                Motivo = $"falha ao inicializar CUDA via ILGPU: {ex.Message}"
            };
        }
    }

    private static CudaDevice? ObterMelhorDispositivoCuda(Context contexto)
    {
        var dispositivos = contexto.GetCudaDevices();
        CudaDevice? melhor = null;
        long melhorMemoria = long.MinValue;

        for (var indice = 0; indice < dispositivos.Count; indice++)
        {
            var atual = dispositivos[indice];
            var memoriaAtual = ConverterMemoria(atual.MemorySize);
            if (memoriaAtual > melhorMemoria)
            {
                melhor = atual;
                melhorMemoria = memoriaAtual;
            }
        }

        return melhor;
    }

    private static long ConverterMemoria(object? valorMemoria)
    {
        return valorMemoria switch
        {
            null => 0L,
            long valorLong => valorLong,
            int valorInt => valorInt,
            _ => Convert.ToInt64(valorMemoria)
        };
    }
}

public sealed class ExecutorNumericoGpuCuda : IExecutorNumerico, IDisposable
{
    private readonly ExecutorNumericoCpu _fallbackCpu = new();
    private readonly Context _contexto;
    private readonly Accelerator _acelerador;
    private readonly Action<Index1D, ArrayView<double>, ArrayView<double>, int, int, ArrayView<double>, int, ArrayView<double>> _kernelMatrizVetor;
    private readonly Action<Index1D, ArrayView<double>, int, ArrayView<double>, ArrayView<double>> _kernelProdutoExternoAditivo;
    private readonly Action<Index1D, ArrayView<double>, ArrayView<double>, int, ArrayView<double>> _kernelRetropropagacaoEntrada;
    private bool _descartado;

    public ExecutorNumericoGpuCuda()
    {
        _contexto = Context.Create(builder => builder.Cuda());
        var dispositivo = ObterMelhorDispositivoCuda(_contexto)
            ?? throw new InvalidOperationException("Nenhum dispositivo CUDA foi encontrado para o backend GPU.");

        _acelerador = dispositivo.CreateAccelerator(_contexto);
        NomeDispositivo = dispositivo.Name;
        MemoriaDispositivoBytes = ConverterMemoria(dispositivo.MemorySize);

        _kernelMatrizVetor = _acelerador.LoadAutoGroupedStreamKernel<
            Index1D,
            ArrayView<double>,
            ArrayView<double>,
            int,
            int,
            ArrayView<double>,
            int,
            ArrayView<double>>(KernelMatrizVetor);

        _kernelProdutoExternoAditivo = _acelerador.LoadAutoGroupedStreamKernel<
            Index1D,
            ArrayView<double>,
            int,
            ArrayView<double>,
            ArrayView<double>>(KernelProdutoExternoAditivo);

        _kernelRetropropagacaoEntrada = _acelerador.LoadAutoGroupedStreamKernel<
            Index1D,
            ArrayView<double>,
            ArrayView<double>,
            int,
            ArrayView<double>>(KernelRetropropagacaoEntrada);
    }

    public string NomeBackend => "gpu-cuda-experimental";
    public string Dispositivo => "gpu";
    public string NomeDispositivo { get; }
    public long MemoriaDispositivoBytes { get; }

    public double[] Zeros(int tamanho) => _fallbackCpu.Zeros(tamanho);
    public double[][] Zeros(int linhas, int colunas) => _fallbackCpu.Zeros(linhas, colunas);
    public double[][] MatrizAleatoria(int linhas, int colunas, Random geradorAleatorio, double escala = 0.02) => _fallbackCpu.MatrizAleatoria(linhas, colunas, geradorAleatorio, escala);
    public double[][] EmbeddingsAleatorios(int linhas, int colunas, Random geradorAleatorio, double escala = 0.1) => _fallbackCpu.EmbeddingsAleatorios(linhas, colunas, geradorAleatorio, escala);
    public double ProdutoEscalar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB) => _fallbackCpu.ProdutoEscalar(vetorA, vetorB);
    public double[] Somar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB) => _fallbackCpu.Somar(vetorA, vetorB);
    public void SomarEm(double[] destino, IReadOnlyList<double> origem) => _fallbackCpu.SomarEm(destino, origem);
    public double[] Softmax(IReadOnlyList<double> logits) => _fallbackCpu.Softmax(logits);
    public int IndiceDoMaior(IReadOnlyList<double> valores) => _fallbackCpu.IndiceDoMaior(valores);
    public List<(int Indice, double Valor)> TopK(IReadOnlyList<double> valores, int quantidade) => _fallbackCpu.TopK(valores, quantidade);
    public double[] ClonarVetor(IReadOnlyList<double> origem) => _fallbackCpu.ClonarVetor(origem);
    public double[][] ClonarMatriz(IReadOnlyList<IReadOnlyList<double>> origem) => _fallbackCpu.ClonarMatriz(origem);
    public double[] Relu(IReadOnlyList<double> entrada) => _fallbackCpu.Relu(entrada);
    public double[] ReluReversa(IReadOnlyList<double> entradaAntesRelu, IReadOnlyList<double> gradienteSaida) => _fallbackCpu.ReluReversa(entradaAntesRelu, gradienteSaida);
    public double PerdaEntropiaCruzada(IReadOnlyList<double> probabilidades, int indiceAlvo) => _fallbackCpu.PerdaEntropiaCruzada(probabilidades, indiceAlvo);

    public double[] MatrizVezesVetor(IReadOnlyList<double> entrada, double[][] pesos, IReadOnlyList<double>? vies = null)
    {
        GarantirNaoDescartado();

        var entradaCpu = entrada as double[] ?? entrada.ToArray();
        var pesosPlanos = PlanificarMatriz(pesos);
        var viesCpu = vies is null ? [0.0] : (vies as double[] ?? vies.ToArray());
        var tamanhoEntrada = entradaCpu.Length;
        var tamanhoSaida = pesos[0].Length;

        using var entradaBuffer = _acelerador.Allocate1D<double>(tamanhoEntrada);
        using var pesosBuffer = _acelerador.Allocate1D<double>(pesosPlanos.Length);
        using var viesBuffer = _acelerador.Allocate1D<double>(System.Math.Max(viesCpu.Length, 1));
        using var resultadoBuffer = _acelerador.Allocate1D<double>(tamanhoSaida);

        entradaBuffer.CopyFromCPU(entradaCpu);
        pesosBuffer.CopyFromCPU(pesosPlanos);
        viesBuffer.CopyFromCPU(viesCpu);

        _kernelMatrizVetor(
            tamanhoSaida,
            entradaBuffer.View,
            pesosBuffer.View,
            tamanhoEntrada,
            tamanhoSaida,
            viesBuffer.View,
            vies is null ? 0 : 1,
            resultadoBuffer.View);
        _acelerador.Synchronize();

        return resultadoBuffer.GetAsArray1D();
    }

    public void SomarProdutoExternoEm(double[][] destino, IReadOnlyList<double> esquerda, IReadOnlyList<double> direita)
    {
        GarantirNaoDescartado();

        var destinoPlano = PlanificarMatriz(destino);
        var esquerdaCpu = esquerda as double[] ?? esquerda.ToArray();
        var direitaCpu = direita as double[] ?? direita.ToArray();
        var colunas = direitaCpu.Length;

        using var destinoBuffer = _acelerador.Allocate1D<double>(destinoPlano.Length);
        using var esquerdaBuffer = _acelerador.Allocate1D<double>(esquerdaCpu.Length);
        using var direitaBuffer = _acelerador.Allocate1D<double>(direitaCpu.Length);

        destinoBuffer.CopyFromCPU(destinoPlano);
        esquerdaBuffer.CopyFromCPU(esquerdaCpu);
        direitaBuffer.CopyFromCPU(direitaCpu);

        _kernelProdutoExternoAditivo(
            destinoPlano.Length,
            destinoBuffer.View,
            colunas,
            esquerdaBuffer.View,
            direitaBuffer.View);
        _acelerador.Synchronize();

        AplicarMatrizPlanificada(destinoBuffer.GetAsArray1D(), destino);
    }

    public double[] RetropropagarParaEntrada(IReadOnlyList<double> gradienteSaida, double[][] pesos)
    {
        GarantirNaoDescartado();

        var gradienteCpu = gradienteSaida as double[] ?? gradienteSaida.ToArray();
        var pesosPlanos = PlanificarMatriz(pesos);
        var tamanhoEntrada = pesos.Length;
        var tamanhoSaida = gradienteCpu.Length;

        using var gradienteBuffer = _acelerador.Allocate1D<double>(tamanhoSaida);
        using var pesosBuffer = _acelerador.Allocate1D<double>(pesosPlanos.Length);
        using var resultadoBuffer = _acelerador.Allocate1D<double>(tamanhoEntrada);

        gradienteBuffer.CopyFromCPU(gradienteCpu);
        pesosBuffer.CopyFromCPU(pesosPlanos);

        _kernelRetropropagacaoEntrada(
            tamanhoEntrada,
            gradienteBuffer.View,
            pesosBuffer.View,
            tamanhoSaida,
            resultadoBuffer.View);
        _acelerador.Synchronize();

        return resultadoBuffer.GetAsArray1D();
    }

    public void Dispose()
    {
        if (_descartado)
        {
            return;
        }

        _acelerador.Dispose();
        _contexto.Dispose();
        _descartado = true;
    }

    private void GarantirNaoDescartado()
    {
        ObjectDisposedException.ThrowIf(_descartado, this);
    }

    private static void KernelMatrizVetor(
        Index1D indiceSaida,
        ArrayView<double> entrada,
        ArrayView<double> pesosPlanos,
        int tamanhoEntrada,
        int tamanhoSaida,
        ArrayView<double> vies,
        int usarVies,
        ArrayView<double> resultado)
    {
        var saida = (int)indiceSaida;
        if (saida >= tamanhoSaida)
        {
            return;
        }

        var soma = usarVies != 0 ? vies[saida] : 0.0;
        for (var entradaIndice = 0; entradaIndice < tamanhoEntrada; entradaIndice++)
        {
            soma += entrada[entradaIndice] * pesosPlanos[entradaIndice * tamanhoSaida + saida];
        }

        resultado[saida] = soma;
    }

    private static void KernelProdutoExternoAditivo(
        Index1D indicePlano,
        ArrayView<double> destinoPlano,
        int colunas,
        ArrayView<double> esquerda,
        ArrayView<double> direita)
    {
        var indice = (int)indicePlano;
        if (indice >= destinoPlano.Length)
        {
            return;
        }

        var linha = indice / colunas;
        var coluna = indice % colunas;
        destinoPlano[indice] += esquerda[linha] * direita[coluna];
    }

    private static void KernelRetropropagacaoEntrada(
        Index1D indiceEntrada,
        ArrayView<double> gradienteSaida,
        ArrayView<double> pesosPlanos,
        int tamanhoSaida,
        ArrayView<double> resultado)
    {
        var entrada = (int)indiceEntrada;
        if (entrada >= resultado.Length)
        {
            return;
        }

        var soma = 0.0;
        for (var saida = 0; saida < tamanhoSaida; saida++)
        {
            soma += pesosPlanos[entrada * tamanhoSaida + saida] * gradienteSaida[saida];
        }

        resultado[entrada] = soma;
    }

    private static double[] PlanificarMatriz(double[][] matriz)
    {
        var linhas = matriz.Length;
        var colunas = linhas == 0 ? 0 : matriz[0].Length;
        var plana = new double[linhas * colunas];

        for (var linha = 0; linha < linhas; linha++)
        {
            Array.Copy(matriz[linha], 0, plana, linha * colunas, colunas);
        }

        return plana;
    }

    private static void AplicarMatrizPlanificada(double[] plana, double[][] destino)
    {
        var colunas = destino[0].Length;
        for (var linha = 0; linha < destino.Length; linha++)
        {
            Array.Copy(plana, linha * colunas, destino[linha], 0, colunas);
        }
    }

    private static long ConverterMemoria(object? valorMemoria)
    {
        return valorMemoria switch
        {
            null => 0L,
            long valorLong => valorLong,
            int valorInt => valorInt,
            _ => Convert.ToInt64(valorMemoria)
        };
    }

    private static CudaDevice? ObterMelhorDispositivoCuda(Context contexto)
    {
        var dispositivos = contexto.GetCudaDevices();
        CudaDevice? melhor = null;
        long melhorMemoria = long.MinValue;

        for (var indice = 0; indice < dispositivos.Count; indice++)
        {
            var atual = dispositivos[indice];
            var memoriaAtual = ConverterMemoria(atual.MemorySize);
            if (memoriaAtual > melhorMemoria)
            {
                melhor = atual;
                melhorMemoria = memoriaAtual;
            }
        }

        return melhor;
    }
}
