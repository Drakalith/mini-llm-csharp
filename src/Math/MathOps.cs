namespace MiniLlmDidatico.Numerica;

public static class OperacoesMatematicas
{
    public static double[] Zeros(int tamanho) => new double[tamanho];

    public static double[][] Zeros(int linhas, int colunas)
    {
        var matriz = new double[linhas][];
        for (var i = 0; i < linhas; i++)
        {
            matriz[i] = new double[colunas];
        }

        return matriz;
    }

    public static double[][] MatrizAleatoria(int linhas, int colunas, Random geradorAleatorio, double escala = 0.02)
    {
        var matriz = Zeros(linhas, colunas);
        for (var i = 0; i < linhas; i++)
        {
            for (var j = 0; j < colunas; j++)
            {
                matriz[i][j] = (geradorAleatorio.NextDouble() * 2.0 - 1.0) * escala;
            }
        }

        return matriz;
    }

    public static double[][] EmbeddingsAleatorios(int linhas, int colunas, Random geradorAleatorio, double escala = 0.1)
    {
        return MatrizAleatoria(linhas, colunas, geradorAleatorio, escala);
    }

    public static double ProdutoEscalar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB)
    {
        var total = 0.0;
        for (var i = 0; i < vetorA.Count; i++)
        {
            total += vetorA[i] * vetorB[i];
        }

        return total;
    }

    public static double[] Somar(IReadOnlyList<double> vetorA, IReadOnlyList<double> vetorB)
    {
        var resultado = new double[vetorA.Count];
        for (var i = 0; i < vetorA.Count; i++)
        {
            resultado[i] = vetorA[i] + vetorB[i];
        }

        return resultado;
    }

    public static void SomarEm(double[] destino, IReadOnlyList<double> origem)
    {
        for (var i = 0; i < destino.Length; i++)
        {
            destino[i] += origem[i];
        }
    }

    public static double[] MatrizVezesVetor(IReadOnlyList<double> entrada, double[][] pesos, IReadOnlyList<double>? vies = null)
    {
        var tamanhoSaida = pesos[0].Length;
        var resultado = new double[tamanhoSaida];

        for (var j = 0; j < tamanhoSaida; j++)
        {
            var soma = vies is null ? 0.0 : vies[j];
            for (var i = 0; i < entrada.Count; i++)
            {
                soma += entrada[i] * pesos[i][j];
            }

            resultado[j] = soma;
        }

        return resultado;
    }

    public static void SomarProdutoExternoEm(double[][] destino, IReadOnlyList<double> esquerda, IReadOnlyList<double> direita)
    {
        for (var i = 0; i < esquerda.Count; i++)
        {
            for (var j = 0; j < direita.Count; j++)
            {
                destino[i][j] += esquerda[i] * direita[j];
            }
        }
    }

    public static double[] RetropropagarParaEntrada(IReadOnlyList<double> gradienteSaida, double[][] pesos)
    {
        var tamanhoEntrada = pesos.Length;
        var resultado = new double[tamanhoEntrada];

        for (var i = 0; i < tamanhoEntrada; i++)
        {
            var soma = 0.0;
            for (var j = 0; j < gradienteSaida.Count; j++)
            {
                soma += pesos[i][j] * gradienteSaida[j];
            }

            resultado[i] = soma;
        }

        return resultado;
    }

    public static double[] Softmax(IReadOnlyList<double> logits)
    {
        var max = logits.Max();
        var exponenciais = new double[logits.Count];
        var soma = 0.0;

        for (var i = 0; i < logits.Count; i++)
        {
            exponenciais[i] = System.Math.Exp(logits[i] - max);
            soma += exponenciais[i];
        }

        for (var i = 0; i < exponenciais.Length; i++)
        {
            exponenciais[i] /= soma;
        }

        return exponenciais;
    }

    public static int IndiceDoMaior(IReadOnlyList<double> valores)
    {
        var melhorIndice = 0;
        var melhorValor = valores[0];

        for (var i = 1; i < valores.Count; i++)
        {
            if (valores[i] > melhorValor)
            {
                melhorValor = valores[i];
                melhorIndice = i;
            }
        }

        return melhorIndice;
    }

    public static List<(int Indice, double Valor)> TopK(IReadOnlyList<double> valores, int quantidade)
    {
        return valores
            .Select((valor, indice) => (Indice: indice, Valor: valor))
            .OrderByDescending(x => x.Valor)
            .Take(quantidade)
            .ToList();
    }

    public static double[] ClonarVetor(IReadOnlyList<double> origem)
    {
        var copia = new double[origem.Count];
        for (var i = 0; i < origem.Count; i++)
        {
            copia[i] = origem[i];
        }

        return copia;
    }

    public static double[][] ClonarMatriz(IReadOnlyList<IReadOnlyList<double>> origem)
    {
        var matriz = new double[origem.Count][];
        for (var i = 0; i < origem.Count; i++)
        {
            matriz[i] = ClonarVetor(origem[i]);
        }

        return matriz;
    }

    public static double[] Relu(IReadOnlyList<double> entrada)
    {
        var resultado = new double[entrada.Count];
        for (var i = 0; i < entrada.Count; i++)
        {
            resultado[i] = entrada[i] > 0.0 ? entrada[i] : 0.0;
        }

        return resultado;
    }

    public static double[] ReluReversa(IReadOnlyList<double> entradaAntesRelu, IReadOnlyList<double> gradienteSaida)
    {
        var gradiente = new double[entradaAntesRelu.Count];
        for (var i = 0; i < entradaAntesRelu.Count; i++)
        {
            gradiente[i] = entradaAntesRelu[i] > 0.0 ? gradienteSaida[i] : 0.0;
        }

        return gradiente;
    }

    public static double PerdaEntropiaCruzada(IReadOnlyList<double> probabilidades, int indiceAlvo)
    {
        var p = System.Math.Max(probabilidades[indiceAlvo], 1e-12);
        return -System.Math.Log(p);
    }

    public static string FormatarVetor(IReadOnlyList<double> valores, int maximoItens = 8)
    {
        var exibidos = valores.Take(maximoItens).Select(v => v.ToString("0.0000"));
        var sufixo = valores.Count > maximoItens ? ", ..." : string.Empty;
        return "[" + string.Join(", ", exibidos) + sufixo + "]";
    }
}
