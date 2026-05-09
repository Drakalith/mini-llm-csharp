using System.Text.Json;
using System.Text.RegularExpressions;
using MiniLlmDidatico.Arquivos;
using MiniLlmDidatico.Diagnostico;
using MiniLlmDidatico.Tokenizacao;

namespace MiniLlmDidatico.Cognicao;

public sealed class PerfilIdentidadeConversacional
{
    public string NomeModelo { get; set; } = "Ôria";
    public string AmbienteBase { get; set; } = string.Empty;
    public string DiferencaEuUsuario { get; set; } = string.Empty;
    public string MissaoBase { get; set; } = string.Empty;
}

public sealed class TurnoMemoriaConversacional
{
    public DateTimeOffset MomentoBrasilSp { get; init; }
    public string Papel { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
}

public sealed class VersaoLembranca
{
    public int NumeroVersao { get; init; }
    public string Texto { get; init; } = string.Empty;
    public DateTimeOffset CriadoEmBrasilSp { get; init; }
    public string MotivoAtualizacao { get; init; } = string.Empty;
}

public sealed class LembrancaConsolidada
{
    public string Chave { get; init; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public List<string> FormasChave { get; init; } = new();
    public int Forca { get; set; }
    public DateTimeOffset PrimeiroRegistroBrasilSp { get; init; }
    public DateTimeOffset UltimaAtualizacaoBrasilSp { get; set; }
    public string ContextoEpisodico { get; set; } = string.Empty;
    public int FrequenciaUso { get; set; } = 1;
    public List<string> DominiosAssociados { get; init; } = new();
    public int VersaoAtual { get; set; } = 1;
    public List<VersaoLembranca> HistoricoVersoes { get; init; } = new();
    public string Recencia { get; set; } = "recente";
    public string EntidadePrincipal { get; set; } = string.Empty;
}

public sealed class EstadoMemoriaConversacional
{
    public string VersaoFormato { get; set; } = "2";
    public DateTimeOffset CriadoEmBrasilSp { get; set; }
    public PerfilIdentidadeConversacional Identidade { get; init; } = new();
    public List<TurnoMemoriaConversacional> Turnos { get; init; } = new();
    public List<LembrancaConsolidada> LembrancasLongoPrazo { get; init; } = new();
}

public sealed class ContextoMemoriaConversacional
{
    public string NomeModelo { get; init; } = "Ôria";
    public string AmbienteBase { get; init; } = string.Empty;
    public string DiferencaEuUsuario { get; init; } = string.Empty;
    public string MissaoBase { get; init; } = string.Empty;
    public string TempoDeVidaTexto { get; init; } = string.Empty;
    public List<string> MemoriaCurta { get; init; } = new();
    public List<string> MemoriaMedia { get; init; } = new();
    public List<string> MemoriaLonga { get; init; } = new();
    public bool DevePerguntarAntesDeResponder { get; init; }
    public string PerguntaCuriosaSugerida { get; init; } = string.Empty;
    public string MotivoCuriosidade { get; init; } = string.Empty;
}

public sealed class GestorMemoriaConversacional
{
    private const string NomeArquivoMemoria = "12_memoria_conversa.json";
    private static readonly JsonSerializerOptions OpcoesJson = PadroesSerializacao.CriarOpcoesJson(
        writeIndented: true,
        propertyNameCaseInsensitive: true);

    private readonly string _caminhoArquivo;
    private readonly EstadoMemoriaConversacional _estado;

    private GestorMemoriaConversacional(string caminhoArquivo, EstadoMemoriaConversacional estado)
    {
        _caminhoArquivo = caminhoArquivo;
        _estado = estado;
    }

