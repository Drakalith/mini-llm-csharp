# Mini LLM didático em C#

Este projeto é um mini-transformer `decoder-only` feito para estudo, em C#, sem framework de machine learning. Ele mostra o pipeline completo de um modelo autoregressivo pequeno: corpus, tokenização BPE, embeddings, autoatenção causal, treino, geração e chat com memória auxiliar.

O foco aqui é transparência. O sistema expõe artefatos intermediários, explica o hardware escolhido, salva o "cérebro" treinado e também mantém memória conversacional, grafo de conhecimento e ferramentas contextuais.

## O que este projeto é

- Um laboratório educacional para entender previsão do próximo token.
- Um transformer pequeno, com treino e retropropagação implementados no próprio projeto.
- Um sistema observável: quase toda etapa gera JSON em `artefatos/`.
- Um chat híbrido: geração + memória + evidência + ferramentas contextuais.

## O que este projeto não é

- Não é um LLM de produção.
- Não usa AdamW, batching grande, mixed precision, KV cache ou beam search.
- Não tenta competir com modelos modernos grandes.
- Não garante respostas factuais só pelo modelo; por isso existe a camada de evidência e ferramentas.

## Requisitos

- `.NET 8 SDK`
- Windows é o ambiente mais bem suportado hoje para diagnóstico de hardware
- GPU é opcional
- CUDA é opcional

O projeto inclui suporte experimental a GPU via `ILGPU`, mas o backend real depende do hardware disponível e da decisão de execução.

## Como rodar

Fluxo completo, didático e com inspeção passo a passo:

```powershell
dotnet run --project .\MiniLlmStudy.csproj
```

Chat direto com cérebro salvo:

```powershell
dotnet run --project .\MiniLlmStudy.csproj --chat
```

Atalhos aceitos para o modo direto:

- `chat`
- `--chat`
- `/chat`

## Dois modos de uso

### 1. Fluxo completo

Esse é o modo principal para estudar o sistema. Ele:

- analisa hardware
- verifica cérebro salvo compatível
- inspeciona corpus
- gera nuvem de palavras
- monta memória epistêmica
- treina ou carrega o tokenizador
- tokeniza o corpus
- monta exemplos de treino
- inicializa o modelo
- treina ou carrega o cérebro salvo
- mostra passagem direta
- mostra geração token por token
- mostra escala e recursos
- abre o chat interativo detalhado

Esse modo é o mais seguro quando você mudou corpus ou configuração, porque ele roda o diagnóstico de compatibilidade antes de decidir se reutiliza o cérebro salvo.

### 2. `--chat`

Esse é o modo rápido. Ele pula o pipeline didático e abre direto a conversa usando `artefatos/10_cerebro_treinado.json`.

Ponto importante: `--chat` agora faz um preflight de compatibilidade antes de conversar. Ele continua permissivo: avisa com destaque quando o cérebro salvo pode estar velho, mas não bloqueia a conversa.

Mais abaixo há uma seção só sobre o que o `--chat` realmente faz.

## Como o programa encontra os arquivos

Na inicialização, o programa sobe até 6 níveis de diretório procurando:

- `appsettings.json`
- `corpus/corpus.txt` ou, se ele não existir, `corpus/corpus.json`

Se `corpus.txt` existir, ele tem prioridade. O `json` só entra como fallback.

## Estrutura principal

```text
mini-llm-csharp/
├─ MiniLlmStudy.csproj
├─ appsettings.json
├─ corpus/
│  └─ corpus.txt
├─ artefatos/
├─ docs/
├─ src/
│  ├─ Cli/
│  ├─ Cognicao/
│  ├─ Config/
│  ├─ Data/
│  ├─ Diagnostico/
│  ├─ Ferramentas/
│  ├─ Generation/
│  ├─ IO/
│  ├─ Math/
│  ├─ Model/
│  ├─ Numerica/
│  ├─ Tokenization/
│  └─ Training/
└─ README.md
```

## Formato do corpus

O formato recomendado hoje é `corpus/corpus.txt`.

Regras do `corpus.txt`:

- linhas vazias são ignoradas
- linhas começando com `#` são comentários
- `[dominio: ...]` troca o domínio das linhas seguintes
- `[domínio: ...]` também é aceito
- se nenhum domínio for aberto, o padrão é `geral`
- cada linha válida vira uma entrada de corpus
- perguntas com várias respostas também podem ser escritas em bloco usando `pergunta: ...` seguido de `[respostas:` e fechado com `]`

Exemplo:

```text
# Comentário
[dominio: conversa-cotidiana]
pergunta: qual animal mia? resposta: gato.
pergunta: qual animal late? resposta: cachorro.

[dominio: identidade]
pergunta: qual e seu nome?
[respostas:
meu nome e oria.
eu me chamo oria.
pode me chamar de oria.
]

[dominio: percepcao]
eu vejo a grama.
pergunta: o que eu vejo? resposta: a grama.
```

