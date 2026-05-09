using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Modelo;
using MiniLlmDidatico.Numerica;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Geracao;

public sealed class ParametrosAmostragemGeracao
{
    public double Temperatura { get; init; }
    public int TopK { get; init; }
    public double TopP { get; init; } = 1.0;

    public static ParametrosAmostragemGeracao DeConfiguracao(ConfiguracaoGeracao configuracao)
    {
        return new ParametrosAmostragemGeracao
        {
            Temperatura = configuracao.Temperatura,
            TopK = configuracao.TopK,
            TopP = configuracao.TopP
        };
    }
}

public sealed class Gerador
{
    private readonly MiniTransformador _modelo;
    private readonly Tokenizador _tokenizador;
    private readonly ModeloTokenizador _modeloTokenizador;
    private readonly ParametrosAmostragemGeracao _parametrosAmostragem;
    private readonly Random _geradorAleatorio;

    public Gerador(
        MiniTransformador modelo,
        Tokenizador tokenizador,
        ModeloTokenizador modeloTokenizador,
        ParametrosAmostragemGeracao? parametrosAmostragem = null,
        int sementeAleatoria = 42)
    {
        _modelo = modelo;
        _tokenizador = tokenizador;
        _modeloTokenizador = modeloTokenizador;
        _parametrosAmostragem = parametrosAmostragem ?? new ParametrosAmostragemGeracao();
        _geradorAleatorio = new Random(sementeAleatoria);
    }

    public Tokenizador Tokenizador => _tokenizador;
    public ParametrosAmostragemGeracao ParametrosAmostragem => _parametrosAmostragem;

    public RastroGeracao Gerar(string prompt, int maximoNovosTokens)
    {
        var rastro = new RastroGeracao
        {
            PromptInterno = prompt,
            ModoSelecaoToken = DescreverModoSelecao(),
            Temperatura = TemperaturaEfetiva(),
            TopK = TopKEfetivo(),
            TopP = TopPEfetivo()
        };

        foreach (var passo in GerarEmFluxo(prompt, maximoNovosTokens))
        {
            rastro.Passos.Add(passo);
        }

        rastro.TextoCompletoDecodificado = rastro.Passos.LastOrDefault()?.TextoParcialDecodificado ?? string.Empty;
        return rastro;
    }

    public IEnumerable<PassoTokenGerado> GerarEmFluxo(string prompt, int maximoNovosTokens)
    {
        var idsPrompt = _tokenizador.Codificar(prompt, adicionarInicio: true, adicionarFim: false).ToList();
        var idsTrabalho = new List<int>(idsPrompt);
        var idsGerados = new List<int>();

        for (var passo = 0; passo < maximoNovosTokens; passo++)
        {
            var idsContexto = idsTrabalho.Count <= _modelo.MaximoComprimentoSequencia
                ? idsTrabalho
                : idsTrabalho.Skip(idsTrabalho.Count - _modelo.MaximoComprimentoSequencia).ToList();

            var cache = _modelo.PassagemDireta(idsContexto);
            var ultimaPosicao = idsContexto.Count - 1;
            var probabilidades = cache.Probabilidades[ultimaPosicao];

            var idPrevisto = SelecionarProximoToken(probabilidades);
            var tokenPrevisto = _tokenizador.DecodificarUnico(idPrevisto);

            idsTrabalho.Add(idPrevisto);

            var melhoresEscolhas = _modelo.ExecutorNumerico.TopK(probabilidades, 5)
                .Select(x => new ProbabilidadeToken
                {
                    IdToken = x.Indice,
                    TextoToken = _tokenizador.DecodificarUnico(x.Indice),
                    Probabilidade = x.Valor
                })
                .ToList();

            if (idPrevisto != _modeloTokenizador.IdFim)
            {
                idsGerados.Add(idPrevisto);
            }

            yield return new PassoTokenGerado
            {
                Passo = passo + 1,
                IdTokenPrevisto = idPrevisto,
                TextoTokenPrevisto = tokenPrevisto,
                ProbabilidadeTokenPrevisto = probabilidades[idPrevisto],
                TextoParcialDecodificado = _tokenizador.Decodificar(idsGerados),
                MelhoresEscolhas = melhoresEscolhas
            };

            if (idPrevisto == _modeloTokenizador.IdFim)
            {
                yield break;
            }
        }
    }

