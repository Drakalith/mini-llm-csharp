# Varredura de Ferramentas do OpenClaude para portar para C#

## Objetivo

Este arquivo resume a varredura feita em `C:\Users\Drakalith\Desktop\NS\openclaude-0.1.8` para decidir o que realmente vale portar para a Oria em `.cs`.

O foco aqui nao e copiar o OpenClaude inteiro. O foco e separar:

- o que entrega capacidade real para a Oria
- o que e so infraestrutura de CLI/agente
- o que ja nasce muito acoplado ao ecossistema TS/Bun

## Fontes varridas

- `README.md`
- `src/tools.ts`
- `src/Tool.ts`
- `src/tools/WebSearchTool/WebSearchTool.ts`
- `src/tools/WebFetchTool/WebFetchTool.ts`
- `src/tools/PowerShellTool/PowerShellTool.tsx`
- `src/tools/FileReadTool/FileReadTool.ts`
- `src/tools/GrepTool/GrepTool.ts`
- `repo/oria/memory.py`
- `repo/oria/knowledge_graph.py`
- `repo/oria/reasoning.py`
- `repo/oria/pipeline.py`
- `repo/oria/decoder.py`
- `repo/oria/instr_parser.py`
- `repo/oria/creative.py`
- `repo/oria/lsa.py`
- `repo/oria/sdm.py`
- `repo/oria/vsa.py`
- `repo/oria/orelha.py`
- `python/smart_router.py`

## Criterio de decisao

Uma ferramenta entra como candidata forte quando atende pelo menos um destes pontos:

- da grounding real para a Oria
- ajuda a responder fatos atuais sem inventar
- melhora memoria, conhecimento ou raciocinio
- ajuda a Oria a inspecionar o proprio ambiente
- pode ser portada para C# sem arrastar metade do runtime do OpenClaude

## Resumo executivo

### Portar agora

1. `PowerShellTool`
2. `FileReadTool`
3. `GlobTool`
4. `GrepTool`
5. `WebSearchTool`
6. `WebFetchTool`
7. `KnowledgeGraph`
8. `HierarchicalMemory`
9. `ReasoningEngine`
10. `Orelha`
11. `InstrParser`

### Portar depois

1. `FileWriteTool`
2. `FileEditTool`
3. `TodoWriteTool`
4. `LSPTool`
5. `LSA`
6. `SDM`
7. `VSA`
8. `Decoder`
9. `CreativeEngine`
10. `OriaPipeline`
11. `SmartRouter`
12. `NotebookEditTool`
13. `MCP` resources

### Nao portar agora

1. `AgentTool`
2. `EnterPlanModeTool`
3. `ExitPlanModeTool`
4. `EnterWorktreeTool`
5. `ExitWorktreeTool`
6. `TaskCreateTool`, `TaskGetTool`, `TaskListTool`, `TaskUpdateTool`, `TaskStopTool`, `TaskOutputTool`
7. `TeamCreateTool`, `TeamDeleteTool`
8. `RemoteTriggerTool`
9. `ScheduleCronTool`
10. `WorkflowTool`
11. `SendMessageTool`
12. `BriefTool`
13. `SkillTool`
14. `ToolSearchTool`
15. `ListMcpResourcesTool`, `ReadMcpResourceTool`
16. `SyntheticOutputTool`
17. `AskUserQuestionTool`
18. `ConfigTool`
19. `McpAuthTool`
20. `REPLTool`
21. `SuggestBackgroundPRTool`
22. `TungstenTool`
23. `VerifyPlanExecutionTool`
24. `SleepTool`

## Inventario das ferramentas de runtime