    public static GestorMemoriaConversacional CarregarOuCriar(
        string raizArtefatos,
        DateTimeOffset? criadoEmBrasilSp = null,
        PerfilIdentidadeConversacional? identidadePadrao = null)
    {
        Directory.CreateDirectory(raizArtefatos);
        var caminhoArquivo = Path.Combine(raizArtefatos, NomeArquivoMemoria);

        if (!File.Exists(caminhoArquivo))
        {
            var gestorNovo = new GestorMemoriaConversacional(caminhoArquivo, CriarEstadoPadrao(criadoEmBrasilSp, identidadePadrao));
            gestorNovo.Persistir();
            return gestorNovo;
        }

        try
        {
            var json = File.ReadAllText(caminhoArquivo, PadroesSerializacao.Utf8SemBom);
            var estado = JsonSerializer.Deserialize<EstadoMemoriaConversacional>(json, OpcoesJson)
                         ?? CriarEstadoPadrao(criadoEmBrasilSp, identidadePadrao);
            var versaoOriginal = estado.VersaoFormato;

            estado.VersaoFormato = "2";
            if (!string.Equals(versaoOriginal, "2", StringComparison.Ordinal))
            {
                estado.Turnos.Clear();
                estado.LembrancasLongoPrazo.Clear();
            }

            AplicarIdentidadePadrao(estado, identidadePadrao);

            foreach (var lembranca in estado.LembrancasLongoPrazo)
            {
                if (lembranca.FormasChave.Count == 0)
                {
                    lembranca.FormasChave.AddRange(
                        ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(lembranca.Texto)
                            .Distinct(StringComparer.Ordinal)
                            .Take(8));
                }
            }

            SanitizarEstado(estado);

            if (criadoEmBrasilSp.HasValue &&
                DiferencaEmMinutos(estado.CriadoEmBrasilSp, criadoEmBrasilSp.Value) > 1.0)
            {
                estado = CriarEstadoPadrao(criadoEmBrasilSp, identidadePadrao);
            }

            var gestor = new GestorMemoriaConversacional(caminhoArquivo, estado);
            gestor.Persistir();
            return gestor;
        }
        catch
        {
            var gestorFallback = new GestorMemoriaConversacional(caminhoArquivo, CriarEstadoPadrao(criadoEmBrasilSp, identidadePadrao));
            gestorFallback.Persistir();
            return gestorFallback;
        }
    }

    public EstadoMemoriaConversacional Estado => _estado;

    public ContextoMemoriaConversacional CriarContexto(string entradaUsuario)
    {
        var entradaNormalizada = TreinadorTokenizadorBpe.NormalizarTexto(entradaUsuario);
        var formasAtivas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(entradaNormalizada)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var memoriaCurta = _estado.Turnos
            .TakeLast(6)
            .Select(FormatarTurno)
            .ToList();

        var memoriaMedia = _estado.Turnos
            .Reverse<TurnoMemoriaConversacional>()
            .Skip(Math.Min(_estado.Turnos.Count, 6))
            .Select(turno => new
            {
                Turno = turno,
                Pontuacao = PontuarTexto(formasAtivas, turno.Texto)
            })
            .Where(x => x.Pontuacao > 0)
            .OrderByDescending(x => x.Pontuacao)
            .ThenByDescending(x => x.Turno.MomentoBrasilSp)
            .Take(4)
            .Select(x => FormatarTurno(x.Turno))
            .ToList();

        var memoriaLonga = _estado.LembrancasLongoPrazo
            .Select(lembranca => new
            {
                Lembranca = lembranca,
                Pontuacao = PontuarTexto(formasAtivas, lembranca.Texto, lembranca.FormasChave) +
                            lembranca.Forca +
                            (lembranca.FrequenciaUso * 2) +
                            (lembranca.DominiosAssociados.Any(d => formasAtivas.Contains(d)) ? 15 : 0)
            })
            .OrderByDescending(x => x.Pontuacao)
            .ThenByDescending(x => x.Lembranca.UltimaAtualizacaoBrasilSp)
            .Take(6)
            .Select(x => x.Lembranca.Texto)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ContextoMemoriaConversacional
        {
            NomeModelo = _estado.Identidade.NomeModelo,
            AmbienteBase = _estado.Identidade.AmbienteBase,
            DiferencaEuUsuario = _estado.Identidade.DiferencaEuUsuario,
            MissaoBase = _estado.Identidade.MissaoBase,
            TempoDeVidaTexto = FormatarTempoDeVida(_estado.CriadoEmBrasilSp, RelogioBrasil.AgoraBrasilSp()),
            MemoriaCurta = memoriaCurta,
            MemoriaMedia = memoriaMedia,
            MemoriaLonga = memoriaLonga,
            DevePerguntarAntesDeResponder = false,
            PerguntaCuriosaSugerida = string.Empty,
            MotivoCuriosidade = string.Empty
        };
    }

