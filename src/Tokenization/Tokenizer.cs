namespace MiniLlmDidatico.Tokenizacao;

public sealed class Tokenizador
{
    private const string SeparadorMescla = "\u001F";

    private readonly ModeloTokenizador _modelo;
    private readonly Dictionary<int, string> _vocabularioReverso;

    public Tokenizador(ModeloTokenizador modelo)
    {
        _modelo = modelo;
        _vocabularioReverso = _modelo.Vocabulario.ToDictionary(x => x.Value, x => x.Key);
    }

    public IReadOnlyList<string> CodificarParaSimbolos(string texto)
    {
        var simbolos = TreinadorTokenizadorBpe.PreTokenizar(texto);

        foreach (var mescla in _modelo.Mesclas)
        {
            AplicarMesclaEm(simbolos, mescla);
        }

        return simbolos;
    }

    public IReadOnlyList<int> Codificar(string texto, bool adicionarInicio = false, bool adicionarFim = false)
    {
        var ids = new List<int>();

        if (adicionarInicio)
        {
            ids.Add(_modelo.IdInicio);
        }

        foreach (var simbolo in CodificarParaSimbolos(texto))
        {
            ids.Add(_modelo.Vocabulario.TryGetValue(simbolo, out var id) ? id : _modelo.IdDesconhecido);
        }

        if (adicionarFim)
        {
            ids.Add(_modelo.IdFim);
        }

        return ids;
    }

    public string Decodificar(IEnumerable<int> ids, bool ignorarTokensEspeciais = true)
    {
        var partes = new List<string>();

        foreach (var id in ids)
        {
            var token = _vocabularioReverso.TryGetValue(id, out var valor) ? valor : _modelo.TokenDesconhecido;

            if (ignorarTokensEspeciais && EhTokenEspecial(token))
            {
                continue;
            }

            partes.AddRange(SepararPartes(token));
        }

        return Destokenizar(partes);
    }

    public string DecodificarUnico(int id)
    {
        return _vocabularioReverso.TryGetValue(id, out var token)
            ? RenderizarSimbolo(token)
            : _modelo.TokenDesconhecido;
    }

    public string RenderizarSimbolo(string simbolo)
    {
        return Destokenizar(SepararPartes(simbolo));
    }

    public IReadOnlyList<string> ListarSimbolosDesconhecidos(string texto)
    {
        return CodificarParaSimbolos(texto)
            .Where(simbolo => !_modelo.Vocabulario.ContainsKey(simbolo))
            .Distinct()
            .Select(RenderizarSimbolo)
            .ToList();
    }

    private bool EhTokenEspecial(string token)
    {
        return token == _modelo.TokenPreenchimento
               || token == _modelo.TokenInicio
               || token == _modelo.TokenFim
               || token == _modelo.TokenDesconhecido;
    }

    private static IEnumerable<string> SepararPartes(string simbolo)
    {
        return simbolo.Split(SeparadorMescla, StringSplitOptions.None);
    }

    private static string Destokenizar(IEnumerable<string> partes)
    {
        var resultado = "";

        foreach (var parte in partes)
        {
            if (string.IsNullOrWhiteSpace(parte))
            {
                continue;
            }

            if (resultado.Length == 0)
            {
                resultado = parte;
                continue;
            }

            resultado += EhPontuacao(parte) ? parte : " " + parte;
        }

        return resultado;
    }

    private static bool EhPontuacao(string token)
    {
        return token.Length == 1 && char.IsPunctuation(token[0]);
    }

    private static void AplicarMesclaEm(List<string> sequencia, RegraMescla regra)
    {
        var indice = 0;
        while (indice < sequencia.Count - 1)
        {
            if (sequencia[indice] == regra.Esquerda && sequencia[indice + 1] == regra.Direita)
            {
                sequencia[indice] = regra.Mesclado;
                sequencia.RemoveAt(indice + 1);
            }
            else
            {
                indice++;
            }
        }
    }
}
