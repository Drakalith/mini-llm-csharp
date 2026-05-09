using MiniLlmDidatico.Cognicao;
using MiniLlmDidatico.Configuracao;

namespace MiniLlmDidatico.Cli;

public static class RenderizadorCortexConversacional
{
    public static void ExibirThink(EstadoCognitivoTurno? estado, ConfiguracaoCognicao configuracao)
    {
        if (!configuracao.Ativar ||
            estado is null ||
            !estado.Ativo ||
            string.Equals(configuracao.ModoThink, "oculto", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(configuracao.ModoThink, "desligado", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RenderizadorConsole.Secao("Think da Ôria");
        RenderizadorConsole.ChaveValor("Intencao", estado.Intencao, TomConsole.Info);
        RenderizadorConsole.ChaveValor("Prioridade", estado.Prioridade);
        RenderizadorConsole.ChaveValor("Fonte dominante", estado.FonteDominante, TomConsole.Info);
        RenderizadorConsole.ChaveValor("Confianca", estado.Confianca.ToString("0.00"), estado.Confianca >= 0.55 ? TomConsole.Sucesso : TomConsole.Aviso);

        if (estado.ReferenciasAtivas.Count > 0)
        {
            var referencias = string.Join(
                " | ",
                estado.ReferenciasAtivas
                    .Take(4)
                    .Select(x => $"{x.RotuloDebug}: {x.ValorAtual} ({x.Confianca:0.00})"));
            RenderizadorConsole.ChaveValor("Referencias", referencias, TomConsole.Muted);
        }
        else
        {
            RenderizadorConsole.ChaveValor("Referencias", "nenhuma referencia forte", TomConsole.Muted);
        }

        if (estado.Conflitos.Count > 0)
        {
            RenderizadorConsole.ChaveValor("Conflitos", string.Join(" | ", estado.Conflitos), TomConsole.Aviso);
        }

        RenderizadorConsole.ChaveValor("Decisao", estado.DecisaoResposta, TomConsole.Sucesso);
        RenderizadorConsole.LinhaEmBranco();
    }
}