    public void RegistrarTurnoUsuario(string texto, string? contextoEpisodico = null, string? dominioAssociado = null)
    {
        RegistrarTurno("usuario", texto);
        ExtrairLembrancasUsuario(texto, contextoEpisodico, dominioAssociado);
        Persistir();
    }

    public void RegistrarTurnoModelo(string texto)
    {
        RegistrarTurno("oria", texto);
        Persistir();
    }

    private void RegistrarTurno(string papel, string texto)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        if (string.IsNullOrWhiteSpace(textoNormalizado))
        {
            return;
        }

        _estado.Turnos.Add(new TurnoMemoriaConversacional
        {
            MomentoBrasilSp = RelogioBrasil.AgoraBrasilSp(),
            Papel = papel,
            Texto = textoNormalizado
        });

        if (_estado.Turnos.Count > 400)
        {
            _estado.Turnos.RemoveRange(0, _estado.Turnos.Count - 400);
        }
    }

    private void ExtrairLembrancasUsuario(string texto, string? contextoEpisodico = null, string? dominioAssociado = null)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        if (string.IsNullOrWhiteSpace(textoNormalizado))
        {
            return;
        }

        foreach (var lembranca in DetectarLembrancas(textoNormalizado))
        {
            ConsolidarLembranca(lembranca.chave, lembranca.texto, lembranca.formasChave, contextoEpisodico, dominioAssociado);
        }
    }

    private IEnumerable<(string chave, string texto, List<string> formasChave)> DetectarLembrancas(string textoNormalizado)
    {
        foreach (var frase in QuebrarEmFrases(textoNormalizado))
        {
            var fraseMemoria = NormalizarFraseMemoria(frase);
            if (string.IsNullOrWhiteSpace(fraseMemoria) || fraseMemoria.EndsWith("?", StringComparison.Ordinal))
            {
                continue;
            }

            var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(fraseMemoria)
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToList();

            if (formas.Count < 2 || fraseMemoria.Length < 12)
            {
                continue;
            }

            yield return (ConstruirChaveSemantica(formas), fraseMemoria, formas);
        }
    }

