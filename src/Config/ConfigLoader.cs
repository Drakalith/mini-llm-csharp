using System.Text.Json;
using MiniLlmDidatico.Arquivos;

namespace MiniLlmDidatico.Configuracao;

public static class CarregadorConfiguracao
{
    public static ConfiguracaoAplicacao Carregar(string caminho)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException($"Arquivo de configuracao nao encontrado: {caminho}");
        }

        var json = File.ReadAllText(caminho, PadroesSerializacao.Utf8SemBom);
        var opcoes = PadroesSerializacao.CriarOpcoesJson(
            writeIndented: true,
            propertyNameCaseInsensitive: true);

        return JsonSerializer.Deserialize<ConfiguracaoAplicacao>(json, opcoes)
               ?? throw new InvalidOperationException("Falha ao desserializar appsettings.json");
    }
}
