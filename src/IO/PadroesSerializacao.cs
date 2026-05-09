using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MiniLlmDidatico.Arquivos;

public static class PadroesSerializacao
{
    public static readonly UTF8Encoding Utf8SemBom = new(encoderShouldEmitUTF8Identifier: false);

    public static JsonSerializerOptions CriarOpcoesJson(
        bool writeIndented = true,
        bool propertyNameCaseInsensitive = false)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNameCaseInsensitive = propertyNameCaseInsensitive,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