    private void ConsolidarLembranca(string chave, string texto, IReadOnlyCollection<string> formasChave, string? contextoEpisodico = null, string? dominioAssociado = null)
    {
        var momentoAtual = RelogioBrasil.AgoraBrasilSp();
        var existente = _estado.LembrancasLongoPrazo.FirstOrDefault(x => x.Chave == chave);
        if (existente is null)
        {
            var entidade = ExtrairEntidadePrincipal(texto);
            _estado.LembrancasLongoPrazo.Add(new LembrancaConsolidada
            {
                Chave = chave,
                Texto = texto,
                FormasChave = formasChave.ToList(),
                Forca = 1,
                FrequenciaUso = 1,
                PrimeiroRegistroBrasilSp = momentoAtual,
                UltimaAtualizacaoBrasilSp = momentoAtual,
                ContextoEpisodico = contextoEpisodico ?? string.Empty,
                DominiosAssociados = dominioAssociado is not null ? new List<string> { dominioAssociado } : new List<string>(),
                VersaoAtual = 1,
                HistoricoVersoes = new List<VersaoLembranca>
                {
                    new() { NumeroVersao = 1, Texto = texto, CriadoEmBrasilSp = momentoAtual, MotivoAtualizacao = "criacao" }
                },
                Recencia = "recente",
                EntidadePrincipal = entidade
            });
        }
        else
        {
            var textoAnterior = existente.Texto;
            var mudouConteudo = !string.Equals(textoAnterior, texto, StringComparison.Ordinal);

            if (mudouConteudo)
            {
                existente.VersaoAtual++;
                existente.HistoricoVersoes.Add(new VersaoLembranca
                {
                    NumeroVersao = existente.VersaoAtual,
                    Texto = textoAnterior,
                    CriadoEmBrasilSp = existente.UltimaAtualizacaoBrasilSp,
                    MotivoAtualizacao = "atualizacao"
                });

                if (existente.HistoricoVersoes.Count > 5)
                {
                    existente.HistoricoVersoes.RemoveRange(0, existente.HistoricoVersoes.Count - 5);
                }
            }

            existente.Texto = texto;
            existente.Forca++;
            existente.FrequenciaUso++;
            existente.UltimaAtualizacaoBrasilSp = momentoAtual;
            existente.Recencia = ClassificarRecencia(existente.UltimaAtualizacaoBrasilSp, momentoAtual);

            if (existente.FormasChave.Count == 0)
            {
                existente.FormasChave.AddRange(formasChave);
            }
            if (!string.IsNullOrWhiteSpace(contextoEpisodico) &&
                existente.ContextoEpisodico != contextoEpisodico)
            {
                existente.ContextoEpisodico = contextoEpisodico;
            }
            if (dominioAssociado is not null &&
                !existente.DominiosAssociados.Contains(dominioAssociado, StringComparer.Ordinal))
            {
                existente.DominiosAssociados.Add(dominioAssociado);
            }
        }

        AtualizarRecenciaTodasMemorias(momentoAtual);

        if (_estado.LembrancasLongoPrazo.Count > 128)
        {
            var excesso = _estado.LembrancasLongoPrazo
                .OrderBy(x => x.Forca)
                .ThenBy(x => x.UltimaAtualizacaoBrasilSp)
                .Take(_estado.LembrancasLongoPrazo.Count - 128)
                .ToList();

            foreach (var item in excesso)
            {
                _estado.LembrancasLongoPrazo.Remove(item);
            }
        }
    }

    private static string ExtrairEntidadePrincipal(string texto)
    {
        var entidades = new[]
        {
            ("cachorro", "rex"), ("gato", "gato"), ("irma", "ana"), ("irmao", "irmao"),
            ("namorada", "julia"), ("namorado", "namorado"), ("chefe", "chefe"),
            ("amigo", "amigo"), ("medico", "medico"), ("salario", "salario"),
            ("trabalho", "trabalho"), ("casa", "casa"), ("viagem", "viagem"),
            ("musica", "musica"), ("comida", "comida"), ("linguagem", "linguagem"),
            ("projeto", "projeto"), ("emprego", "emprego"), ("praia", "praia"),
            ("parque", "parque"), ("livro", "livro"), ("autor", "autor"),
            ("cidade", "cidade"), ("estado", "estado"), ("pais", "pais")
        };

        var textoLower = texto.ToLowerInvariant();
        foreach (var (entidade, _) in entidades)
        {
            if (textoLower.Contains(entidade, StringComparison.Ordinal))
            {
                return entidade;
            }
        }

        var nomesProprios = Regex.Matches(texto, @"\b([A-Z][a-záàâãéèêíïóòôõöç]+)\b", RegexOptions.CultureInvariant);
        foreach (Match match in nomesProprios)
        {
            var nome = match.Groups[1].Value.ToLowerInvariant();
            if (nome.Length > 2 && !FormasPessoais.Contains(nome))
            {
                return nome;
            }
        }

        return "geral";
    }

    private static readonly HashSet<string> FormasPessoais = new(StringComparer.Ordinal)
    {
        "eu", "me", "mim", "comigo", "voce", "você", "ele", "ela", "nos", "eles", "elas",
        "meu", "minha", "meus", "minhas", "seu", "sua", "seus", "suas",
        "que", "o", "a", "os", "as", "um", "uma", "e", "ou", "nao", "sim",
        "muito", "mais", "pouco", "bem", "mal", "ja", "ainda", "tambem",
        "hoje", "ontem", "amanha", "agora", "sempre", "nunca"
    };

