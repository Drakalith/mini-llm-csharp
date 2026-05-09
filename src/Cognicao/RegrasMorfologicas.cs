using System.Text.Json;
using MiniLlmDidatico.Arquivos;

namespace MiniLlmDidatico.Cognicao;

public sealed class PadraoConjugacao
{
    public string SufixoEntrada { get; set; } = string.Empty;
    public string SufixoSaida { get; set; } = string.Empty;
    public int TamanhoMinimo { get; set; }
    public List<string>? RaizTerminacoes { get; set; }
    public List<string>? RaizTerminacoesExcluidas { get; set; }
    public string? SufixoExcluido { get; set; }
    public string Descricao { get; set; } = string.Empty;
}

public sealed class EstadoRegrasMorfologicas
{
    public int Versao { get; set; }
    public Dictionary<string, string> MapaPronomes { get; set; } = new();
    public Dictionary<string, string> VerbosIrregularesPresente { get; set; } = new();
    public Dictionary<string, string> VerbosIrregularesPretPerfeito { get; set; } = new();
    public List<PadraoConjugacao> PadroesConjugacao { get; set; } = new();
}

public static class RegrasMorfologicas
{
    private static EstadoRegrasMorfologicas? _estado;
    private static readonly string CaminhoPadrao = Path.Combine("artefatos", "18_regras_morfologicas.json");

    public static EstadoRegrasMorfologicas Carregar(string? caminho = null)
    {
        if (_estado is not null) return _estado;

        var arquivo = caminho ?? CaminhoPadrao;
        if (File.Exists(arquivo))
        {
            var json = File.ReadAllText(arquivo, PadroesSerializacao.Utf8SemBom);
            _estado = JsonSerializer.Deserialize<EstadoRegrasMorfologicas>(json, PadroesSerializacao.CriarOpcoesJson())
                      ?? new EstadoRegrasMorfologicas();
        }
        else
        {
            _estado = new EstadoRegrasMorfologicas();
        }

        return _estado;
    }

    public static void Resetar()
    {
        _estado = null;
    }
}