Quando você usa o bloco `[respostas:]`, o carregador expande internamente aquilo em várias entradas `pergunta: ... resposta: ...`, uma por resposta. Assim, a sintaxe nova funciona sem quebrar o restante do pipeline.

Também existe fallback para `corpus/corpus.json`, usando uma lista de objetos com:

- `Identificador`
- `Dominio`
- `Texto`

## O que entra no treino de verdade

Esse ponto é importante porque ele afeta diretamente o que o modelo aprende.

### Domínio

O domínio continua existindo no sistema como metadado interno, memória e organização cognitiva, mas não entra mais no texto treinado. Em outras palavras, o modelo não aprende mais prefixos como:

```text
dominio : conversa_cotidiana
```

### `pergunta:` e `resposta:`

Se a linha do corpus estiver no formato:

```text
pergunta: ... resposta: ...
```

o sistema ainda trata isso como um exemplo estruturado de pergunta e resposta.

Hoje isso significa:

- `pergunta:` e `resposta:` ainda entram no texto usado para treino desses exemplos
- a perda do prefixo é zerada para focar o treino na parte da resposta
- texto livre continua sendo treinado como texto livre

Então o projeto hoje é "misto":

- aceita corpus totalmente livre
- aceita corpus com pares pergunta/resposta
- usa a estrutura de QA quando ela existe

## Como o chat interpreta a sua entrada

No chat, a interpretação não é invisível. Ela segue regras fixas:

- se você digitar algo vazio, a conversa encerra
- se você começar com `texto:`, isso vira continuação livre
- se você escrever `pergunta:` ou `resposta:` explicitamente, isso é tratado como estrutura explícita
- se você escrever uma frase comum, o sistema assume modo `pergunta`

Exemplos:

```text
qual animal mia?
```

Internamente tende a virar algo no formato:

```text
pergunta : qual animal mia ? resposta :
```

Já isto:

```text
texto: o gato gosta
```

vai como texto livre, sem embrulhar como pergunta.

## Configuração em `appsettings.json`

O arquivo `appsettings.json` controla tokenização, treino, geração e backend numérico.

Ponto importante: quando `autoConfiguracaoCorpus.ativar` está ligado, os valores do arquivo passam a ser a `configuração solicitada`, não necessariamente a `configuração efetiva`. O sistema analisa o corpus logo no início e pode recalcular várias opções automaticamente.

### Raiz

- `sementeAleatoria`: semente principal
- `maximoMesclasTokenizador`: máximo de merges do BPE
- `maximoComprimentoSequencia`: limite de tokens no contexto

### `modelo`

- `dimensaoEmbedding`
- `dimensaoFeedForward`
- `numeroCamadas`
- `numeroCabecasAtencao`
- `epsilonLayerNorm`
- `taxaAprendizado`
- `epocas`
- `maximoExemplosPorEpoca`
- `maximoPassosTreino`
- `embaralharExemplosPorEpoca`
- `usarParadaAntecipada`
- `pacienciaParadaAntecipada`
- `melhoraMinimaPerda`
- `treinoAdaptativoAutomatico`
- `tempoMaximoTreinoMinutos`
- `passosAmostraPlanejamento`
- `minimoExemplosPorEpocaAdaptado`

### `geracao`

- `maximoNovosTokens`
- `atrasoFluxoMs`
- `maximoAlternativasExibidas`
- `temperatura`: controla quão conservadora ou criativa fica a amostragem; `0` mantém modo guloso
- `topK`: limita a escolha aos `K` tokens mais prováveis; `0` desliga
- `topP`: nucleus sampling; mantém só a massa acumulada de probabilidade até `P`; `1.0` desliga

### `execucao`

- `treino`: `auto`, `cpu` ou `gpu`
- `inferencia`: `auto`, `cpu` ou `gpu`

### `autoConfiguracaoCorpus`

- `ativar`: quando `true`, o sistema lê o corpus e recalcula automaticamente as opções mais sensíveis ao conteúdo

Hoje a auto-configuração pode ajustar automaticamente:

- `maximoMesclasTokenizador`
- `maximoComprimentoSequencia`
- `modelo.dimensaoEmbedding`
- `modelo.dimensaoFeedForward`
- `modelo.numeroCamadas`
- `modelo.numeroCabecasAtencao`
- `modelo.taxaAprendizado`
- `modelo.epocas`
- `modelo.maximoExemplosPorEpoca`
- `modelo.embaralharExemplosPorEpoca`
- `modelo.usarParadaAntecipada`
- `modelo.pacienciaParadaAntecipada`
- `modelo.melhoraMinimaPerda`
- `modelo.minimoExemplosPorEpocaAdaptado`
- `geracao.maximoNovosTokens`
- `geracao.temperatura`
- `geracao.topK`
- `geracao.topP`