| Ferramenta | Status nesta copia | O que faz | Valor para a Oria | Dificuldade de portar | Decisao |
| --- | --- | --- | --- | --- | --- |
| `PowerShellTool` | real | executa comandos PowerShell com validacao e progresso | altissimo no Windows; hora, data, processo, memoria, arquivos, ambiente | media | portar agora |
| `BashTool` | real | shell POSIX | util so se a Oria precisar ser multi-plataforma de verdade | media | depois |
| `FileReadTool` | real | le arquivos com limites, ranges e tratamento especial para pdf/imagem/notebook | altissimo; grounding interno do projeto | media | portar agora |
| `FileWriteTool` | real | cria arquivos | util para autonomia futura | baixa | depois |
| `FileEditTool` | real | edita arquivos | util para agir no projeto | media | depois |
| `GlobTool` | real | busca arquivos por padrao | alto; ajuda a Oria a localizar memoria, corpus, artefatos e codigo | baixa | portar agora |
| `GrepTool` | real | busca conteudo por regex | alto; recuperacao factual dentro do workspace | baixa | portar agora |
| `NotebookEditTool` | real | edita notebooks | pouco valor imediato para a Oria atual | media | depois |
| `WebSearchTool` | real | pesquisa na web por busca nativa, DDG, Firecrawl ou Codex responses | altissimo para fatos atuais e aprendizado assistido | media | portar agora |
| `WebFetchTool` | real | baixa pagina e extrai markdown/resumo | altissimo para consolidar fatos web | media | portar agora |
| `TodoWriteTool` | real | mantem lista de tarefas | util para planejador interno, nao para conversa basica | baixa | depois |
| `LSPTool` | real mas condicional | usa linguagem/IDE para navegacao de codigo | muito util se a Oria for virar assistente de codigo | alta | depois |
| `MCPTool` | real | executa tools MCP | poderoso, mas grande demais para primeira fase | alta | depois |
| `ListMcpResourcesTool` | real | lista recursos MCP | vale apenas com MCP rodando | media | nao agora |
| `ReadMcpResourceTool` | real | le recurso MCP | vale apenas com MCP rodando | media | nao agora |
| `ToolSearchTool` | real | pesquisa tools quando ha muitas | infraestrutura do CLI, nao capacidade nuclear da Oria | media | nao agora |
| `AgentTool` | real | spawn de subagentes | pesado demais para esta fase | alta | nao agora |
| `Task*` | real ou condicional | fluxo de tarefas internas do CLI | organizacao do OpenClaude, nao do cerebro da Oria | media | nao agora |
| `Team*` | condicional | cria equipes/agentes coordenados | muito acoplado a swarm/coordinator mode | alta | nao agora |
| `EnterPlanModeTool` | real | troca o modo do agente | infraestrutura de UX | baixa | nao agora |
| `ExitPlanModeTool` | real | sai do modo plano | infraestrutura de UX | baixa | nao agora |
| `EnterWorktreeTool` | condicional | entra em worktree | muito especifico do workflow do CLI | media | nao agora |
| `ExitWorktreeTool` | condicional | sai de worktree | muito especifico do workflow do CLI | media | nao agora |
| `RemoteTriggerTool` | condicional | aciona trigger remota | nao agrega cognicao | alta | nao agora |
| `ScheduleCronTool` | condicional | agenda automacoes | interessante no futuro, mas nao para primeira conversao | media | nao agora |
| `WorkflowTool` | condicional | roda scripts de workflow | muito acoplado a automacao do CLI | media | nao agora |
| `SendMessageTool` | real | envia mensagem entre agentes | infraestrutura de multiagente | alta | nao agora |
| `BriefTool` | real | resume/brief de contexto | pode ser reproduzido depois de forma propria | baixa | nao agora |
| `SkillTool` | real | carrega skills locais | ecossistema do OpenClaude, nao parte essencial da Oria | media | nao agora |
| `AskUserQuestionTool` | real | questiona o usuario de forma estruturada | bom para UX, mas nao e nucleo cognitivo | baixa | nao agora |
| `ConfigTool` | ant-only | mexe em configuracoes do app | valor baixo para a Oria | baixa | nao agora |
| `McpAuthTool` | pasta existe, mas nao apareceu no pool principal | autenticacao MCP | muito especifico do runtime TS | media | nao agora |
| `REPLTool` | nulo nesta copia | wrapper REPL | nao ha o que portar agora | alta | nao agora |
| `SuggestBackgroundPRTool` | nulo nesta copia | PR em background | sem valor para a Oria atual | alta | nao agora |
| `TungstenTool` | nulo nesta copia | placeholder interno | sem valor | alta | nao agora |
| `VerifyPlanExecutionTool` | nulo/feature-gated | checa execucao de plano | infraestrutura de QA interna | media | nao agora |
| `SleepTool` | condicional | dorme/espera | utilitario pequeno, mas nao prioritario | baixa | nao agora |
| `SyntheticOutputTool` | interno | output sintetico do runtime | nao e capacidade da Oria | media | nao agora |

## Inventario dos modulos cognitivos Python em `repo/oria`

| Modulo | O que faz | Valor para a Oria | Dificuldade de portar | Decisao |
| --- | --- | --- | --- | --- |
| `memory.py` | memoria hierarquica em 4 niveis: trabalho, episodica, semantica, meta | altissimo; casa com o que voce quer de curto, medio e longo prazo | media | portar agora |
| `knowledge_graph.py` | grafo sujeito-relacao-objeto com extracao regex pt-br | altissimo; permite fatos persistentes e relacoes | media | portar agora |
| `reasoning.py` | aritmetica e verificacao de fato via KG | alto; raciocinio leve e verificavel | baixa | portar agora |
| `orelha.py` | classificador de intencao, entidades, polaridade e tema | alto; melhora roteamento conversacional | baixa | portar agora |
| `instr_parser.py` | parse de instrucoes complexas, estilo, forma e restricoes | alto; deixa a Oria entender pedidos mais ricos | baixa | portar agora |
| `vsa.py` | simbolos vetoriais de alta dimensao | medio; bom para episodios e composicao semantica | media | depois |
| `sdm.py` | memoria distribuida esparsa por similaridade | medio/alto; muito interessante, mas aumenta complexidade | media | depois |
| `lsa.py` | semantica por co-ocorrencia + SVD | medio; bom para busca semantica local sem depender de LLM | alta | depois |
| `decoder.py` | transforma vetores/fatos em frases | medio; util mais como fallback gerativo simbolico | media | depois |
| `creative.py` | geracao criativa nao neural | baixo/medio; legal, mas nao e o problema principal da Oria agora | media | depois |
| `pipeline.py` | orquestra todos os modulos | alto no longo prazo, mas so depois de portar os blocos base | media | depois |
| `orelha.py` + `instr_parser.py` + `reasoning.py` | juntos formam um pre-frontal leve | altissimo como desenho | baixa/media | portar agora em blocos separados |