    public string DescreverModoSelecao()
    {
        return UsaSelecaoGulosa()
            ? "greedy"
            : $"amostragem (temperature={TemperaturaEfetiva():0.00}, top-k={TopKEfetivo()}, top-p={TopPEfetivo():0.00})";
    }

    private int SelecionarProximoToken(IReadOnlyList<double> probabilidades)
    {
        if (UsaSelecaoGulosa())
        {
            return _modelo.ExecutorNumerico.IndiceDoMaior(probabilidades);
        }

        var candidatos = ConstruirCandidatos(probabilidades);
        if (candidatos.Count == 1)
        {
            return candidatos[0].Indice;
        }

        var temperatura = TemperaturaEfetiva();
        var pesos = new double[candidatos.Count];
        var somaPesos = 0.0;

        for (var i = 0; i < candidatos.Count; i++)
        {
            var peso = Math.Pow(Math.Max(candidatos[i].Probabilidade, 1e-12), 1.0 / temperatura);
            pesos[i] = peso;
            somaPesos += peso;
        }

        if (somaPesos <= 0.0 || double.IsNaN(somaPesos))
        {
            return candidatos[0].Indice;
        }

        var alvo = _geradorAleatorio.NextDouble() * somaPesos;
        var acumulado = 0.0;

        for (var i = 0; i < candidatos.Count; i++)
        {
            acumulado += pesos[i];
            if (alvo <= acumulado)
            {
                return candidatos[i].Indice;
            }
        }

        return candidatos[^1].Indice;
    }

    private List<(int Indice, double Probabilidade)> ConstruirCandidatos(IReadOnlyList<double> probabilidades)
    {
        var candidatos = probabilidades
            .Select((probabilidade, indice) => new
            {
                Indice = indice,
                Probabilidade = double.IsNaN(probabilidade) || probabilidade < 0.0 ? 0.0 : probabilidade
            })
            .Where(x => x.Probabilidade > 0.0)
            .OrderByDescending(x => x.Probabilidade)
            .Select(x => (x.Indice, x.Probabilidade))
            .ToList();

        if (candidatos.Count == 0)
        {
            var indiceFallback = _modelo.ExecutorNumerico.IndiceDoMaior(probabilidades);
            return [(indiceFallback, Math.Max(probabilidades[indiceFallback], 1e-12))];
        }

        var topK = TopKEfetivo();
        if (topK > 0 && candidatos.Count > topK)
        {
            candidatos = candidatos.Take(topK).ToList();
        }

        var topP = TopPEfetivo();
        if (topP < 1.0)
        {
            var cumulativo = 0.0;
            var filtrados = new List<(int Indice, double Probabilidade)>();

            foreach (var candidato in candidatos)
            {
                filtrados.Add(candidato);
                cumulativo += candidato.Probabilidade;
                if (cumulativo >= topP)
                {
                    break;
                }
            }

            candidatos = filtrados;
        }

        return candidatos;
    }

    private bool UsaSelecaoGulosa()
    {
        return _parametrosAmostragem.Temperatura <= 0.0 &&
               _parametrosAmostragem.TopK <= 0 &&
               (_parametrosAmostragem.TopP <= 0.0 || _parametrosAmostragem.TopP >= 1.0);
    }

    private double TemperaturaEfetiva()
    {
        return _parametrosAmostragem.Temperatura > 0.0
            ? _parametrosAmostragem.Temperatura
            : 1.0;
    }

    private int TopKEfetivo()
    {
        return Math.Max(0, _parametrosAmostragem.TopK);
    }

    private double TopPEfetivo()
    {
        return _parametrosAmostragem.TopP <= 0.0 || _parametrosAmostragem.TopP >= 1.0
            ? 1.0
            : _parametrosAmostragem.TopP;
    }
}