O relatório dessa leitura fica salvo em `artefatos/00_autoconfiguracao_corpus.json`.

## Parada antecipada

Agora existe uma chave explícita:

```json
"usarParadaAntecipada": false
```

Semântica atual:

- `usarParadaAntecipada: true` ativa early stopping
- `usarParadaAntecipada: false` desativa
- `pacienciaParadaAntecipada` define quantas épocas sem melhora relevante são toleradas
- `melhoraMinimaPerda` define o quanto a perda precisa cair para contar como melhora real

Se `usarParadaAntecipada` não existir, o código mantém compatibilidade com o comportamento antigo e usa `pacienciaParadaAntecipada > 0`.

## Treino adaptativo

O sistema tem um planejador que pode reduzir o plano de treino para caber no orçamento de tempo do hardware atual.

Quando `treinoAdaptativoAutomatico` está ligado, ele:

- mede uma amostra de passos num modelo sombra
- estima exemplos por segundo
- compara o plano pedido com o orçamento de tempo
- reduz épocas e/ou exemplos por época se necessário

Quando `treinoAdaptativoAutomatico` está desligado, ele usa o plano solicitado.

## Escolha de CPU e GPU

O projeto tem uma camada de adaptação de hardware.

Ela:

- detecta CPU, RAM e GPU
- tenta detectar CUDA
- pode rodar benchmark real para a carga atual
- escolhe backend de treino e backend de inferência

Valores aceitos em `execucao`:

```json
"execucao": {
  "treino": "auto",
  "inferencia": "auto"
}
```

Ou:

```json
"execucao": {
  "treino": "gpu",
  "inferencia": "cpu"
}
```

Mesmo em modo manual, o sistema continua explicando a escolha final. Se `gpu` for forçado mas não houver backend utilizável, ele cai para CPU e informa o motivo.

## O fluxo completo, etapa por etapa

O modo padrão percorre 13 macroetapas:

1. adaptação de hardware
2. diagnóstico do cérebro salvo
3. corpus
4. nuvem de palavras
5. memória epistêmica
6. tokenizador
7. sequências e exemplos de treino
8. inicialização do modelo
9. treino
10. passagem direta detalhada
11. geração token por token
12. escala e recursos
13. chat interativo detalhado

No final, o chat do fluxo completo é mais explicativo que o `--chat`: ele mostra análise pragmática, apoio no corpus, memórias curtas/médias/longas, contexto de ferramentas, prompt interno, top tokens e rastros de geração.

## Persistência do cérebro salvo

O cérebro treinado é salvo em:

```text
artefatos/10_cerebro_treinado.json
```

Ele guarda, entre outras coisas:

- modelo tokenizador
- hiperparâmetros do transformador
- pesos do modelo
- hashes de corpus e configuração
- perda final
- número de épocas
- timestamp de criação

### Como a compatibilidade é calculada

O diagnóstico compara:

- hash do corpus atual
- hash da configuração relevante do modelo

Se ambos baterem, o fluxo completo pode pular o treino e carregar o cérebro salvo.

### Importante sobre a mensagem `Corpus compativel: nao`

Quando não existe cérebro salvo ainda, o diagnóstico mostra:

- `Cerebro salvo encontrado: nao`
- `Corpus compativel: nao`
- `Configuracao compativel: nao`

Nesse caso, isso não quer dizer necessariamente que corpus e configuração "mudaram". Quer dizer apenas que não existe arquivo salvo para comparar.

## O que o `--chat` realmente faz

Esta é a parte mais importante para uso prático.

Quando você roda:

```powershell
dotnet run --project .\MiniLlmStudy.csproj --chat
```

o sistema:

- exige que `artefatos/10_cerebro_treinado.json` já exista
- carrega diretamente os pesos salvos
- não executa o pipeline didático completo
- não faz o diagnóstico completo de compatibilidade antes de conversar
- reanalisa o hardware para decidir o backend de inferência atual
- recria a memória epistêmica a partir do corpus atual
- extrai a identidade padrão a partir do corpus atual
- carrega ou cria a memória conversacional persistente
- carrega ou reconstrói o grafo de conhecimento persistente

Isso leva a uma consequência importante:

- os pesos vêm do cérebro salvo
- mas a identidade, a memória epistêmica, o grafo e o contexto auxiliar vêm do corpus atual

Então, se você mudou o corpus ou a configuração e não retreinou, o `--chat` pode conversar com pesos antigos usando memória e contexto novos.

### Regra prática

Se você alterou o corpus ou a configuração do modelo e quer coerência total, rode o fluxo completo ao menos uma vez antes de usar `--chat`.

## Memória conversacional persistente

O chat usa:

```text
artefatos/12_memoria_conversa.json
```

Essa memória guarda:

- identidade conversacional
- turnos recentes
- lembranças de longo prazo

Comportamento atual:

- turnos são limitados
- memórias longas também são limitadas
- apenas frases declarativas do usuário tendem a virar memória longa
- respostas degradadas não são persistidas

Além disso, o sistema mostra o "tempo de vida" da identidade com base na data de criação associada ao cérebro salvo.

## Grafo de conhecimento persistente

O chat também mantém:

```text
artefatos/13_grafo_conhecimento.json
```

Esse grafo:

- extrai triplas simples do corpus
- extrai triplas de turnos do usuário
- extrai triplas de memórias consolidadas
- recupera fatos relacionados ao prompt atual

Esses fatos podem ser injetados como contexto auxiliar antes da resposta.

## Ferramentas contextuais automáticas

O chat não depende só do modelo. Existe uma camada de ferramentas que pode injetar contexto observacional.

Hoje ela pode:

- responder perguntas de tempo e sistema
- ler arquivos citados na entrada
- buscar ocorrências em arquivos do projeto
- fazer busca web
- baixar e resumir uma página web
- recuperar fatos relacionados do grafo

Em geral:

- se você cita um arquivo, o sistema tenta localizar e ler
- se você cita URL, ele tenta baixar
- se você fala em `internet`, `web`, `site`, `artigo`, `noticia` ou `pesquise`, ele pode disparar busca web

## Ordem real de tentativa de resposta

No chat, a resposta não vem de uma única fonte. O sistema tenta combinar camadas.

Em alto nível:

1. evidência de ferramentas, quando forte
2. memória pergunta/resposta do corpus
3. exemplos de diálogo do tipo `usuário: ... Ôria: ...`
4. geração pelo modelo
5. fallback para prompt base
6. fallback com contexto de ferramentas

Além disso:

- se uma resposta gerada parecer degradada ou desalinhada, uma resposta baseada em evidência pode substituir a geração
- respostas degradadas normalmente não entram na memória persistente

## Geração

A geração atual é greedy.

Isso significa:

- o sistema escolhe sempre o token mais provável em cada passo
- não há temperatura, top-p ou beam search
- a saída pode ficar rígida ou repetir padrões em corpora pequenos

O gerador para quando:

- encontra o token especial de fim
- atinge `maximoNovosTokens`

## Artefatos gerados

Arquivos principais gerados pelo fluxo:

```text
artefatos/
├─ 00_adaptacao_hardware.json
├─ 00b_persistencia_cerebro.json
├─ 01_corpus.json
├─ 01a_nuvem_palavras.json
├─ 01b_memoria_epistemica.json
├─ 02_modelo_tokenizador.json
├─ 02_sequencias_tokenizadas.json
├─ 03_exemplos_treino.json
├─ 04_retrato_modelo.json
├─ 05a_plano_treino.json
├─ 05_log_treino.json
├─ 06a_analise_prompt_epistemica.json
├─ 06_passagem_direta.json
├─ 07_rastro_geracao.json
├─ 08_relatorio_escala.json
├─ 09_medicoes_recursos.json
├─ 10_cerebro_treinado.json
├─ 11_monitoramento_execucao.json
├─ 12_memoria_conversa.json
└─ 13_grafo_conhecimento.json
```

Resumo rápido:

- `00*`: hardware e persistência
- `01*`: corpus e camada cognitiva inicial
- `02*` e `03`: tokenização e exemplos de treino
- `04` e `05*`: modelo e treino
- `06*` e `07`: análise do prompt e geração
- `08` e `09`: escala e consumo de recursos
- `10`: cérebro salvo
- `11`: monitoramento contínuo
- `12`: memória persistente da conversa
- `13`: grafo de conhecimento persistente

## Limitações atuais

- Corpus muito pequeno gera comportamento frágil
- Geração greedy pode soar mecânica
- `pergunta:` e `resposta:` ainda fazem parte do protocolo textual dos exemplos estruturados
- `--chat` não protege você automaticamente contra incompatibilidade entre pesos antigos e corpus atual
- O backend GPU é experimental

## Sugestão de uso para estudar

1. Rode o fluxo completo.
2. Leia os artefatos JSON.
3. Edite o `corpus/corpus.txt`.
4. Mude merges do tokenizador.
5. Mude embedding, camadas e taxa de aprendizado.
6. Repare como muda a tokenização, a perda e o chat.
7. Depois use `--chat` só quando já houver um cérebro salvo coerente com seu corpus atual.

## Arquivos úteis para editar

- `corpus/corpus.txt`
- `appsettings.json`

Exemplo de execução:

```powershell
dotnet run --project .\MiniLlmStudy.csproj
dotnet run --project .\MiniLlmStudy.csproj --chat
```