## Modulo extra fora de `repo/oria`

| Modulo | O que faz | Valor para a Oria | Dificuldade | Decisao |
| --- | --- | --- | --- | --- |
| `python/smart_router.py` | roteia requests entre providers por latencia, custo e saude | util se a Oria tiver mais de um backend/model provider | media | depois |

## O que faz mais sentido portar primeiro para C#

### Fase 1 - grounding e memoria

1. `PowerShellTool`
2. `FileReadTool`
3. `GlobTool`
4. `GrepTool`
5. `WebSearchTool`
6. `WebFetchTool`
7. `KnowledgeGraph`
8. `HierarchicalMemory`
9. `ReasoningEngine`
10. `Orelha`
11. `InstrParser`

Por que:

- da acesso ao mundo real
- da acesso ao proprio projeto
- tira da rede neural o peso de saber hora, data e fatos atuais
- cria memoria factual e relacional de verdade
- permite respostas mais verificaveis

### Fase 2 - semantica simbolica mais forte

1. `VSA`
2. `SDM`
3. `LSA`
4. `Decoder`
5. `CreativeEngine`
6. `OriaPipeline`

Por que:

- melhora recuperacao por similaridade
- ajuda a montar memoria episodica melhor
- cria uma segunda camada de conhecimento alem do mini-transformer

### Fase 3 - autonomia e ecossistema

1. `FileWriteTool`
2. `FileEditTool`
3. `TodoWriteTool`
4. `LSPTool`
5. `MCP` resources
6. `SmartRouter`

Por que:

- aqui a Oria deixa de ser so conversacional e vira agente util
- mas essa fase so vale depois que grounding, memoria e raciocinio estiverem estaveis

## Ferramentas que parecem boas mas enganariam a gente agora

### Multiagente e coordenacao

- `AgentTool`
- `Team*`
- `SendMessageTool`
- `Task*`
- `PlanMode`

Problema:

- isso aumenta muito a complexidade
- nao resolve o defeito atual da Oria
- sem memoria e conhecimento fortes, so cria mais ruido

### Worktree, cron e workflow

- `EnterWorktreeTool`
- `ExitWorktreeTool`
- `ScheduleCronTool`
- `WorkflowTool`

Problema:

- sao ferramentas de operacao do CLI
- nao sao cognitivas
- podem entrar so quando a Oria estiver madura como agente

### Null tools ou tools acopladas demais ao runtime

- `REPLTool`
- `TungstenTool`
- `SuggestBackgroundPRTool`
- `VerifyPlanExecutionTool`
- `SyntheticOutputTool`

Problema:

- nao entregam capacidade concreta para a Oria nesta copia
- ou ja estao nulas
- ou dependem demais do runtime TS/Bun

## Decisao final

Se a conversao comecar agora, a ordem correta e:

1. `SystemTools` em C# com foco em PowerShell, relogio e metricas do sistema
2. `FileTools` em C# com leitura, glob e grep
3. `WebSearchService` e `WebFetchService`
4. `KnowledgeGraph` em C#
5. `MemoriaHierarquica` em C#
6. `ReasoningEngine` em C#
7. `Orelha` e `InstrParser` em C#

Isso entrega o maior ganho real para a Oria com o menor custo de conversao.

## Proxima conversao recomendada

O primeiro pacote em `.cs` deveria nascer assim:

- `src/Ferramentas/SystemTool.cs`
- `src/Ferramentas/FileReadTool.cs`
- `src/Ferramentas/FileSearchTool.cs`
- `src/Ferramentas/WebSearchTool.cs`
- `src/Ferramentas/WebFetchTool.cs`
- `src/Cognicao/KnowledgeGraph.cs`
- `src/Cognicao/MemoriaHierarquica.cs`
- `src/Cognicao/ReasoningEngine.cs`
- `src/Cognicao/Orelha.cs`
- `src/Cognicao/InstrParser.cs`

Essa e a menor conversao que ja muda a qualidade cognitiva da Oria de forma mensuravel.
