using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Configuracao;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Ferramentas;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class ObjetoReferenciaCognitiva
{
    public string Id { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string RotuloDebug { get; set; } = string.Empty;
    public string ValorAtual { get; set; } = string.Empty;
    public List<string> Aliases { get; init; } = new();
    public List<string> Evidencias { get; init; } = new();
    public double Confianca { get; set; }
    public int Forca { get; set; }
    public DateTimeOffset CriadoEmBrasilSp { get; set; }
    public DateTimeOffset UltimaAtualizacaoBrasilSp { get; set; }
}

public sealed class EpisodioExperiencial
{
    public DateTimeOffset MomentoBrasilSp { get; init; }
    public string EntradaNormalizada { get; init; } = string.Empty;
    public string IntencaoInferida { get; init; } = string.Empty;
    public List<string> ObjetosAtivos { get; init; } = new();
    public string AcaoEscolhida { get; init; } = string.Empty;
    public string RespostaFinal { get; init; } = string.Empty;
    public string FontePredominante { get; init; } = string.Empty;
    public string SinalQualidade { get; init; } = string.Empty;
}

public sealed class ReferenciaAtivaCognitiva
{
    public string Id { get; init; } = string.Empty;
    public string Familia { get; init; } = string.Empty;
    public string RotuloDebug { get; init; } = string.Empty;
    public string ValorAtual { get; init; } = string.Empty;
    public string Sujeito { get; init; } = string.Empty;
    public string PadraoRelacional { get; init; } = string.Empty;
    public string TipoValor { get; init; } = string.Empty;
    public string Unidade { get; init; } = string.Empty;
    public string Durabilidade { get; init; } = string.Empty;
    public List<string> Aliases { get; init; } = new();
    public double Confianca { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

public sealed class EstadoCognitivoTurno
{
    public bool Ativo { get; init; }
    public string Intencao { get; init; } = string.Empty;
    public string Prioridade { get; init; } = string.Empty;
    public string FonteDominante { get; init; } = string.Empty;
    public string DecisaoResposta { get; init; } = string.Empty;
    public string ContextoCompacto { get; init; } = string.Empty;
    public string ResumoThink { get; init; } = string.Empty;
    public double Confianca { get; init; }
    public List<ReferenciaAtivaCognitiva> ReferenciasAtivas { get; init; } = new();
    public List<string> Conflitos { get; init; } = new();
    public List<string> ObjetosAtualizados { get; init; } = new();
    public List<string> RelacoesAtualizadas { get; init; } = new();
}

public sealed class AtoConversacional
{
    public string Tipo { get; init; } = string.Empty;
    public string Acao { get; init; } = string.Empty;
    public string Alvo { get; init; } = string.Empty;
    public List<string> Focos { get; init; } = new();
    public bool EhPergunta { get; init; }
    public bool ConsultaMemoria { get; init; }
    public bool ConsultaPreferencia { get; init; }
    public bool ConsultaIdentidade { get; init; }
    public bool SolicitaAjuda { get; init; }
    public bool CorrigeRumo { get; init; }
}

public sealed class RevisaoRelacaoEmergente
{
    public DateTimeOffset MomentoBrasilSp { get; init; }
    public string ValorAnterior { get; init; } = string.Empty;
    public string ValorNovo { get; init; } = string.Empty;
    public string Evidencia { get; init; } = string.Empty;
}

public sealed class RelacaoEmergenteCognitiva
{
    public string Id { get; set; } = string.Empty;
    public string Sujeito { get; set; } = string.Empty;
    public string PadraoRelacional { get; set; } = string.Empty;
    public string RotuloProvisorio { get; set; } = string.Empty;
    public string ValorAtual { get; set; } = string.Empty;
    public string TipoValor { get; set; } = string.Empty;
    public string Unidade { get; set; } = string.Empty;
    public string Durabilidade { get; set; } = string.Empty;
    public List<string> Aliases { get; init; } = new();
    public List<string> Evidencias { get; init; } = new();
    public List<RevisaoRelacaoEmergente> Revisoes { get; init; } = new();
    public double Confianca { get; set; }
    public int Forca { get; set; }
    public DateTimeOffset CriadoEmBrasilSp { get; set; }
    public DateTimeOffset UltimaAtualizacaoBrasilSp { get; set; }
}

public sealed class EstadoMemoriaExperiencial
{
    public string VersaoFormato { get; set; } = "1";
    public DateTimeOffset CriadoEmBrasilSp { get; set; }
    public List<ObjetoReferenciaCognitiva> Objetos { get; init; } = new();
    public List<RelacaoEmergenteCognitiva> RelacoesEmergentes { get; init; } = new();
    public List<EpisodioExperiencial> Episodios { get; init; } = new();
}

public sealed class CortexConversacional
{
    private const string NomeArquivoMemoria = "14_memoria_experiencial.json";
    private const int MaximoObjetos = 256;
    private const int MaximoRelacoesEmergentes = 512;
    private const int MaximoEpisodios = 300;
    private const int MaximoEvidenciasPorObjeto = 8;
    private const int MaximoEvidenciasPorRelacao = 8;
    private const int MaximoRevisoesPorRelacao = 12;

    private static readonly HashSet<string> FormasPrimeiraPessoa = new(StringComparer.Ordinal)
    {
        "eu", "meu", "minha", "meus", "minhas", "mim", "me", "comigo"
    };

    private static readonly HashSet<string> FormasSegundaPessoa = new(StringComparer.Ordinal)
    {
        "vc", "voce", "você", "te", "tu", "seu", "sua", "seus", "suas", "contigo"
    };

    private static readonly HashSet<string> FormasModelo = new(StringComparer.Ordinal)
    {
        "oria", "ôria", "modelo", "robo", "robô"
    };

    private static readonly HashSet<string> FormasMemoria = new(StringComparer.Ordinal)
    {
        "lembra", "lembrar", "lembrou", "memoria", "memória", "disse", "falei", "contei", "pedi", "pedido", "mais"
    };

    private static readonly HashSet<string> FormasPreferencia = new(StringComparer.Ordinal)
    {
        "gosto", "gosta", "gostar", "prefiro", "prefere", "preferencia", "preferência"
    };

    private static readonly HashSet<string> FormasIdentidade = new(StringComparer.Ordinal)
    {
        "nome", "chamo", "chama", "identidade"
    };

    private static readonly HashSet<string> FormasAjuda = new(StringComparer.Ordinal)
    {
        "ajuda", "ajudar", "ajude", "auxilio", "auxílio"
    };

    private static readonly HashSet<string> FormasCorrecaoRumo = new(StringComparer.Ordinal)
    {
        "perguntei", "era", "foi", "isso", "oxi", "errado"
    };

    private static readonly JsonSerializerOptions OpcoesJson = PadroesSerializacao.CriarOpcoesJson(
        writeIndented: true,
        propertyNameCaseInsensitive: true);

    private readonly string _caminhoArquivo;
    private readonly ConfiguracaoCognicao _configuracao;
    private readonly EstadoMemoriaExperiencial _estado;

    private CortexConversacional(
        string caminhoArquivo,
        ConfiguracaoCognicao configuracao,
        EstadoMemoriaExperiencial estado)
    {
        _caminhoArquivo = caminhoArquivo;
        _configuracao = configuracao;
        _estado = estado;
    }

    public static CortexConversacional? CarregarOuCriar(string raizArtefatos, ConfiguracaoCognicao configuracao)
    {
        if (!configuracao.Ativar)
        {
            return null;
        }

        Directory.CreateDirectory(raizArtefatos);
        var caminhoArquivo = Path.Combine(raizArtefatos, NomeArquivoMemoria);
        var estado = CarregarEstado(caminhoArquivo);
        var cortex = new CortexConversacional(caminhoArquivo, configuracao.Clonar(), estado);
        cortex.PersistirSeNecessario();
        return cortex;
    }

    public EstadoCognitivoTurno PrepararTurno(
        AnalisePromptEpistemica analiseBase,
        ContextoMemoriaConversacional contextoConversacional,
        ContextoFerramentasConversacionais contextoFerramentas)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(analiseBase.EntradaOriginal);
        var formasAtivas = analiseBase.FormasAtivas
            .Select(TreinadorTokenizadorBpe.NormalizarTexto)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var atoTurno = AnalisarAtoConversacional(entradaNormalizada);
        var intencao = AjustarIntencaoPorAto(InferirIntencao(entradaNormalizada, analiseBase), atoTurno);
        var conflitos = new List<string>();
        var atualizados = new List<ObjetoReferenciaCognitiva>();
        var relacoesAtualizadas = new List<RelacaoEmergenteCognitiva>();

        foreach (var candidato in ExtrairCandidatosReferencia(entradaNormalizada))
        {
            atualizados.Add(AtualizarObjeto(candidato, entradaNormalizada, conflitos));
        }

        foreach (var proposicao in ExtrairProposicoesEmergentes(entradaNormalizada))
        {
            relacoesAtualizadas.Add(AtualizarRelacaoEmergente(proposicao, entradaNormalizada, conflitos));
        }

        var referencias = RecuperarReferencias(formasAtivas, entradaNormalizada, intencao, atualizados, relacoesAtualizadas)
            .Take(6)
            .ToList();

        if (referencias.Count == 0 &&
            analiseBase.ApoioNoCorpus < 0.35 &&
            !AtoPodeResponderSemReferencia(atoTurno))
        {
            conflitos.Add("baixa ancoragem no corpus e na memoria experiencial");
        }

        var fonteDominante = DeterminarFonteDominante(analiseBase, contextoConversacional, contextoFerramentas, referencias);
        var prioridade = DeterminarPrioridade(intencao, fonteDominante, conflitos, referencias);
        var decisao = DeterminarDecisao(intencao, fonteDominante, referencias);
        var contextoCompacto = MontarContextoCompacto(prioridade, referencias);
        var confianca = CalcularConfianca(analiseBase, referencias, conflitos, contextoFerramentas);

        var estadoTurno = new EstadoCognitivoTurno
        {
            Ativo = true,
            Intencao = intencao,
            Prioridade = prioridade,
            FonteDominante = fonteDominante,
            DecisaoResposta = decisao,
            ContextoCompacto = contextoCompacto,
            Confianca = confianca,
            ReferenciasAtivas = referencias,
            Conflitos = conflitos.Distinct(StringComparer.Ordinal).Take(4).ToList(),
            ObjetosAtualizados = atualizados.Select(x => x.Id).Distinct(StringComparer.Ordinal).ToList(),
            RelacoesAtualizadas = relacoesAtualizadas.Select(x => x.Id).Distinct(StringComparer.Ordinal).ToList(),
            ResumoThink = MontarResumoThink(intencao, prioridade, fonteDominante, decisao, confianca, referencias, conflitos)
        };

        PersistirSeNecessario();
        return estadoTurno;
    }

    public string TentarResponderComEstado(AnalisePromptEpistemica analisePrompt)
    {
        var estado = analisePrompt.EstadoCognitivo;
        if (estado is null || !estado.Ativo)
        {
            return string.Empty;
        }

        var entrada = TreinadorTokenizadorBpe.NormalizarTexto(analisePrompt.EntradaOriginal);
        var nomeUsuario = estado.ReferenciasAtivas
            .FirstOrDefault(x => x.Familia == "identidade_usuario" && x.Confianca >= 0.55)
            ?.ValorAtual;
        var preferenciaUsuario = estado.ReferenciasAtivas
            .FirstOrDefault(x => x.Familia == "preferencia_usuario" && x.Confianca >= 0.55);
        var preferenciasUsuario = estado.ReferenciasAtivas
            .Where(x => x.Familia == "preferencia_usuario" && x.Confianca >= 0.55)
            .ToList();
        var fatosHistorico = ExtrairFatosDoHistorico(analisePrompt);
        var ato = AnalisarAtoConversacional(entrada, estado);

        if (estado.Conflitos.Any(x => x.Contains("sem correcao explicita", StringComparison.Ordinal)))
        {
            return "isso conflita com uma referencia estavel que eu ja tinha. se for correcao, me diga explicitamente.";
        }

        var relacaoPerguntada = SelecionarRelacaoParaResposta(estado, entrada);

        if (ato.CorrigeRumo)
        {
            return "desculpa, entendi. você não estava perguntando isso. me diga no que quer ajuda.";
        }

        if (ato.SolicitaAjuda)
        {
            return "posso ajudar. me diga o que você precisa.";
        }

        if (ato.ConsultaPreferencia && ato.Alvo == "modelo")
        {
            return "eu gosto de conversar, aprender com o contexto e ajudar com calma.";
        }

        if (ato.ConsultaPreferencia && ato.Alvo == "usuario")
        {
            var respostaPreferencia = FormatarRespostaPreferencias(preferenciasUsuario, fatosHistorico);
            if (!string.IsNullOrWhiteSpace(respostaPreferencia))
            {
                return respostaPreferencia;
            }
        }

        if (ato.ConsultaMemoria && !ato.ConsultaIdentidade && !ato.ConsultaPreferencia)
        {
            var resumo = FormatarResumoMemoriaUsuario(estado, nomeUsuario, preferenciasUsuario, fatosHistorico);
            if (!string.IsNullOrWhiteSpace(resumo))
            {
                return resumo;
            }
        }

        if (ato.ConsultaIdentidade && ato.Alvo == "modelo")
        {
            var nomeModelo = string.IsNullOrWhiteSpace(analisePrompt.NomeIdentidade)
                ? "Ôria"
                : analisePrompt.NomeIdentidade;
            if (!string.IsNullOrWhiteSpace(nomeUsuario) &&
                estado.ObjetosAtualizados.Count > 0)
            {
                return $"meu nome é {TreinadorTokenizadorBpe.NormalizarTexto(nomeModelo)}. anotado, seu nome é {nomeUsuario}.";
            }

            return $"meu nome é {TreinadorTokenizadorBpe.NormalizarTexto(nomeModelo)}.";
        }

        if (ato.ConsultaIdentidade && ato.Alvo == "usuario" &&
            !string.IsNullOrWhiteSpace(nomeUsuario) &&
            relacaoPerguntada is not null &&
            PerguntaCompostaSobreNomeEReferencia(entrada, relacaoPerguntada))
        {
            return FormatarRespostaCompostaNomeRelacao(nomeUsuario, relacaoPerguntada);
        }

        if (ato.ConsultaIdentidade && ato.Alvo == "usuario" && !string.IsNullOrWhiteSpace(nomeUsuario))
        {
            var complementoHistorico = SelecionarComplementoHistoricoParaPerguntaComNome(entrada, fatosHistorico);
            if (!string.IsNullOrWhiteSpace(complementoHistorico))
            {
                return $"seu nome é {nomeUsuario} e {complementoHistorico}.";
            }
        }

        if (ato.ConsultaIdentidade && ato.Alvo == "usuario" && !string.IsNullOrWhiteSpace(nomeUsuario))
        {
            return $"seu nome é {nomeUsuario}.";
        }

        if (ato.ConsultaIdentidade && ato.Alvo == "usuario")
        {
            var nomeHistorico = fatosHistorico.FirstOrDefault(x => x.StartsWith("seu nome é", StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(nomeHistorico))
            {
                return $"{nomeHistorico}.";
            }

            return "ainda não sei seu nome. se quiser, diga: meu nome é ...";
        }

        if (relacaoPerguntada is not null && ato.Acao == "consultar_relacao")
        {
            return FormatarRespostaRelacaoEmergente(relacaoPerguntada);
        }

        if (ato.Acao == "consultar_relacao" || ato.ConsultaMemoria)
        {
            var respostaHistorico = SelecionarRespostaHistoricoParaPergunta(entrada, fatosHistorico);
            if (!string.IsNullOrWhiteSpace(respostaHistorico))
            {
                return $"{respostaHistorico}.";
            }
        }

        if (estado.Intencao is "informar identidade do usuario" or "informar preferencia" or "informar relacao emergente" &&
            (estado.ObjetosAtualizados.Count > 0 || estado.RelacoesAtualizadas.Count > 0))
        {
            return FormatarConfirmacaoMemoria(estado, nomeUsuario, preferenciasUsuario, entrada);
        }

        if (estado.Intencao == "informar preferencia")
        {
            return "entendido, anotado aqui.";
        }

        return string.Empty;
    }

    public static bool DevePriorizarRespostaCognitiva(
        AnalisePromptEpistemica analisePrompt,
        string respostaCognitiva)
    {
        if (string.IsNullOrWhiteSpace(respostaCognitiva) ||
            GestorMemoriaConversacional.TextoEstaDegradado(respostaCognitiva))
        {
            return false;
        }

        var estado = analisePrompt.EstadoCognitivo;
        if (estado is null || !estado.Ativo)
        {
            return false;
        }

        return estado.FonteDominante == "memoria experiencial" ||
               estado.Intencao is "informar identidade do usuario" or "informar preferencia" or "informar relacao emergente" ||
               estado.DecisaoResposta.Contains("memoria experiencial", StringComparison.Ordinal);
    }

    public void RegistrarResultadoTurno(
        EstadoCognitivoTurno? estadoTurno,
        string entradaUsuario,
        string respostaFinal,
        string fontePredominante,
        string sinalQualidade)
    {
        if (estadoTurno is null || !estadoTurno.Ativo)
        {
            return;
        }

        if (!string.Equals(sinalQualidade, "boa", StringComparison.Ordinal))
        {
            return;
        }

        var agora = RelogioBrasil.AgoraBrasilSp();
        _estado.Episodios.Add(new EpisodioExperiencial
        {
            MomentoBrasilSp = agora,
            EntradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario),
            IntencaoInferida = estadoTurno.Intencao,
            ObjetosAtivos = estadoTurno.ReferenciasAtivas.Select(x => x.Id).Distinct(StringComparer.Ordinal).ToList(),
            AcaoEscolhida = estadoTurno.DecisaoResposta,
            RespostaFinal = TreinadorTokenizadorBpe.NormalizarTexto(respostaFinal),
            FontePredominante = fontePredominante,
            SinalQualidade = sinalQualidade
        });

        if (_estado.Episodios.Count > MaximoEpisodios)
        {
            _estado.Episodios.RemoveRange(0, _estado.Episodios.Count - MaximoEpisodios);
        }

        foreach (var referencia in estadoTurno.ReferenciasAtivas)
        {
            var objeto = _estado.Objetos.FirstOrDefault(x => x.Id == referencia.Id);
            if (objeto is not null)
            {
                objeto.Confianca = Math.Min(0.98, objeto.Confianca + 0.03);
                objeto.UltimaAtualizacaoBrasilSp = agora;
            }

            var relacao = _estado.RelacoesEmergentes.FirstOrDefault(x => x.Id == referencia.Id);
            if (relacao is not null)
            {
                relacao.Confianca = Math.Min(0.98, relacao.Confianca + 0.03);
                relacao.UltimaAtualizacaoBrasilSp = agora;
            }
        }

        PersistirSeNecessario();
    }

    private static EstadoMemoriaExperiencial CarregarEstado(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
        {
            return new EstadoMemoriaExperiencial
            {
                CriadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp()
            };
        }

        try
        {
            var json = File.ReadAllText(caminhoArquivo, PadroesSerializacao.Utf8SemBom);
            var estado = JsonSerializer.Deserialize<EstadoMemoriaExperiencial>(json, OpcoesJson)
                         ?? new EstadoMemoriaExperiencial();
            estado.VersaoFormato = "1";
            if (estado.CriadoEmBrasilSp == default)
            {
                estado.CriadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp();
            }

            SanitizarEstado(estado);
            return estado;
        }
        catch
        {
            return new EstadoMemoriaExperiencial
            {
                CriadoEmBrasilSp = RelogioBrasil.AgoraBrasilSp()
            };
        }
    }

    private static void SanitizarEstado(EstadoMemoriaExperiencial estado)
    {
        var objetosValidos = estado.Objetos
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => x.OrderByDescending(o => o.UltimaAtualizacaoBrasilSp).First())
            .TakeLast(MaximoObjetos)
            .ToList();

        estado.Objetos.Clear();
        estado.Objetos.AddRange(objetosValidos);

        var relacoesValidas = estado.RelacoesEmergentes
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => x.OrderByDescending(r => r.UltimaAtualizacaoBrasilSp).First())
            .TakeLast(MaximoRelacoesEmergentes)
            .ToList();

        estado.RelacoesEmergentes.Clear();
        estado.RelacoesEmergentes.AddRange(relacoesValidas);

        var episodiosValidos = estado.Episodios
            .Where(x => !string.IsNullOrWhiteSpace(x.EntradaNormalizada))
            .TakeLast(MaximoEpisodios)
            .ToList();

        estado.Episodios.Clear();
        estado.Episodios.AddRange(episodiosValidos);
    }

    private IEnumerable<CandidatoReferencia> ExtrairCandidatosReferencia(string entradaNormalizada)
    {
        foreach (var candidato in ExtrairIdentidadeUsuario(entradaNormalizada))
        {
            yield return candidato;
        }

        foreach (var candidato in ExtrairPreferencias(entradaNormalizada))
        {
            yield return candidato;
        }

        foreach (var candidato in ExtrairReferenciasProjeto(entradaNormalizada))
        {
            yield return candidato;
        }

        foreach (var candidato in ExtrairReferenciasArquivo(entradaNormalizada))
        {
            yield return candidato;
        }
    }

    private static IEnumerable<CandidatoReferencia> ExtrairIdentidadeUsuario(string entradaNormalizada)
    {
        if (ParecePergunta(entradaNormalizada) &&
            !TemComandoMemoria(entradaNormalizada) &&
            !TemMarcadorCorrecao(entradaNormalizada) &&
            !ContemDeclaracaoIdentidadeUsuario(entradaNormalizada))
        {
            yield break;
        }

        string[] padroes =
        [
            @"(?:nao|não).*meu nome.*(?:é|e)\s+(?<valor>[\p{L}\p{N}_-]+)",
            @"(?:^|[.!?]\s*)(?:na verdade\s+|corrige\s+|corrija\s+|atualize\s+|atualiza\s+)?meu nome\s+(?:é|e)\s+(?<valor>[\p{L}\p{N}_-]+)",
            @"(?:eu\s+)?me chamo\s+(?<valor>[\p{L}\p{N}_-]+)",
            @"pode me chamar de\s+(?<valor>[\p{L}\p{N}_-]+)"
        ];

        foreach (var padrao in padroes)
        {
            var match = Regex.Match(entradaNormalizada, padrao, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                continue;
            }

            var valor = LimparValorCurto(match.Groups["valor"].Value);
            if (ValorReservado(valor))
            {
                continue;
            }

            yield return new CandidatoReferencia(
                "identidade_usuario",
                "nome do usuario",
                valor,
                ["usuario", "nome", valor],
                0.86);
            yield break;
        }
    }

    private static IEnumerable<CandidatoReferencia> ExtrairPreferencias(string entradaNormalizada)
    {
        if (Regex.IsMatch(entradaNormalizada, @"\b(?:disse|falei|contei)\b.*\bdo que\b.*\bgosto\b", RegexOptions.CultureInvariant))
        {
            yield break;
        }

        var match = Regex.Match(
            entradaNormalizada,
            @"\b(?:eu\s+)?(?<verbo>prefiro|gosto|nao gosto|não gosto)\s+(?<valor>.{4,90})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            yield break;
        }

        var valor = LimparFraseCurta(match.Groups["valor"].Value);
        if (string.IsNullOrWhiteSpace(valor) || valor.EndsWith("?", StringComparison.Ordinal))
        {
            yield break;
        }

        var verbo = TreinadorTokenizadorBpe.NormalizarTexto(match.Groups["verbo"].Value);
        var preferencia = $"{verbo} {valor}";
        var rotulo = CriarRotuloPreferenciaUsuario(preferencia);
        yield return new CandidatoReferencia(
            "preferencia_usuario",
            rotulo,
            preferencia,
            AliasesEmergentes(preferencia, "usuario", "preferencia", "prefiro", "gosto"),
            0.68);
    }

    private static IEnumerable<CandidatoReferencia> ExtrairReferenciasProjeto(string entradaNormalizada)
    {
        string[] marcadores = ["oria", "ôria", "mini-llm", "corpus", "cortex", "cerebro", "cérebro"];
        foreach (var marcador in marcadores)
        {
            if (!entradaNormalizada.Contains(marcador, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new CandidatoReferencia(
                "projeto",
                "projeto em foco",
                marcador,
                ["projeto", marcador],
                0.55);
            yield break;
        }
    }

    private static IEnumerable<CandidatoReferencia> ExtrairReferenciasArquivo(string entradaNormalizada)
    {
        var match = Regex.Match(
            entradaNormalizada,
            @"[a-z]:\\[^\r\n]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            yield break;
        }

        var valor = match.Value.Trim();
        yield return new CandidatoReferencia(
            "arquivo",
            "arquivo referenciado",
            valor,
            ["arquivo", Path.GetFileName(valor)],
            0.72);
    }

    private ObjetoReferenciaCognitiva AtualizarObjeto(
        CandidatoReferencia candidato,
        string entradaNormalizada,
        List<string> conflitos)
    {
        var agora = RelogioBrasil.AgoraBrasilSp();
        var id = CriarIdReferencia(candidato.Familia, candidato.RotuloDebug);
        var existente = _estado.Objetos.FirstOrDefault(x => x.Id == id);
        if (existente is null)
        {
            existente = new ObjetoReferenciaCognitiva
            {
                Id = id,
                Familia = candidato.Familia,
                RotuloDebug = candidato.RotuloDebug,
                ValorAtual = candidato.Valor,
                Confianca = candidato.ConfiancaInicial,
                Forca = 1,
                CriadoEmBrasilSp = agora,
                UltimaAtualizacaoBrasilSp = agora
            };
            _estado.Objetos.Add(existente);
        }
        else
        {
            if (!string.Equals(existente.ValorAtual, candidato.Valor, StringComparison.Ordinal))
            {
                if (ReferenciaObjetoProtegida(existente) &&
                    !TemMarcadorCorrecao(entradaNormalizada))
                {
                    conflitos.Add($"{existente.RotuloDebug} recebeu valor diferente sem correcao explicita; valor preservado");
                    existente.Evidencias.Add($"conflito nao aplicado : {entradaNormalizada}");
                    while (existente.Evidencias.Count > MaximoEvidenciasPorObjeto)
                    {
                        existente.Evidencias.RemoveAt(0);
                    }

                    existente.UltimaAtualizacaoBrasilSp = agora;
                    return existente;
                }

                conflitos.Add($"{existente.RotuloDebug} revisado de {existente.ValorAtual} para {candidato.Valor}");
            }

            existente.ValorAtual = candidato.Valor;
            existente.Confianca = Math.Min(0.96, Math.Max(existente.Confianca, candidato.ConfiancaInicial) + 0.04);
            existente.Forca++;
            existente.UltimaAtualizacaoBrasilSp = agora;
        }

        foreach (var alias in candidato.Aliases.Select(TreinadorTokenizadorBpe.NormalizarTexto))
        {
            if (!string.IsNullOrWhiteSpace(alias) &&
                !existente.Aliases.Contains(alias, StringComparer.Ordinal))
            {
                existente.Aliases.Add(alias);
            }
        }

        var evidencia = $"usuario disse : {entradaNormalizada}";
        existente.Evidencias.Add(evidencia);
        while (existente.Evidencias.Count > MaximoEvidenciasPorObjeto)
        {
            existente.Evidencias.RemoveAt(0);
        }

        if (_estado.Objetos.Count > MaximoObjetos)
        {
            var remover = _estado.Objetos
                .OrderBy(x => x.Forca)
                .ThenBy(x => x.UltimaAtualizacaoBrasilSp)
                .Take(_estado.Objetos.Count - MaximoObjetos)
                .ToList();
            foreach (var item in remover)
            {
                _estado.Objetos.Remove(item);
            }
        }

        return existente;
    }

    private RelacaoEmergenteCognitiva AtualizarRelacaoEmergente(
        CandidatoRelacaoEmergente candidato,
        string entradaNormalizada,
        List<string> conflitos)
    {
        var agora = RelogioBrasil.AgoraBrasilSp();
        var id = CriarIdRelacaoEmergente(candidato.Sujeito, candidato.PadraoRelacional);
        var existente = _estado.RelacoesEmergentes.FirstOrDefault(x => x.Id == id)
                        ?? EncontrarRelacaoEmergenteSimilar(candidato);

        if (existente is null)
        {
            existente = new RelacaoEmergenteCognitiva
            {
                Id = id,
                Sujeito = candidato.Sujeito,
                PadraoRelacional = candidato.PadraoRelacional,
                RotuloProvisorio = candidato.RotuloProvisorio,
                ValorAtual = candidato.Valor,
                TipoValor = candidato.TipoValor,
                Unidade = candidato.Unidade,
                Durabilidade = candidato.Durabilidade,
                Confianca = candidato.ConfiancaInicial,
                Forca = 1,
                CriadoEmBrasilSp = agora,
                UltimaAtualizacaoBrasilSp = agora
            };
            _estado.RelacoesEmergentes.Add(existente);
        }
        else
        {
            if (!ValoresEquivalentes(existente.ValorAtual, candidato.Valor))
            {
                if (RelacaoEmergenteProtegida(existente) &&
                    !TemMarcadorCorrecao(entradaNormalizada))
                {
                    conflitos.Add($"{existente.RotuloProvisorio} recebeu valor diferente sem correcao explicita; valor preservado");
                    existente.Evidencias.Add($"conflito nao aplicado : {entradaNormalizada}");
                    while (existente.Evidencias.Count > MaximoEvidenciasPorRelacao)
                    {
                        existente.Evidencias.RemoveAt(0);
                    }

                    existente.UltimaAtualizacaoBrasilSp = agora;
                    return existente;
                }

                existente.Revisoes.Add(new RevisaoRelacaoEmergente
                {
                    MomentoBrasilSp = agora,
                    ValorAnterior = existente.ValorAtual,
                    ValorNovo = candidato.Valor,
                    Evidencia = entradaNormalizada
                });
                while (existente.Revisoes.Count > MaximoRevisoesPorRelacao)
                {
                    existente.Revisoes.RemoveAt(0);
                }

                conflitos.Add($"{existente.RotuloProvisorio} revisado de {existente.ValorAtual} para {candidato.Valor}");
            }

            existente.ValorAtual = candidato.Valor;
            existente.PadraoRelacional = candidato.PadraoRelacional;
            existente.RotuloProvisorio = candidato.RotuloProvisorio;
            existente.TipoValor = string.IsNullOrWhiteSpace(candidato.TipoValor) ? existente.TipoValor : candidato.TipoValor;
            existente.Unidade = string.IsNullOrWhiteSpace(candidato.Unidade) ? existente.Unidade : candidato.Unidade;
            existente.Durabilidade = candidato.Durabilidade;
            existente.Confianca = Math.Min(0.97, Math.Max(existente.Confianca, candidato.ConfiancaInicial) + 0.04);
            existente.Forca++;
            existente.UltimaAtualizacaoBrasilSp = agora;
        }

        foreach (var alias in candidato.Aliases.Select(TreinadorTokenizadorBpe.NormalizarTexto))
        {
            if (!string.IsNullOrWhiteSpace(alias) &&
                !existente.Aliases.Contains(alias, StringComparer.Ordinal))
            {
                existente.Aliases.Add(alias);
            }
        }

        var evidencia = $"usuario disse : {entradaNormalizada}";
        existente.Evidencias.Add(evidencia);
        while (existente.Evidencias.Count > MaximoEvidenciasPorRelacao)
        {
            existente.Evidencias.RemoveAt(0);
        }

        if (_estado.RelacoesEmergentes.Count > MaximoRelacoesEmergentes)
        {
            var remover = _estado.RelacoesEmergentes
                .OrderBy(x => x.Forca)
                .ThenBy(x => x.UltimaAtualizacaoBrasilSp)
                .Take(_estado.RelacoesEmergentes.Count - MaximoRelacoesEmergentes)
                .ToList();
            foreach (var item in remover)
            {
                _estado.RelacoesEmergentes.Remove(item);
            }
        }

        return existente;
    }

    private RelacaoEmergenteCognitiva? EncontrarRelacaoEmergenteSimilar(CandidatoRelacaoEmergente candidato)
    {
        var aliasesCandidato = candidato.Aliases
            .SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo)
            .Where(x => !AliasRelacionalGenerico(x))
            .ToHashSet(StringComparer.Ordinal);

        return _estado.RelacoesEmergentes
            .Where(x => x.Sujeito == candidato.Sujeito)
            .Select(relacao => new
            {
                Relacao = relacao,
                Pontos = PontuarSimilaridadeRelacao(relacao, candidato, aliasesCandidato)
            })
            .Where(x => x.Pontos >= 32)
            .OrderByDescending(x => x.Pontos)
            .ThenByDescending(x => x.Relacao.UltimaAtualizacaoBrasilSp)
            .Select(x => x.Relacao)
            .FirstOrDefault();
    }

    private IReadOnlyList<ReferenciaAtivaCognitiva> RecuperarReferencias(
        HashSet<string> formasAtivas,
        string entradaNormalizada,
        string intencao,
        IReadOnlyList<ObjetoReferenciaCognitiva> objetosAtualizados,
        IReadOnlyList<RelacaoEmergenteCognitiva> relacoesAtualizadas)
    {
        var objetosAtualizadosIds = objetosAtualizados.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var relacoesAtualizadasIds = relacoesAtualizadas.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var ato = AnalisarAtoConversacional(entradaNormalizada);
        var perguntaGenericaMemoria = ato.ConsultaMemoria && ato.Alvo == "usuario";
        var perguntaSobreModelo = ato.Alvo == "modelo" && (ato.ConsultaIdentidade || ato.ConsultaPreferencia);

        var referenciasObjetos = _estado.Objetos
            .Select(objeto => new
            {
                Referencia = new ReferenciaAtivaCognitiva
                {
                    Id = objeto.Id,
                    Familia = objeto.Familia,
                    RotuloDebug = objeto.RotuloDebug,
                    ValorAtual = objeto.ValorAtual,
                    Aliases = objeto.Aliases.ToList(),
                    Confianca = objeto.Confianca,
                    Motivo = objetosAtualizadosIds.Contains(objeto.Id) ? "atualizada neste turno" : "recuperada da memoria experiencial"
                },
                Pontuacao = PontuarReferencia(objeto, formasAtivas, intencao, objetosAtualizadosIds.Contains(objeto.Id)) +
                            (perguntaGenericaMemoria && ObjetoSobreUsuario(objeto) ? 72 : 0),
                Atualizacao = objeto.UltimaAtualizacaoBrasilSp
            });

        var referenciasRelacoes = _estado.RelacoesEmergentes
            .Select(relacao => new
            {
                Referencia = new ReferenciaAtivaCognitiva
                {
                    Id = relacao.Id,
                    Familia = "relacao_emergente",
                    RotuloDebug = relacao.RotuloProvisorio,
                    ValorAtual = relacao.ValorAtual,
                    Sujeito = relacao.Sujeito,
                    PadraoRelacional = relacao.PadraoRelacional,
                    TipoValor = relacao.TipoValor,
                    Unidade = relacao.Unidade,
                    Durabilidade = relacao.Durabilidade,
                    Aliases = AliasesReferenciaRelacao(relacao),
                    Confianca = relacao.Confianca,
                    Motivo = relacoesAtualizadasIds.Contains(relacao.Id)
                        ? "relacao atualizada neste turno"
                        : "relacao recuperada da memoria emergente"
                },
                Pontuacao = PontuarRelacaoEmergente(relacao, formasAtivas, intencao, relacoesAtualizadasIds.Contains(relacao.Id)) +
                            (perguntaGenericaMemoria && relacao.Sujeito == "usuario" ? 72 : 0),
                Atualizacao = relacao.UltimaAtualizacaoBrasilSp
            });

        return referenciasObjetos
            .Concat(referenciasRelacoes)
            .Where(x => x.Pontuacao > 0)
            .Where(x => !perguntaSobreModelo || !ReferenciaSobreUsuario(x.Referencia) || objetosAtualizadosIds.Contains(x.Referencia.Id))
            .OrderByDescending(x => x.Pontuacao)
            .ThenByDescending(x => x.Atualizacao)
            .Select(x => x.Referencia)
            .ToList();
    }

    private static int PontuarReferencia(
        ObjetoReferenciaCognitiva objeto,
        HashSet<string> formasAtivas,
        string intencao,
        bool atualizadaNesteTurno)
    {
        var pontuacao = atualizadaNesteTurno ? 100 : 0;
        var formasObjeto = objeto.Aliases
            .Append(objeto.RotuloDebug)
            .Append(objeto.ValorAtual)
            .SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo)
            .ToHashSet(StringComparer.Ordinal);

        var intersecoes = formasObjeto.Count(formasAtivas.Contains);
        var bonusIntencao = 0;

        if (intencao.Contains("identidade", StringComparison.Ordinal) &&
            objeto.Familia == "identidade_usuario")
        {
            bonusIntencao += 45;
        }

        if (intencao.Contains("preferencia", StringComparison.Ordinal) &&
            objeto.Familia == "preferencia_usuario")
        {
            bonusIntencao += 40;
        }

        if (intencao.Contains("arquivo", StringComparison.Ordinal) &&
            objeto.Familia == "arquivo")
        {
            bonusIntencao += 35;
        }

        if (formasAtivas.Contains("nome") &&
            objeto.Familia == "identidade_usuario")
        {
            bonusIntencao += 35;
        }

        if (!atualizadaNesteTurno && intersecoes == 0 && bonusIntencao == 0)
        {
            return 0;
        }

        pontuacao += intersecoes * 18;
        pontuacao += bonusIntencao;
        pontuacao += Math.Min(30, objeto.Forca * 3);
        pontuacao += (int)Math.Round(objeto.Confianca * 20);
        return pontuacao;
    }

    private static int PontuarRelacaoEmergente(
        RelacaoEmergenteCognitiva relacao,
        HashSet<string> formasAtivas,
        string intencao,
        bool atualizadaNesteTurno)
    {
        var pontuacao = atualizadaNesteTurno ? 105 : 0;
        var formasRelacao = FormasRelacaoEmergente(relacao)
            .Where(x => !AliasRelacionalGenerico(x))
            .ToHashSet(StringComparer.Ordinal);
        var intersecoes = formasRelacao.Count(formasAtivas.Contains);

        if (!atualizadaNesteTurno &&
            intersecoes == 0 &&
            !intencao.Contains("relacao emergente", StringComparison.Ordinal))
        {
            return 0;
        }

        pontuacao += intersecoes * 24;
        pontuacao += Math.Min(34, relacao.Forca * 3);
        pontuacao += (int)Math.Round(relacao.Confianca * 22);

        if (intencao.Contains("relacao emergente", StringComparison.Ordinal))
        {
            pontuacao += 36;
        }

        if (intencao == "perguntar" && formasRelacao.Overlaps(formasAtivas))
        {
            pontuacao += 42;
        }

        if (relacao.Sujeito == "usuario" &&
            (formasAtivas.Contains("meu") || formasAtivas.Contains("minha") || formasAtivas.Contains("eu")))
        {
            pontuacao += 12;
        }

        return pontuacao;
    }

    private static int PontuarSimilaridadeRelacao(
        RelacaoEmergenteCognitiva relacao,
        CandidatoRelacaoEmergente candidato,
        HashSet<string> aliasesCandidato)
    {
        var pontuacao = 0;
        if (relacao.Sujeito == candidato.Sujeito)
        {
            pontuacao += 12;
        }

        if (!string.IsNullOrWhiteSpace(relacao.Unidade) &&
            relacao.Unidade == candidato.Unidade)
        {
            pontuacao += 34;
        }

        if (!string.IsNullOrWhiteSpace(relacao.TipoValor) &&
            relacao.TipoValor == candidato.TipoValor)
        {
            pontuacao += 8;
        }

        var aliasesRelacao = FormasRelacaoEmergente(relacao)
            .Where(x => !AliasRelacionalGenerico(x))
            .ToHashSet(StringComparer.Ordinal);
        pontuacao += aliasesRelacao.Count(aliasesCandidato.Contains) * 18;

        if (relacao.PadraoRelacional == candidato.PadraoRelacional)
        {
            pontuacao += 40;
        }

        return pontuacao;
    }

    private static string InferirIntencao(string entradaNormalizada, AnalisePromptEpistemica analiseBase)
    {
        if (ContemDeclaracaoIdentidadeUsuario(entradaNormalizada))
        {
            return "informar identidade do usuario";
        }

        if (ExtrairPreferencias(entradaNormalizada).Any())
        {
            return "informar preferencia";
        }

        if (ExtrairProposicoesEmergentes(entradaNormalizada).Any())
        {
            return "informar relacao emergente";
        }

        if (Regex.IsMatch(entradaNormalizada, @"[a-z]:\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "referenciar arquivo";
        }

        if (Regex.IsMatch(entradaNormalizada, @"\b(implemente|faça|faca|crie|mexa|corrija|atualize)\b|\bquero que\b", RegexOptions.CultureInvariant))
        {
            return "pedir execucao";
        }

        if (Regex.IsMatch(entradaNormalizada, @"\b(como podemos|arquitetura|sistema|cortex|memoria|memória)\b", RegexOptions.CultureInvariant))
        {
            return "discutir arquitetura";
        }

        return analiseBase.TipoPragmatica == "pergunta"
            ? "perguntar"
            : analiseBase.TipoPragmatica;
    }

    private static string AjustarIntencaoPorAto(string intencaoBase, AtoConversacional ato)
    {
        if (ato.CorrigeRumo)
        {
            return "corrigir rumo conversacional";
        }

        if (ato.SolicitaAjuda)
        {
            return "pedir ajuda";
        }

        if (ato.ConsultaPreferencia)
        {
            return ato.Alvo == "modelo"
                ? "perguntar preferencia da oria"
                : "perguntar preferencia do usuario";
        }

        if (ato.ConsultaIdentidade)
        {
            return ato.Alvo == "modelo"
                ? "perguntar identidade da oria"
                : "perguntar identidade do usuario";
        }

        if (ato.Acao == "consultar_relacao")
        {
            return "consultar referencia do usuario";
        }

        if (ato.ConsultaMemoria)
        {
            return "consultar memoria do usuario";
        }

        return intencaoBase;
    }

    private static bool AtoPodeResponderSemReferencia(AtoConversacional ato)
    {
        return ato.CorrigeRumo ||
               ato.SolicitaAjuda ||
               (ato.Alvo == "modelo" && (ato.ConsultaIdentidade || ato.ConsultaPreferencia));
    }

    private static string DeterminarFonteDominante(
        AnalisePromptEpistemica analiseBase,
        ContextoMemoriaConversacional contextoConversacional,
        ContextoFerramentasConversacionais contextoFerramentas,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias)
    {
        if (SintetizadorRespostaContextual.TemEvidenciaObservacionalForte(
                contextoFerramentas.Blocos.Select(x => $"{x.Rotulo}: {x.Texto}").ToList()))
        {
            return "ferramentas";
        }

        if (referencias.Any(x => x.Confianca >= 0.62))
        {
            return "memoria experiencial";
        }

        if (analiseBase.TipoApoioPredominante == "pergunta-resposta" &&
            analiseBase.ApoioNoCorpus >= 0.95)
        {
            return "corpus pergunta-resposta";
        }

        if (contextoConversacional.MemoriaCurta.Count > 0 ||
            contextoConversacional.MemoriaMedia.Count > 0 ||
            contextoConversacional.MemoriaLonga.Count > 0)
        {
            return "memoria conversacional";
        }

        return "geracao neural";
    }

    private static string DeterminarPrioridade(
        string intencao,
        string fonteDominante,
        IReadOnlyList<string> conflitos,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias)
    {
        if (fonteDominante == "ferramentas")
        {
            return "usar evidencia observada antes de gerar";
        }

        if (conflitos.Count > 0)
        {
            return "resolver conflito de referencia antes da resposta";
        }

        if (intencao == "informar identidade do usuario")
        {
            return "registrar identidade e responder pergunta";
        }

        if (intencao == "informar preferencia")
        {
            return "registrar preferencia do usuario";
        }

        if (intencao == "informar relacao emergente")
        {
            return "registrar proposicao emergente como referencia revisavel";
        }

        if (referencias.Count > 0)
        {
            return "usar referencias recuperadas sem alongar o prompt";
        }

        return "responder diretamente com cuidado";
    }

    private static string DeterminarDecisao(
        string intencao,
        string fonteDominante,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias)
    {
        if (fonteDominante == "memoria experiencial" &&
            referencias.Count > 0)
        {
            return "responder com memoria experiencial e contexto compacto";
        }

        if (fonteDominante == "ferramentas")
        {
            return "priorizar resposta baseada em evidencia observavel";
        }

        if (intencao == "pedir execucao")
        {
            return "preservar pedido do usuario e evitar inventar estado";
        }

        return "gerar resposta neural com verificacao de degradacao";
    }

    private static double CalcularConfianca(
        AnalisePromptEpistemica analiseBase,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias,
        IReadOnlyList<string> conflitos,
        ContextoFerramentasConversacionais contextoFerramentas)
    {
        var confianca = 0.35 + Math.Min(0.35, analiseBase.ApoioNoCorpus * 0.25);
        if (referencias.Count > 0)
        {
            confianca += Math.Min(0.35, referencias.Average(x => x.Confianca) * 0.35);
        }

        if (contextoFerramentas.Blocos.Count > 0)
        {
            confianca += 0.08;
        }

        confianca -= conflitos.Count * 0.08;
        return Math.Clamp(confianca, 0.05, 0.98);
    }

    private static string MontarContextoCompacto(
        string prioridade,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias)
    {
        var partes = new List<string>();
        foreach (var referencia in referencias.Where(x => x.Confianca >= 0.55).Take(3))
        {
            if (referencia.Familia == "identidade_usuario")
            {
                partes.Add($"usuario se chama {referencia.ValorAtual}");
            }
            else if (referencia.Familia == "preferencia_usuario")
            {
                partes.Add($"preferencia do usuario {referencia.ValorAtual}");
            }
            else if (referencia.Familia == "projeto")
            {
                partes.Add($"projeto em foco {referencia.ValorAtual}");
            }
            else if (referencia.Familia == "arquivo")
            {
                partes.Add($"arquivo citado {Path.GetFileName(referencia.ValorAtual)}");
            }
            else if (referencia.Familia == "relacao_emergente")
            {
                partes.Add(FormatarContextoRelacaoEmergente(referencia));
            }
        }

        if (partes.Count == 0)
        {
            return string.Empty;
        }

        partes.Add($"prioridade {prioridade}");
        return $"contexto cognitivo : {string.Join(" . ", partes)} .";
    }

    private static string MontarResumoThink(
        string intencao,
        string prioridade,
        string fonteDominante,
        string decisao,
        double confianca,
        IReadOnlyList<ReferenciaAtivaCognitiva> referencias,
        IReadOnlyList<string> conflitos)
    {
        var refs = referencias.Count == 0
            ? "nenhuma referencia forte"
            : string.Join(", ", referencias.Take(3).Select(x => $"{x.RotuloDebug}={x.ValorAtual} ({x.Confianca:0.00})"));
        var conflitosTexto = conflitos.Count == 0
            ? "sem conflito relevante"
            : string.Join(" | ", conflitos.Take(3));

        return $"intencao={intencao}; prioridade={prioridade}; fonte={fonteDominante}; refs={refs}; conflitos={conflitosTexto}; confianca={confianca:0.00}; decisao={decisao}";
    }

    private static IEnumerable<CandidatoRelacaoEmergente> ExtrairProposicoesEmergentes(string entradaNormalizada)
    {
        var entrada = RemoverComandoMemoria(entradaNormalizada);
        if (string.IsNullOrWhiteSpace(entrada))
        {
            yield break;
        }

        var comandoMemoria = TemComandoMemoria(entradaNormalizada);
        var perguntaSemComando = ParecePergunta(entrada) && !comandoMemoria;
        if (perguntaSemComando ||
            Regex.IsMatch(entrada, @"\b(meu nome|me chamo|pode me chamar)\b", RegexOptions.CultureInvariant))
        {
            yield break;
        }

        foreach (Match match in Regex.Matches(
                     entrada,
                     @"\b(?:eu\s+)?tenho\s+(?<valor>\d+(?:[,.]\d+)?)\s*(?<unidade>[\p{L}]+(?:\s+de\s+[\p{L}]+)?)?",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (MatchEstaNegado(entrada, match.Index))
            {
                continue;
            }

            var valor = LimparValorCurto(match.Groups["valor"].Value);
            var unidade = NormalizarUnidade(match.Groups["unidade"].Value);
            if (string.IsNullOrWhiteSpace(unidade) &&
                Regex.IsMatch(entrada, @"\banos?\b", RegexOptions.CultureInvariant))
            {
                unidade = "anos";
            }

            var padrao = unidade == "anos" ? "tenho anos" : "tenho valor";
            yield return CriarCandidatoRelacaoEmergente(
                sujeito: "usuario",
                padraoRelacional: padrao,
                rotuloProvisorio: string.IsNullOrWhiteSpace(unidade) ? "eu tenho" : $"eu tenho {unidade}",
                valor: valor,
                unidade: unidade,
                durabilidade: "duravel",
                aliasesBase: AliasesEmergentes(entrada, "usuario", "eu", "tenho", unidade, valor));
        }

        foreach (Match match in Regex.Matches(
                     entrada,
                     @"\b(?:fiz|completei)\s+(?<valor>\d+(?:[,.]\d+)?)\s*(?<unidade>anos?)?",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var valor = LimparValorCurto(match.Groups["valor"].Value);
            var unidade = NormalizarUnidade(match.Groups["unidade"].Value);
            if (string.IsNullOrWhiteSpace(unidade))
            {
                unidade = "anos";
            }

            yield return CriarCandidatoRelacaoEmergente(
                sujeito: "usuario",
                padraoRelacional: "tenho anos",
                rotuloProvisorio: "eu fiz anos",
                valor: valor,
                unidade: unidade,
                durabilidade: "duravel",
                aliasesBase: AliasesEmergentes(entrada, "usuario", "eu", "fiz", "completei", unidade, valor));
        }

        foreach (Match match in Regex.Matches(
                     entrada,
                     @"\bminh[ao]s?\s+(?<relacao>[\p{L}\p{N}_-]+(?:\s+(?!(?:é|e|são|sao)\b)[\p{L}\p{N}_-]+){0,3})\s+(?:é|e|são|sao)\s+(?:de\s+)?(?<valor>[^?.!,;]+)",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var relacao = LimparRelacaoCurta(match.Groups["relacao"].Value);
            if (string.IsNullOrWhiteSpace(relacao) || relacao == "nome")
            {
                continue;
            }

            var (valor, unidade, tipoValor) = NormalizarValorRelacao(match.Groups["valor"].Value, string.Empty);
            if (string.IsNullOrWhiteSpace(valor) || ValorReservado(valor))
            {
                continue;
            }

            yield return CriarCandidatoRelacaoEmergente(
                sujeito: "usuario",
                padraoRelacional: relacao,
                rotuloProvisorio: $"minha {relacao}",
                valor: valor,
                unidade: unidade,
                durabilidade: InferirDurabilidadeRelacao(relacao, unidade),
                aliasesBase: AliasesEmergentes(entrada, "usuario", "eu", "meu", "minha", relacao, unidade));
        }

        foreach (Match match in Regex.Matches(
                     entrada,
                     @"\b(?:eu\s+)?moro\s+(?:em|no|na|num|numa)\s+(?<valor>[^?.!,;]{2,80})",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var valor = LimparFraseCurta(match.Groups["valor"].Value);
            if (string.IsNullOrWhiteSpace(valor))
            {
                continue;
            }

            yield return CriarCandidatoRelacaoEmergente(
                sujeito: "usuario",
                padraoRelacional: "moro em",
                rotuloProvisorio: "eu moro em",
                valor: valor,
                unidade: string.Empty,
                durabilidade: "duravel",
                aliasesBase: AliasesEmergentes(entrada, "usuario", "eu", "moro", valor));
        }

        foreach (Match match in Regex.Matches(
                     entrada,
                     @"\b(?:eu\s+)?trabalho\s+(?<conector>com|em|no|na)\s+(?<valor>[^?.!,;]{2,80})",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var valor = LimparFraseCurta(match.Groups["valor"].Value);
            if (string.IsNullOrWhiteSpace(valor))
            {
                continue;
            }

            var conector = TreinadorTokenizadorBpe.NormalizarTexto(match.Groups["conector"].Value);
            yield return CriarCandidatoRelacaoEmergente(
                sujeito: "usuario",
                padraoRelacional: $"trabalho {conector}",
                rotuloProvisorio: $"eu trabalho {conector}",
                valor: valor,
                unidade: string.Empty,
                durabilidade: "duravel",
                aliasesBase: AliasesEmergentes(entrada, "usuario", "eu", "trabalho", conector, valor));
        }
    }

    private static ReferenciaAtivaCognitiva? SelecionarRelacaoParaResposta(
        EstadoCognitivoTurno estado,
        string entradaNormalizada)
    {
        var formasEntrada = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .ToHashSet(StringComparer.Ordinal);

        return estado.ReferenciasAtivas
            .Where(x => x.Familia == "relacao_emergente" && x.Confianca >= 0.50)
            .Select(relacao => new
            {
                Relacao = relacao,
                Pontos = PontuarRelacaoAtivaParaPergunta(relacao, formasEntrada, entradaNormalizada)
            })
            .Where(x => x.Pontos > 0)
            .OrderByDescending(x => x.Pontos)
            .ThenByDescending(x => x.Relacao.Confianca)
            .Select(x => x.Relacao)
            .FirstOrDefault();
    }

    private static int PontuarRelacaoAtivaParaPergunta(
        ReferenciaAtivaCognitiva relacao,
        HashSet<string> formasEntrada,
        string entradaNormalizada)
    {
        var formasRelacao = ExtrairFormasReferenciaAtiva(relacao)
            .Where(x => !AliasRelacionalGenerico(x))
            .ToHashSet(StringComparer.Ordinal);

        var intersecoes = formasRelacao.Count(formasEntrada.Contains);
        var bonusEspecifico = 0;

        if (!string.IsNullOrWhiteSpace(relacao.Unidade) &&
            formasEntrada.Contains(relacao.Unidade))
        {
            bonusEspecifico += 58;
        }

        var verboRelacional = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(relacao.PadraoRelacional)
            .FirstOrDefault(x => !AliasRelacionalGenerico(x));
        if (!string.IsNullOrWhiteSpace(verboRelacional) &&
            formasEntrada.Contains(verboRelacional))
        {
            bonusEspecifico += 42;
        }

        if (intersecoes == 0 && bonusEspecifico == 0)
        {
            return 0;
        }

        var pontuacao = intersecoes * 28;
        pontuacao += bonusEspecifico;
        pontuacao += (int)Math.Round(relacao.Confianca * 20);
        return pontuacao;
    }

    private static AtoConversacional AnalisarAtoConversacional(
        string entradaNormalizada,
        EstadoCognitivoTurno? estado = null)
    {
        var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Select(TreinadorTokenizadorBpe.NormalizarTexto)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var ehPergunta = ParecePergunta(entradaNormalizada);
        var alvo = InferirAlvoConversacional(formas);
        var corrigeRumo = TemCorrecaoDeRumo(formas);
        var solicitaAjuda = TemAlgumaForma(formas, FormasAjuda) &&
                             !TemAlgumaForma(formas, FormasMemoria);
        var consultaPreferencia = ehPergunta && TemAlgumaForma(formas, FormasPreferencia);
        var consultaIdentidade = ehPergunta && TemAlgumaForma(formas, FormasIdentidade);
        var consultaMemoria = ehPergunta && TemAlgumaForma(formas, FormasMemoria);
        var consultaRelacao = ehPergunta &&
                              !consultaMemoria &&
                              alvo == "usuario" &&
                              !consultaIdentidade &&
                              !consultaPreferencia &&
                              ExisteFocoReferencial(formas, estado);

        var acao = "gerar";
        if (corrigeRumo)
        {
            acao = "corrigir_rumo";
        }
        else if (solicitaAjuda)
        {
            acao = "solicitar_ajuda";
        }
        else if (consultaPreferencia)
        {
            acao = "consultar_preferencia";
        }
        else if (consultaIdentidade)
        {
            acao = "consultar_identidade";
        }
        else if (consultaRelacao)
        {
            acao = "consultar_relacao";
        }
        else if (consultaMemoria)
        {
            acao = "consultar_memoria";
        }

        return new AtoConversacional
        {
            Tipo = ehPergunta ? "pergunta" : "declaracao",
            Acao = acao,
            Alvo = alvo,
            Focos = formas
                .Where(x => !AliasRelacionalGenerico(x))
                .Take(12)
                .ToList(),
            EhPergunta = ehPergunta,
            ConsultaMemoria = consultaMemoria,
            ConsultaPreferencia = consultaPreferencia,
            ConsultaIdentidade = consultaIdentidade,
            SolicitaAjuda = solicitaAjuda,
            CorrigeRumo = corrigeRumo
        };
    }

    private static string InferirAlvoConversacional(HashSet<string> formas)
    {
        var falaDeUsuario = TemAlgumaForma(formas, FormasPrimeiraPessoa);
        var falaDeModelo = TemAlgumaForma(formas, FormasSegundaPessoa) ||
                           TemAlgumaForma(formas, FormasModelo);
        var falaDeMemoriaDoUsuario = TemAlgumaForma(formas, FormasMemoria) &&
                                     formas.Overlaps(["pedi", "disse", "falei", "contei", "lembrar", "lembra"]);

        if (falaDeMemoriaDoUsuario)
        {
            return "usuario";
        }

        if (falaDeModelo && !falaDeUsuario)
        {
            return "modelo";
        }

        if (falaDeUsuario)
        {
            return "usuario";
        }

        return TemAlgumaForma(formas, FormasMemoria) ? "usuario" : "indefinido";
    }

    private static bool TemAlgumaForma(HashSet<string> formas, HashSet<string> candidatas)
    {
        return formas.Overlaps(candidatas);
    }

    private static bool TemCorrecaoDeRumo(HashSet<string> formas)
    {
        var temNegacao = formas.Contains("n") || formas.Contains("nao") || formas.Contains("não");
        return formas.Contains("oxi") ||
               formas.Contains("errado") ||
               (temNegacao && formas.Overlaps(FormasCorrecaoRumo));
    }

    private static bool ExisteFocoReferencial(HashSet<string> formas, EstadoCognitivoTurno? estado)
    {
        if (estado is null)
        {
            return formas.Count > 0;
        }

        return estado.ReferenciasAtivas.Any(referencia => ExtrairFormasReferenciaAtiva(referencia)
            .Where(x => !AliasRelacionalGenerico(x))
            .Any(formas.Contains));
    }

    private static bool PerguntaCompostaSobreNomeEReferencia(
        string entradaNormalizada,
        ReferenciaAtivaCognitiva relacao)
    {
        if (!ParecePergunta(entradaNormalizada))
        {
            return false;
        }

        var formasEntrada = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .ToHashSet(StringComparer.Ordinal);
        var formasRelacao = ExtrairFormasReferenciaAtiva(relacao)
            .Where(x => !AliasRelacionalGenerico(x))
            .ToHashSet(StringComparer.Ordinal);

        return formasRelacao.Overlaps(formasEntrada) ||
               (!string.IsNullOrWhiteSpace(relacao.Unidade) && formasEntrada.Contains(relacao.Unidade));
    }

    private static string FormatarResumoMemoriaUsuario(
        EstadoCognitivoTurno estado,
        string? nomeUsuario,
        IReadOnlyList<ReferenciaAtivaCognitiva> preferenciasUsuario,
        IReadOnlyList<string> fatosHistorico)
    {
        var fatos = new List<string>();
        if (!string.IsNullOrWhiteSpace(nomeUsuario))
        {
            fatos.Add($"seu nome é {nomeUsuario}");
        }

        foreach (var relacao in estado.ReferenciasAtivas.Where(x => x.Familia == "relacao_emergente" && x.Confianca >= 0.50))
        {
            fatos.Add(FormatarRespostaRelacaoEmergente(relacao).Trim().TrimEnd('.'));
        }

        var resumoPreferencias = FormatarRespostaPreferencias(preferenciasUsuario, fatosHistorico).Trim().TrimEnd('.');
        if (!string.IsNullOrWhiteSpace(resumoPreferencias))
        {
            fatos.Add(resumoPreferencias);
        }

        fatos.AddRange(fatosHistorico.Where(x => !x.StartsWith("você gosta", StringComparison.Ordinal)));
        fatos = fatos
            .Select(LimparFatoParaResumo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToList();

        if (fatos.Count == 0)
        {
            return string.Empty;
        }

        return $"você me disse que {JuntarListaNatural(fatos)}.";
    }

    private static string FormatarConfirmacaoMemoria(
        EstadoCognitivoTurno estado,
        string? nomeUsuario,
        IReadOnlyList<ReferenciaAtivaCognitiva> preferenciasUsuario,
        string entradaNormalizada)
    {
        var fatos = new List<string>();
        if (!string.IsNullOrWhiteSpace(nomeUsuario) &&
            estado.ReferenciasAtivas.Any(x => x.Familia == "identidade_usuario" && estado.ObjetosAtualizados.Contains(x.Id)))
        {
            fatos.Add($"seu nome é {nomeUsuario}");
        }

        foreach (var relacao in estado.ReferenciasAtivas
                     .Where(x => x.Familia == "relacao_emergente" &&
                                 (estado.RelacoesAtualizadas.Contains(x.Id) ||
                                  ReferenciaApareceNaEntrada(x, entradaNormalizada))))
        {
            fatos.Add(FormatarRespostaRelacaoEmergente(relacao).Trim().TrimEnd('.'));
        }

        foreach (var preferencia in preferenciasUsuario
                     .Where(x => estado.ObjetosAtualizados.Contains(x.Id)))
        {
            fatos.Add(FormatarRespostaPreferencia(preferencia).Trim().TrimEnd('.'));
        }

        fatos = fatos
            .Select(LimparFatoParaResumo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(4)
            .ToList();

        return fatos.Count == 0
            ? "entendido, anotado aqui."
            : $"entendi. {JuntarListaNatural(fatos)}.";
    }

    private static string FormatarRespostaPreferencia(ReferenciaAtivaCognitiva preferencia)
    {
        var valor = LimparPontuacaoFinal(preferencia.ValorAtual);
        if (valor.StartsWith("gosto ", StringComparison.Ordinal))
        {
            var complemento = LimparComplementoPreferencia(valor["gosto ".Length..]);
            return complemento.StartsWith("de ", StringComparison.Ordinal)
                ? $"você gosta {complemento}."
                : $"você gosta de {complemento}.";
        }

        if (valor.StartsWith("prefiro ", StringComparison.Ordinal))
        {
            return $"você {valor.Replace("prefiro ", "prefere ", StringComparison.Ordinal)}.";
        }

        if (valor.StartsWith("não gosto ", StringComparison.Ordinal) ||
            valor.StartsWith("nao gosto ", StringComparison.Ordinal))
        {
            var complemento = valor
                .Replace("não gosto ", string.Empty, StringComparison.Ordinal)
                .Replace("nao gosto ", string.Empty, StringComparison.Ordinal)
                .Trim();
            complemento = LimparComplementoPreferencia(complemento);
            return complemento.StartsWith("de ", StringComparison.Ordinal)
                ? $"você não gosta {complemento}."
                : $"você não gosta de {complemento}.";
        }

        return $"você gosta de {LimparComplementoPreferencia(valor)}.";
    }

    private static string FormatarRespostaPreferencias(
        IReadOnlyList<ReferenciaAtivaCognitiva> preferenciasUsuario,
        IReadOnlyList<string> fatosHistorico)
    {
        var fatos = preferenciasUsuario
            .Select(FormatarRespostaPreferencia)
            .Select(x => x.Trim().TrimEnd('.'))
            .Concat(fatosHistorico.Where(x => x.StartsWith("você gosta", StringComparison.Ordinal)))
            .Select(NormalizarFatoPreferencia)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(6)
            .ToList();

        if (fatos.Count == 0)
        {
            return string.Empty;
        }

        if (fatos.Count == 1)
        {
            return $"{fatos[0]}.";
        }

        var complementosPorChave = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var complemento in fatos
                     .Select(ExtrairComplementoPreferencia)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var chave = CanonicalizarComplementoPreferencia(complemento);
            if (!string.IsNullOrWhiteSpace(chave) && !complementosPorChave.ContainsKey(chave))
            {
                complementosPorChave[chave] = LimparComplementoPreferencia(complemento);
            }
        }

        var complementos = complementosPorChave.Values.ToList();

        return complementos.Count == 0
            ? $"{fatos[0]}."
            : $"você gosta de {JuntarListaNatural(complementos)}.";
    }

    private static IReadOnlyList<string> ExtrairFatosDoHistorico(AnalisePromptEpistemica analisePrompt)
    {
        var textos = analisePrompt.MemoriaLonga
            .Concat(analisePrompt.MemoriaMedia)
            .Concat(analisePrompt.MemoriaCurta)
            .Concat(analisePrompt.TrechosRecuperados)
            .SelectMany(SepararBlocosHistorico)
            .ToList();

        var fatos = new List<string>();
        foreach (var texto in textos)
        {
            fatos.AddRange(ExtrairFatosSimplesDeTexto(texto));
        }

        return fatos
            .Select(LimparFatoParaResumo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToList();
    }

    private static string SelecionarRespostaHistoricoParaPergunta(
        string entradaNormalizada,
        IReadOnlyList<string> fatosHistorico)
    {
        if (fatosHistorico.Count == 0)
        {
            return string.Empty;
        }

        if (Regex.IsMatch(entradaNormalizada, @"\b(nome|chamo)\b", RegexOptions.CultureInvariant))
        {
            return fatosHistorico.FirstOrDefault(x => x.StartsWith("seu nome é", StringComparison.Ordinal)) ?? string.Empty;
        }

        if (Regex.IsMatch(entradaNormalizada, @"\b(idade|anos|quantos)\b", RegexOptions.CultureInvariant))
        {
            return fatosHistorico.FirstOrDefault(x => x.StartsWith("você tem", StringComparison.Ordinal)) ?? string.Empty;
        }

        if (Regex.IsMatch(entradaNormalizada, @"\bgosto\b", RegexOptions.CultureInvariant))
        {
            return fatosHistorico.FirstOrDefault(x => x.StartsWith("você gosta", StringComparison.Ordinal)) ?? string.Empty;
        }

        return fatosHistorico.FirstOrDefault() ?? string.Empty;
    }

    private static string SelecionarComplementoHistoricoParaPerguntaComNome(
        string entradaNormalizada,
        IReadOnlyList<string> fatosHistorico)
    {
        if (fatosHistorico.Count == 0)
        {
            return string.Empty;
        }

        if (Regex.IsMatch(entradaNormalizada, @"\b(idade|anos|quantos)\b", RegexOptions.CultureInvariant))
        {
            return fatosHistorico.FirstOrDefault(x => x.StartsWith("você tem", StringComparison.Ordinal)) ?? string.Empty;
        }

        if (Regex.IsMatch(entradaNormalizada, @"\bgosto\b", RegexOptions.CultureInvariant))
        {
            return fatosHistorico.FirstOrDefault(x => x.StartsWith("você gosta", StringComparison.Ordinal)) ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool ReferenciaApareceNaEntrada(
        ReferenciaAtivaCognitiva referencia,
        string entradaNormalizada)
    {
        var formasEntrada = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .ToHashSet(StringComparer.Ordinal);
        return ExtrairFormasReferenciaAtiva(referencia)
            .Where(x => !AliasRelacionalGenerico(x))
            .Any(formasEntrada.Contains);
    }

    private static IEnumerable<string> SepararBlocosHistorico(string texto)
    {
        var normalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        foreach (var parte in normalizado.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bloco = parte.Trim();
            if (Regex.IsMatch(bloco, @"^(ôria|oria|modelo)\s*:", RegexOptions.CultureInvariant))
            {
                continue;
            }

            bloco = Regex.Replace(bloco, @"^(usuário|usuario)\s*:\s*", string.Empty, RegexOptions.CultureInvariant).Trim();
            if (!string.IsNullOrWhiteSpace(bloco))
            {
                yield return bloco;
            }
        }
    }

    private static IEnumerable<string> ExtrairFatosSimplesDeTexto(string texto)
    {
        var matchNome = Regex.Match(texto, @"\bmeu nome\s+(?:é|e)\s+(?<valor>[\p{L}\p{N}_-]+)", RegexOptions.CultureInvariant);
        if (matchNome.Success)
        {
            var valor = LimparValorCurto(matchNome.Groups["valor"].Value);
            if (!ValorReservado(valor))
            {
                yield return $"seu nome é {valor}";
            }
        }

        var matchIdade = Regex.Match(texto, @"\b(?:eu\s+)?tenho\s+(?<valor>\d+(?:[,.]\d+)?)\s*(?<unidade>[\p{L}]+)?", RegexOptions.CultureInvariant);
        if (matchIdade.Success)
        {
            var valor = LimparValorCurto(matchIdade.Groups["valor"].Value);
            var unidade = NormalizarUnidade(matchIdade.Groups["unidade"].Value);
            yield return $"você tem {JuntarValorUnidade(valor, unidade)}";
        }

        var matchGosto = Regex.Match(texto, @"\b(?:eu\s+)?gosto\s+(?<valor>[^?.!,;]{2,80})", RegexOptions.CultureInvariant);
        if (matchGosto.Success)
        {
            var valor = LimparPontuacaoFinal(matchGosto.Groups["valor"].Value);
            if (!string.IsNullOrWhiteSpace(valor))
            {
                yield return valor.StartsWith("de ", StringComparison.Ordinal)
                    ? $"você gosta {valor}"
                    : $"você gosta de {valor}";
            }
        }

        var matchGenerico = Regex.Match(
            texto,
            @"\bminh[ao]\s+(?<relacao>[\p{L}\p{N}_-]+(?:\s+[\p{L}\p{N}_-]+){0,3})\s+(?:é|e)\s+(?<valor>[^?.!,;]{2,80})",
            RegexOptions.CultureInvariant);
        if (matchGenerico.Success)
        {
            var relacao = LimparRelacaoCurta(matchGenerico.Groups["relacao"].Value);
            var valor = LimparPontuacaoFinal(matchGenerico.Groups["valor"].Value);
            if (!string.IsNullOrWhiteSpace(relacao) && !string.IsNullOrWhiteSpace(valor))
            {
                yield return $"sua {relacao} é {valor}";
            }
        }
    }

    private static string FormatarRespostaCompostaNomeRelacao(
        string nomeUsuario,
        ReferenciaAtivaCognitiva relacao)
    {
        var respostaRelacao = FormatarRespostaRelacaoEmergente(relacao).Trim().TrimEnd('.');
        return $"seu nome é {nomeUsuario} e {respostaRelacao}.";
    }

    private static string FormatarRespostaRelacaoEmergente(ReferenciaAtivaCognitiva relacao)
    {
        var valorComUnidade = JuntarValorUnidade(relacao.ValorAtual, relacao.Unidade);
        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("tenho", StringComparison.Ordinal))
        {
            return $"você tem {valorComUnidade}.";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("moro", StringComparison.Ordinal))
        {
            return $"você mora em {relacao.ValorAtual}.";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("trabalho", StringComparison.Ordinal))
        {
            var conector = relacao.PadraoRelacional.Replace("trabalho", string.Empty, StringComparison.Ordinal).Trim();
            conector = string.IsNullOrWhiteSpace(conector) ? "com" : conector;
            return $"você trabalha {conector} {relacao.ValorAtual}.";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.RotuloDebug.StartsWith("minha ", StringComparison.Ordinal))
        {
            var campo = relacao.RotuloDebug.Replace("minha ", string.Empty, StringComparison.Ordinal).Trim();
            if (!string.IsNullOrWhiteSpace(campo))
            {
                return $"sua {campo} é {valorComUnidade}.";
            }
        }

        var sujeito = relacao.Sujeito == "usuario" ? "você" : relacao.Sujeito;
        return $"{sujeito} {relacao.RotuloDebug.Replace("eu ", string.Empty, StringComparison.Ordinal)} {valorComUnidade}.";
    }

    private static string FormatarContextoRelacaoEmergente(ReferenciaAtivaCognitiva relacao)
    {
        var valorComUnidade = JuntarValorUnidade(relacao.ValorAtual, relacao.Unidade);
        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("tenho", StringComparison.Ordinal))
        {
            return $"usuario tem {valorComUnidade}";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("moro", StringComparison.Ordinal))
        {
            return $"usuario mora em {relacao.ValorAtual}";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.PadraoRelacional.StartsWith("trabalho", StringComparison.Ordinal))
        {
            return $"usuario {relacao.PadraoRelacional} {relacao.ValorAtual}";
        }

        if (relacao.Sujeito == "usuario" &&
            relacao.RotuloDebug.StartsWith("minha ", StringComparison.Ordinal))
        {
            var campo = relacao.RotuloDebug.Replace("minha ", string.Empty, StringComparison.Ordinal).Trim();
            if (!string.IsNullOrWhiteSpace(campo))
            {
                return $"usuario {campo} é {valorComUnidade}";
            }
        }

        return $"{relacao.Sujeito} {relacao.RotuloDebug} {valorComUnidade}";
    }

    private static CandidatoRelacaoEmergente CriarCandidatoRelacaoEmergente(
        string sujeito,
        string padraoRelacional,
        string rotuloProvisorio,
        string valor,
        string unidade,
        string durabilidade,
        IReadOnlyList<string> aliasesBase)
    {
        var (valorNormalizado, unidadeNormalizada, tipoValor) = NormalizarValorRelacao(valor, unidade);
        var aliases = aliasesBase
            .Append(padraoRelacional)
            .Append(rotuloProvisorio)
            .Append(valorNormalizado)
            .Append(unidadeNormalizada)
            .Select(TreinadorTokenizadorBpe.NormalizarTexto)
            .SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(24)
            .ToList();

        return new CandidatoRelacaoEmergente(
            sujeito,
            TreinadorTokenizadorBpe.NormalizarTexto(padraoRelacional),
            TreinadorTokenizadorBpe.NormalizarTexto(rotuloProvisorio),
            valorNormalizado,
            tipoValor,
            unidadeNormalizada,
            durabilidade,
            aliases,
            string.Equals(durabilidade, "duravel", StringComparison.Ordinal) ? 0.78 : 0.62);
    }

    private static (string Valor, string Unidade, string TipoValor) NormalizarValorRelacao(string valorBruto, string unidadeBruta)
    {
        var valor = LimparFraseCurta(valorBruto);
        var unidade = NormalizarUnidade(unidadeBruta);
        var matchNumero = Regex.Match(
            valor,
            @"^(?<numero>\d+(?:[,.]\d+)?)\s*(?<unidade>[\p{L}]+(?:\s+de\s+[\p{L}]+)?)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (matchNumero.Success)
        {
            valor = LimparValorCurto(matchNumero.Groups["numero"].Value);
            if (string.IsNullOrWhiteSpace(unidade))
            {
                unidade = NormalizarUnidade(matchNumero.Groups["unidade"].Value);
            }

            return (valor, unidade, "numero");
        }

        return (valor, unidade, "texto");
    }

    private static string NormalizarUnidade(string unidadeBruta)
    {
        var unidade = LimparFraseCurta(unidadeBruta);
        if (string.IsNullOrWhiteSpace(unidade))
        {
            return string.Empty;
        }

        if (Regex.IsMatch(unidade, @"\banos?\b", RegexOptions.CultureInvariant))
        {
            return "anos";
        }

        return unidade.Length <= 32 ? unidade : unidade[..32].Trim();
    }

    private static string InferirDurabilidadeRelacao(string relacao, string unidade)
    {
        if (!string.IsNullOrWhiteSpace(unidade))
        {
            return "duravel";
        }

        if (Regex.IsMatch(relacao, @"\b(agora|hoje|amanha|amanhã|ontem|cansado|triste|feliz)\b", RegexOptions.CultureInvariant))
        {
            return "temporario";
        }

        return "duravel";
    }

    private static string RemoverComandoMemoria(string entradaNormalizada)
    {
        return Regex.Replace(
                entradaNormalizada,
                @"^\s*(?:pode\s+)?(?:gravar|guardar|guarde|lembra|lembre|anota|anote|registre|registrar|corrige|corrija|atualize|atualiza)\s+(?:que\s+)?",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Trim();
    }

    private static bool TemComandoMemoria(string entradaNormalizada)
    {
        return Regex.IsMatch(
            entradaNormalizada,
            @"^\s*(?:pode\s+)?(?:gravar|guardar|guarde|lembra|lembre|anota|anote|registre|registrar|corrige|corrija|atualize|atualiza)\s+(?:que\s+)?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool ParecePergunta(string entradaNormalizada)
    {
        return entradaNormalizada.EndsWith("?", StringComparison.Ordinal) ||
               Regex.IsMatch(entradaNormalizada, @"^\s*(qual|quais|quanto|quantos|onde|quando|quem|como|por que)\b", RegexOptions.CultureInvariant);
    }

    private static bool ContemDeclaracaoIdentidadeUsuario(string entradaNormalizada)
    {
        return Regex.IsMatch(
            entradaNormalizada,
            @"(?:nao|não).*meu nome.*(?:é|e)\s+[\p{L}\p{N}_-]+|(?:^|[.!?]\s*)(?:na verdade\s+|corrige\s+|corrija\s+|atualize\s+|atualiza\s+)?meu nome\s+(?:é|e)\s+[\p{L}\p{N}_-]+|(?:^|[.!?]\s*)(?:eu\s+)?me chamo\s+[\p{L}\p{N}_-]+|pode me chamar de\s+[\p{L}\p{N}_-]+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool MatchEstaNegado(string entradaNormalizada, int indiceMatch)
    {
        var inicio = Math.Max(0, indiceMatch - 16);
        var prefixo = entradaNormalizada[inicio..indiceMatch];
        return Regex.IsMatch(prefixo, @"\b(?:nao|não)\s*$", RegexOptions.CultureInvariant);
    }

    private static string LimparRelacaoCurta(string valor)
    {
        var normalizado = LimparFraseCurta(valor);
        normalizado = Regex.Replace(normalizado, @"[^\p{L}\p{N}_\-\s]+", "", RegexOptions.CultureInvariant);
        normalizado = Regex.Replace(normalizado, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return normalizado.Length <= 48 ? normalizado : normalizado[..48].Trim();
    }

    private static IReadOnlyList<string> AliasesEmergentes(string textoBase, params string[] extras)
    {
        return ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(textoBase)
            .Concat(extras.SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(32)
            .ToList();
    }

    private static bool ValoresEquivalentes(string esquerdo, string direito)
    {
        return string.Equals(
            TreinadorTokenizadorBpe.NormalizarTexto(esquerdo),
            TreinadorTokenizadorBpe.NormalizarTexto(direito),
            StringComparison.Ordinal);
    }

    private static bool ReferenciaObjetoProtegida(ObjetoReferenciaCognitiva objeto)
    {
        return objeto.Familia == "identidade_usuario" &&
               (objeto.Confianca >= 0.78 || objeto.Forca >= 2);
    }

    private static bool ObjetoSobreUsuario(ObjetoReferenciaCognitiva objeto)
    {
        return objeto.Familia is "identidade_usuario" or "preferencia_usuario";
    }

    private static bool ReferenciaSobreUsuario(ReferenciaAtivaCognitiva referencia)
    {
        return referencia.Familia is "identidade_usuario" or "preferencia_usuario" ||
               referencia.Sujeito == "usuario";
    }

    private static bool RelacaoEmergenteProtegida(RelacaoEmergenteCognitiva relacao)
    {
        return relacao.Durabilidade == "duravel" &&
               (relacao.Confianca >= 0.78 || relacao.Forca >= 2);
    }

    private static bool TemMarcadorCorrecao(string entradaNormalizada)
    {
        return Regex.IsMatch(
            entradaNormalizada,
            @"\b(na verdade|corrige|correcao|correção|errado|errei|atualize|atualiza|troque|troca|mude|muda|mudou|agora é|agora e|não|nao)\b",
            RegexOptions.CultureInvariant);
    }

    private static bool AliasRelacionalGenerico(string alias)
    {
        string[] genericos =
        [
            "usuario", "usuário", "eu", "meu", "minha", "tenho", "tem", "sou", "é", "e", "valor", "algo"
        ];
        return genericos.Contains(alias, StringComparer.Ordinal);
    }

    private static IEnumerable<string> FormasRelacaoEmergente(RelacaoEmergenteCognitiva relacao)
    {
        return relacao.Aliases
            .Concat(relacao.Evidencias)
            .Append(relacao.PadraoRelacional)
            .Append(relacao.RotuloProvisorio)
            .Append(relacao.ValorAtual)
            .Append(relacao.Unidade)
            .SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo);
    }

    private static IEnumerable<string> ExtrairFormasReferenciaAtiva(ReferenciaAtivaCognitiva relacao)
    {
        return relacao.Aliases
            .Concat(new[]
            {
                relacao.RotuloDebug,
                relacao.PadraoRelacional,
                relacao.ValorAtual,
                relacao.Unidade
            })
            .SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo);
    }

    private static List<string> AliasesReferenciaRelacao(RelacaoEmergenteCognitiva relacao)
    {
        return relacao.Aliases
            .Concat(relacao.Evidencias.SelectMany(ConstrutorMemoriaEpistemica.ExtrairFormasConteudo))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(40)
            .ToList();
    }

    private static string JuntarValorUnidade(string valor, string unidade)
    {
        if (string.IsNullOrWhiteSpace(unidade) ||
            valor.Contains(unidade, StringComparison.Ordinal))
        {
            return valor;
        }

        return $"{valor} {unidade}";
    }

    private static string CriarIdRelacaoEmergente(string sujeito, string padraoRelacional)
    {
        var conteudo = $"{sujeito}|{padraoRelacional}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(conteudo));
        return "rel_" + Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private void PersistirSeNecessario()
    {
        if (!_configuracao.PersistirExperiencia)
        {
            return;
        }

        var json = JsonSerializer.Serialize(_estado, OpcoesJson);
        File.WriteAllText(_caminhoArquivo, json, PadroesSerializacao.Utf8SemBom);
    }

    private static string CriarIdReferencia(string familia, string rotulo)
    {
        var conteudo = $"{familia}|{rotulo}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(conteudo));
        return "ref_" + Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string LimparValorCurto(string valor)
    {
        var normalizado = TreinadorTokenizadorBpe.NormalizarTexto(valor);
        normalizado = Regex.Replace(normalizado, @"[^\p{L}\p{N}_-]+", "", RegexOptions.CultureInvariant);
        return normalizado.Trim();
    }

    private static string LimparFraseCurta(string valor)
    {
        var normalizado = TreinadorTokenizadorBpe.NormalizarTexto(valor);
        normalizado = Regex.Replace(normalizado, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return normalizado.Length <= 90 ? normalizado : normalizado[..90].Trim();
    }

    private static string LimparPontuacaoFinal(string valor)
    {
        var normalizado = LimparFraseCurta(valor);
        normalizado = Regex.Replace(normalizado, @"\s*[?.!,;:]+$", string.Empty, RegexOptions.CultureInvariant);
        return normalizado.Trim();
    }

    private static string LimparFatoParaResumo(string fato)
    {
        var normalizado = LimparPontuacaoFinal(fato);
        normalizado = Regex.Replace(normalizado, @"^você me disse que\s+", string.Empty, RegexOptions.CultureInvariant);
        return normalizado.Trim();
    }

    private static string NormalizarFatoPreferencia(string fato)
    {
        var normalizado = LimparFatoParaResumo(fato);
        normalizado = Regex.Replace(normalizado, @"\btmb\b", "tambem", RegexOptions.CultureInvariant);
        normalizado = Regex.Replace(normalizado, @"\btambém\b", "tambem", RegexOptions.CultureInvariant);
        normalizado = Regex.Replace(normalizado, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return normalizado;
    }

    private static string ExtrairComplementoPreferencia(string fato)
    {
        return LimparComplementoPreferencia(fato
            .Replace("você gosta de ", string.Empty, StringComparison.Ordinal)
            .Replace("você gosta ", string.Empty, StringComparison.Ordinal));
    }

    private static string LimparComplementoPreferencia(string complemento)
    {
        var limpo = LimparPontuacaoFinal(complemento);
        limpo = Regex.Replace(limpo, @"\b(tmb|tambem|também)\b", string.Empty, RegexOptions.CultureInvariant);
        limpo = Regex.Replace(limpo, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return limpo;
    }

    private static string CanonicalizarComplementoPreferencia(string complemento)
    {
        var limpo = RemoverAcentos(LimparComplementoPreferencia(complemento));
        var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(limpo)
            .Where(x => x is not "de" and not "tambem" and not "tmb")
            .Select(NormalizarChavePreferencia)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return string.Join(" ", formas);
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);
        foreach (var caractere in normalizado)
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caractere);
            if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CriarRotuloPreferenciaUsuario(string preferencia)
    {
        var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(preferencia)
            .Where(x => !AliasRelacionalGenerico(x))
            .Where(x => x is not "gosto" and not "prefiro" and not "nao" and not "não" and not "de" and not "tmb" and not "tambem" and not "também")
            .Select(NormalizarChavePreferencia)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();

        var chave = formas.Count == 0 ? "geral" : string.Join(" ", formas);
        return $"preferencia do usuario {chave}";
    }

    private static string NormalizarChavePreferencia(string forma)
    {
        var chave = LimparValorCurto(forma);
        if (chave.Length > 4 && chave.EndsWith("s", StringComparison.Ordinal))
        {
            chave = chave[..^1];
        }

        return chave;
    }

    private static string JuntarListaNatural(IReadOnlyList<string> itens)
    {
        if (itens.Count == 0)
        {
            return string.Empty;
        }

        if (itens.Count == 1)
        {
            return itens[0];
        }

        if (itens.Count == 2)
        {
            return $"{itens[0]} e {itens[1]}";
        }

        return $"{string.Join(", ", itens.Take(itens.Count - 1))} e {itens[^1]}";
    }

    private static bool ValorReservado(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        string[] reservados = ["e", "qual", "quem", "como", "nome", "seu", "meu", "vc", "voce", "você"];
        return reservados.Contains(valor, StringComparer.Ordinal);
    }

    private sealed record CandidatoReferencia(
        string Familia,
        string RotuloDebug,
        string Valor,
        IReadOnlyList<string> Aliases,
        double ConfiancaInicial);

    private sealed record CandidatoRelacaoEmergente(
        string Sujeito,
        string PadraoRelacional,
        string RotuloProvisorio,
        string Valor,
        string TipoValor,
        string Unidade,
        string Durabilidade,
        IReadOnlyList<string> Aliases,
        double ConfiancaInicial);
}