    private static string ClassificarRecencia(DateTimeOffset ultimaAtualizacao, DateTimeOffset agora)
    {
        var diferenca = agora - ultimaAtualizacao;
        if (diferenca.TotalMinutes < 30) return "recente";
        if (diferenca.TotalHours < 2) return "media";
        if (diferenca.TotalHours < 24) return "antiga";
        return "muito_antiga";
    }

    private void AtualizarRecenciaTodasMemorias(DateTimeOffset agora)
    {
        foreach (var lembranca in _estado.LembrancasLongoPrazo)
        {
            lembranca.Recencia = ClassificarRecencia(lembranca.UltimaAtualizacaoBrasilSp, agora);
        }
    }

    public IReadOnlyList<LembrancaConsolidada> BuscarMemoriasPorEntidade(string entidade)
    {
        return _estado.LembrancasLongoPrazo
            .Where(l => l.EntidadePrincipal.Equals(entidade, StringComparison.OrdinalIgnoreCase) ||
                        l.Texto.Contains(entidade, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.UltimaAtualizacaoBrasilSp)
            .ToList();
    }

    public string FormatarTempoDesdeAtualizacao(LembrancaConsolidada lembranca)
    {
        var agora = RelogioBrasil.AgoraBrasilSp();
        var diferenca = agora - lembranca.UltimaAtualizacaoBrasilSp;

        if (diferenca.TotalMinutes < 1) return "agora mesmo";
        if (diferenca.TotalMinutes < 60) return $"ha {(int)diferenca.TotalMinutes} minutos";
        if (diferenca.TotalHours < 24) return $"ha {(int)diferenca.TotalHours} horas";
        if (diferenca.TotalDays < 7) return $"ha {(int)diferenca.TotalDays} dias";
        return $"ha {(int)diferenca.TotalDays} dias";
    }

    public LembrancaConsolidada? ObterVersaoMaisRecente(string chave)
    {
        return _estado.LembrancasLongoPrazo
            .Where(l => l.Chave == chave)
            .OrderByDescending(l => l.UltimaAtualizacaoBrasilSp)
            .FirstOrDefault();
    }

    public void Persistir()
    {
        var json = JsonSerializer.Serialize(_estado, OpcoesJson);
        File.WriteAllText(_caminhoArquivo, json, PadroesSerializacao.Utf8SemBom);
    }

    private static EstadoMemoriaConversacional CriarEstadoPadrao(
        DateTimeOffset? criadoEmBrasilSp,
        PerfilIdentidadeConversacional? identidadePadrao = null)
    {
        var estado = new EstadoMemoriaConversacional
        {
            CriadoEmBrasilSp = criadoEmBrasilSp ?? RelogioBrasil.AgoraBrasilSp()
        };
        AplicarIdentidadePadrao(estado, identidadePadrao);
        return estado;
    }

    private static void AplicarIdentidadePadrao(
        EstadoMemoriaConversacional estado,
        PerfilIdentidadeConversacional? identidadePadrao)
    {
        if (identidadePadrao is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(estado.Identidade.NomeModelo))
        {
            estado.Identidade.NomeModelo = identidadePadrao.NomeModelo;
        }

        if (string.IsNullOrWhiteSpace(estado.Identidade.AmbienteBase))
        {
            estado.Identidade.AmbienteBase = identidadePadrao.AmbienteBase;
        }

        if (string.IsNullOrWhiteSpace(estado.Identidade.DiferencaEuUsuario))
        {
            estado.Identidade.DiferencaEuUsuario = identidadePadrao.DiferencaEuUsuario;
        }

        if (string.IsNullOrWhiteSpace(estado.Identidade.MissaoBase))
        {
            estado.Identidade.MissaoBase = identidadePadrao.MissaoBase;
        }
    }

    private static string FormatarTurno(TurnoMemoriaConversacional turno)
    {
        var papel = turno.Papel == "oria" ? "Ôria" : "usuário";
        return $"{papel}: {turno.Texto}";
    }

    private static void SanitizarEstado(EstadoMemoriaConversacional estado)
    {
        var turnosFiltrados = estado.Turnos
            .Select(turno => new TurnoMemoriaConversacional
            {
                MomentoBrasilSp = turno.MomentoBrasilSp,
                Papel = turno.Papel,
                Texto = TreinadorTokenizadorBpe.NormalizarTexto(turno.Texto)
            })
            .Where(turno => !TextoEstaDegradado(turno.Texto))
            .TakeLast(400)
            .ToList();

        estado.Turnos.Clear();
        estado.Turnos.AddRange(turnosFiltrados);

        var lembrancasFiltradas = estado.LembrancasLongoPrazo
            .Where(lembranca => !TextoEstaDegradado(lembranca.Texto))
            .ToList();

        foreach (var lembranca in lembrancasFiltradas)
        {
            lembranca.Texto = NormalizarFraseMemoria(TreinadorTokenizadorBpe.NormalizarTexto(lembranca.Texto));
            if (lembranca.FormasChave.Count == 0)
            {
                lembranca.FormasChave.AddRange(
                    ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(lembranca.Texto)
                        .Distinct(StringComparer.Ordinal)
                        .Take(8));
            }
        }

        estado.LembrancasLongoPrazo.Clear();
        estado.LembrancasLongoPrazo.AddRange(lembrancasFiltradas);
    }

    private static int PontuarTexto(HashSet<string> formasAtivas, string texto, IReadOnlyCollection<string>? formasConhecidas = null)
    {
        if (formasAtivas.Count == 0)
        {
            return 0;
        }

        var formasTexto = formasConhecidas?.Count > 0
            ? formasConhecidas
            : ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(texto);
        var comuns = formasTexto.Count(formasAtivas.Contains);
        return comuns * 10;
    }

    public static bool TextoEstaDegradado(string texto)
    {
        var textoNormalizado = TreinadorTokenizadorBpe.NormalizarTexto(texto);
        if (string.IsNullOrWhiteSpace(textoNormalizado))
        {
            return true;
        }

        var tokens = TreinadorTokenizadorBpe.PreTokenizar(textoNormalizado);
        if (tokens.Count == 0)
        {
            return true;
        }

        if (TextoPareceContextoInterno(textoNormalizado))
        {
            return true;
        }

        if (tokens.All(x => Regex.IsMatch(x, @"^\p{P}+$", RegexOptions.CultureInvariant)))
        {
            return true;
        }

        var formas = ConstrutorMemoriaEpistemica.ExtrairFormasConteudo(textoNormalizado)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var contemSinaisDeArquivoOuCaminho = TextoPareceArquivoOuCaminho(textoNormalizado);
        var contemIdentificadorComUnderscore = Regex.IsMatch(
            textoNormalizado,
            @"\b[\p{L}\p{N}]+(?:_[\p{L}\p{N}]+)+\b",
            RegexOptions.CultureInvariant);

        if (formas.Count == 0 && tokens.Count <= 3 && !contemSinaisDeArquivoOuCaminho && !contemIdentificadorComUnderscore)
        {
            return true;
        }

        var quantidadeUnderscores = textoNormalizado.Count(c => c == '_');
        if (quantidadeUnderscores >= 2 &&
            !contemSinaisDeArquivoOuCaminho &&
            !(contemIdentificadorComUnderscore && formas.Count >= 1))
        {
            return true;
        }

        if (tokens.Count >= 6 && formas.Count <= 1 && !contemSinaisDeArquivoOuCaminho)
        {
            return true;
        }

        var repeticaoMaxima = tokens
            .GroupBy(x => x, StringComparer.Ordinal)
            .Select(x => x.Count())
            .DefaultIfEmpty(0)
            .Max();

        if (repeticaoMaxima >= 4 && repeticaoMaxima >= Math.Ceiling(tokens.Count / 2.0))
        {
            return true;
        }

        if (!contemSinaisDeArquivoOuCaminho &&
            (TemNgramaRepetido(tokens, tamanhoNgrama: 2, repeticoesMinimas: 3) ||
             TemNgramaRepetido(tokens, tamanhoNgrama: 3, repeticoesMinimas: 2)))
        {
            return true;
        }

        var diversidadeLexica = tokens
            .Distinct(StringComparer.Ordinal)
            .Count() / (double)tokens.Count;

        return tokens.Count >= 8 &&
               diversidadeLexica < 0.45 &&
               formas.Count <= 4 &&
               !contemSinaisDeArquivoOuCaminho;
    }

    private static bool TextoPareceArquivoOuCaminho(string textoNormalizado)
    {
        return textoNormalizado.Contains('\\') ||
               textoNormalizado.Contains('/') ||
               Regex.IsMatch(textoNormalizado, @"\b[\w\-]+\.(json|txt|md|cs|mmd|log)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool TextoPareceContextoInterno(string textoNormalizado)
    {
        string[] rotulosInternos =
        [
            "fatos relacionados :",
            "contexto de ferramentas :",
            "arquivo referenciado :",
            "ocorrencias em arquivos :",
            "pagina web :",
            "resultados web :",
            "memoria curta :",
            "memoria media :",
            "memoria longa :",
            "identidade :",
            "tempo de vida :",
            "ambiente :",
            "eu e usuario :",
            "missao :",
            "contexto cognitivo :",
            "prompt interno :",
            "fato atual confirmado pelo sistema :"
        ];

        return rotulosInternos.Any(rotulo =>
            textoNormalizado.StartsWith(rotulo, StringComparison.Ordinal) ||
            textoNormalizado.Contains(" " + rotulo, StringComparison.Ordinal));
    }

    private static bool TemNgramaRepetido(
        IReadOnlyList<string> tokens,
        int tamanhoNgrama,
        int repeticoesMinimas)
    {
        if (tokens.Count < tamanhoNgrama * repeticoesMinimas)
        {
            return false;
        }

        var frequencias = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var indice = 0; indice <= tokens.Count - tamanhoNgrama; indice++)
        {
            var janela = tokens.Skip(indice).Take(tamanhoNgrama).ToList();
            if (janela.All(x => x.Length == 1 && char.IsPunctuation(x[0])))
            {
                continue;
            }

            var chave = string.Join("\u001F", janela);
            frequencias[chave] = frequencias.TryGetValue(chave, out var atual) ? atual + 1 : 1;
            if (frequencias[chave] >= repeticoesMinimas)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> QuebrarEmFrases(string textoNormalizado)
    {
        return Regex.Split(textoNormalizado, @"(?<=[.!?])\s+")
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string NormalizarFraseMemoria(string frase)
    {
        var fraseNormalizada = frase.Trim();
        if (string.IsNullOrWhiteSpace(fraseNormalizada))
        {
            return string.Empty;
        }

        if (!fraseNormalizada.EndsWith(".", StringComparison.Ordinal) &&
            !fraseNormalizada.EndsWith("!", StringComparison.Ordinal) &&
            !fraseNormalizada.EndsWith("?", StringComparison.Ordinal))
        {
            fraseNormalizada += ".";
        }

        return fraseNormalizada;
    }

    private static string ConstruirChaveSemantica(IReadOnlyList<string> formas)
    {
        return "memoria:" + string.Join("|", formas.Take(6));
    }

    private static string FormatarTempoDeVida(DateTimeOffset criadoEm, DateTimeOffset agora)
    {
        if (agora <= criadoEm)
        {
            return "acabei de ser criada";
        }

        var diferenca = agora - criadoEm;
        if (diferenca.TotalDays >= 1)
        {
            return $"{(int)diferenca.TotalDays} dias, {diferenca.Hours} horas e {diferenca.Minutes} minutos";
        }

        if (diferenca.TotalHours >= 1)
        {
            return $"{(int)diferenca.TotalHours} horas e {diferenca.Minutes} minutos";
        }

        if (diferenca.TotalMinutes >= 1)
        {
            return $"{(int)diferenca.TotalMinutes} minutos";
        }

        return $"{Math.Max(1, (int)diferenca.TotalSeconds)} segundos";
    }

    private static double DiferencaEmMinutos(DateTimeOffset primeiro, DateTimeOffset segundo)
    {
        return Math.Abs((primeiro - segundo).TotalMinutes);
    }
}
