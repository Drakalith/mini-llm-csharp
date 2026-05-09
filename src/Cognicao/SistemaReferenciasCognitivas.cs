using System.Text.Json;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Dados;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class ReferenciaCognitiva
{
    public string Id { get; init; } = string.Empty;
    public string Conceito { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public List<string> Atributos { get; init; } = new();
    public List<string> Exemplos { get; init; } = new();
    public List<string> Contrastes { get; init; } = new();
    public List<string> Relacionados { get; init; } = new();
    public double Peso { get; set; } = 1.0;
    public int FrequenciaUso { get; set; } = 1;
    public DateTimeOffset CriadoEmBrasilSp { get; init; }
    public DateTimeOffset UltimaAtivacaoBrasilSp { get; set; }
}

public sealed class EstadoReferenciasCognitivas
{
    public string VersaoFormato { get; set; } = "1";
    public List<ReferenciaCognitiva> Referencias { get; init; } = new();
}

public static class SistemaReferenciasCognitivas
{
    private const string NomeArquivo = "16_referencias_cognitivas.json";

    private static readonly List<ReferenciaCognitiva> ReferenciasNativas =
    [
        Criar("pessoa", "um ser humano com identidade, corpo e mente",
            atributos: ["nome", "idade", "corpo", "personalidade", "emocoes", "memorias"],
            exemplos: ["eu", "voce", "amigo", "familia"],
            contrastes: ["objeto", "animal", "lugar"]),

        Criar("lugar", "um espaco fisico ou geografico onde algo existe",
            atributos: ["nome", "localizacao", "tamanho", "clima", "caracteristicas"],
            exemplos: ["casa", "trabalho", "cidade", "pais", "parque"],
            contrastes: ["pessoa", "tempo", "ideia"]),

        Criar("tempo", "uma dimensao de duracao e sequencia de eventos",
            atributos: ["duracao", "momento", "frequencia", "periodo"],
            exemplos: ["hoje", "ontem", "semana", "mes", "ano", "manha", "noite"],
            contrastes: ["espaco", "lugar", "objeto"]),

        Criar("objeto", "algo concreto que pode ser possuido ou usado",
            atributos: ["nome", "tipo", "cor", "tamanho", "funcao"],
            exemplos: ["carro", "livro", "telefone", "computador"],
            contrastes: ["pessoa", "ideia", "emocao"]),

        Criar("relacao", "uma conexao entre duas ou mais entidades",
            atributos: ["tipo", "intensidade", "duracao", "reciprocidade"],
            exemplos: ["amizade", "familia", "trabalho", "amor"],
            contrastes: ["isolamento", "solidao"]),

        Criar("atividade", "algo que alguem faz ou pratica",
            atributos: ["nome", "frequencia", "duracao", "local", "proposito"],
            exemplos: ["trabalhar", "estudar", "correr", "jogar", "cozinhar"],
            contrastes: ["descanso", "ociosidade"]),

        Criar("preferencia", "algo que alguem gosta ou escolhe",
            atributos: ["tipo", "intensidade", "origem", "estabilidade"],
            exemplos: ["musica", "comida", "cor", "hobby", "estilo"],
            contrastes: ["aversao", "indiferenca"]),

        Criar("profissao", "um tipo de trabalho ou ocupacao",
            atributos: ["nome", "area", "habilidades", "salario", "local"],
            exemplos: ["programador", "medico", "professor", "designer"],
            contrastes: ["desemprego", "aposentadoria"]),

        Criar("animal_domestico", "um animal que vive com uma pessoa",
            atributos: ["nome", "especie", "raca", "idade", "personalidade"],
            exemplos: ["cachorro", "gato", "passaro", "peixe"],
            contrastes: ["animal selvagem", "praga"]),

        Criar("emocao", "um estado interno de sentimento",
            atributos: ["tipo", "intensidade", "duracao", "causa"],
            exemplos: ["alegria", "tristeza", "raiva", "medo", "amor"],
            contrastes: ["neutralidade", "apatia"]),

        Criar("objetivo", "algo que alguem quer alcancar",
            atributos: ["tipo", "prazo", "dificuldade", "importancia"],
            exemplos: ["mudar de emprego", "aprender idioma", "viajar"],
            contrastes: ["desistencia", "conformismo"]),

        Criar("rotina", "algo que alguem faz regularmente",
            atributos: ["frequencia", "horario", "duracao", "local"],
            exemplos: ["acordar cedo", "correr", "trabalhar", "dormir"],
            contrastes: ["imprevisibilidade", "caos"]),
    ];

    private static ReferenciaCognitiva Criar(
        string conceito,
        string descricao,
        string[]? atributos = null,
        string[]? exemplos = null,
        string[]? contrastes = null)
    {
        return new ReferenciaCognitiva
        {
            Id = $"ref_{conceito.ToLowerInvariant()}",
            Conceito = conceito,
            Descricao = descricao,
            Atributos = atributos?.ToList() ?? new List<string>(),
            Exemplos = exemplos?.ToList() ?? new List<string>(),
            Contrastes = contrastes?.ToList() ?? new List<string>(),
            Relacionados = new List<string>(),
            CriadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp(),
            UltimaAtivacaoBrasilSp = RelogioBrasil.AgoraBrasilSp()
        };
    }

    public static List<ReferenciaCognitiva> CarregarOuCriar(string raizArtefatos)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);

        if (File.Exists(caminho))
        {
            try
            {
                var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
                var estado = JsonSerializer.Deserialize<EstadoReferenciasCognitivas>(json, PadroesSerializacao.CriarOpcoesJson());
                if (estado is not null && estado.Referencias.Count > 0)
                {
                    return estado.Referencias;
                }
            }
            catch { }
        }

        var referencias = new List<ReferenciaCognitiva>(ReferenciasNativas);
        Persistir(raizArtefatos, referencias);
        return referencias;
    }

    public static void Persistir(string raizArtefatos, List<ReferenciaCognitiva> referencias)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminho = Path.Combine(raizArtefatos, NomeArquivo);
        var estado = new EstadoReferenciasCognitivas { Referencias = referencias };
        var json = JsonSerializer.Serialize(estado, PadroesSerializacao.CriarOpcoesJson());
        File.WriteAllText(caminho, json, PadroesSerializacao.Utf8SemBom);
    }

    public static ReferenciaCognitiva? EncontrarReferencia(string termo, List<ReferenciaCognitiva> referencias)
    {
        var termoLower = termo.ToLowerInvariant();
        return referencias
            .Where(r => r.Conceito.Contains(termoLower, StringComparison.OrdinalIgnoreCase) ||
                        r.Atributos.Any(a => a.Contains(termoLower, StringComparison.OrdinalIgnoreCase)) ||
                        r.Exemplos.Any(e => e.Contains(termoLower, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.Peso)
            .ThenByDescending(r => r.FrequenciaUso)
            .FirstOrDefault();
    }

    public static List<ReferenciaCognitiva> EncontrarRelacionadas(string texto, List<ReferenciaCognitiva> referencias)
    {
        var textoLower = texto.ToLowerInvariant();
        return referencias
            .Where(r => textoLower.Contains(r.Conceito, StringComparison.OrdinalIgnoreCase) ||
                        r.Exemplos.Any(e => textoLower.Contains(e, StringComparison.OrdinalIgnoreCase)) ||
                        r.Atributos.Any(a => textoLower.Contains(a, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.Peso)
            .ThenByDescending(r => r.FrequenciaUso)
            .ToList();
    }

    public static string ClassificarEntidade(string texto, List<ReferenciaCognitiva> referencias)
    {
        var encontradas = EncontrarRelacionadas(texto, referencias);
        return encontradas.Count > 0 ? encontradas.First().Conceito : "geral";
    }

    public static string GerarDescricaoContextual(string texto, List<ReferenciaCognitiva> referencias)
    {
        var encontradas = EncontrarRelacionadas(texto, referencias);
        if (encontradas.Count == 0) return string.Empty;

        var partes = new List<string>();
        foreach (var ref_ in encontradas.Take(3))
        {
            ref_.FrequenciaUso++;
            ref_.UltimaAtivacaoBrasilSp = RelogioBrasil.AgoraBrasilSp();
            partes.Add(ref_.Descricao);
        }

        return string.Join(". ", partes);
    }
}
