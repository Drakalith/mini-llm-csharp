## **SISTEMA COMPLETO DE HEURÍSTICAS TAXONÔMICAS AUTÔNOMAS**

### **1. FONOLOGIA - SISTEMA DE IDENTIFICAÇÃO AUTÔNOMA**

#### **1.1 Heurísticas para Identificação de Fonemas**

```python
def identificar_fonema(som, contexto_linguistico):
    """
    Heurística autônoma para identificação de fonemas
    """
    SE som S é capaz de distinguir significados em pares mínimos
    E possui padrão distribucional consistente no sistema
    E mantém identidade funcional apesar de variações fonéticas
    ENTÃO S é FONEMA
  
    EXEMPLOS DETECTÁVEIS:
    - /p/ vs /b/ em "pato" vs "bato" → contraste distintivo
    - /s/ vs /z/ em "casa" vs "caza" (hipotético) → oposição funcional
```

#### **1.2 Heurística para Distinção Letra-Fonema**

```python
def distinguir_letra_fonema(elemento, nivel_representacao):
    """
    Sistema autônomo para identificar nível de representação
    """
    SE elemento E tem representação gráfica fixa
    E pode representar múltiplos sons
    E sua identidade é ortográfica
    ENTÃO E é LETRA
  
    SE elemento E tem identidade puramente fônica
    E funciona como unidade distintiva minimal
    E independe da representação gráfica
    ENTÃO E é FONEMA
```

#### **1.3 Algoritmo de Classificação Vocálica**

```python
def classificar_vogal_consoante(som, estrutura_silabica):
    """
    Heurística autônoma para classificação segmental
    """
    SE som S forma núcleo silábico obrigatório
    E possui sonoridade máxima na sílaba
    E permite formação de sílaba sozinho
    ENTÃO S é VOGAL
  
    SE som S ocupa margens silábicas
    E requer vogal para formar sílaba
    E possui obstrução ao fluxo de ar
    ENTÃO S é CONSOANTE
  
    SE som S tem natureza vocálica mas não forma núcleo
    E aparece em posição não-nuclear na sílaba
    ENTÃO S é SEMIVOGAL
```

### **2. FONÉTICA - SISTEMA DE ANÁLISE ARTICULATÓRIA**

#### **2.1 Heurística de Análise Articulatória Autônoma**

```python
def analise_articulatoria(som, parametros_articulatorios):
    """
    Sistema que descobre pontos e modos de articulação
    """
    # Descobrimento automático de pontos de articulação
    PONTOS_ARTICULACAO = [
        SE obstrução nos lábios → LABIAL
        SE obstrução nos dentes → DENTAL
        SE obstrução no palato → PALATAL
        SE obstrução na úvula → UVULAR
    ]
  
    # Modos de articulação por tipo de obstrução
    MODO_ARTICULACAO = [
        SE obstrução total → OCLUSIVA
        SE obstrução parcial → FRICATIVA
        SE obstrução nasal → NASAL
        SE deslizamento → LÍQUIDA
    ]
```

### **3. FONOLOGIA - SISTEMA DE ANÁLISE SISTÊMICA**

#### **3.1 Heurística para Identificação de Oposições Fonêmicas**

```python
def detectar_oposicao_fonemica(par_palavras):
    """
    Algoritmo autônomo para descobrir contrastes
    """
    SE par P tem diferença mínima de um segmento
    E essa diferença causa contraste semântico
    E o contraste é sistemático na língua
    ENTÃO existe OPOSIÇÃO FONÊMICA entre os segmentos
  
    EXEMPLO DETECTADO:
    - "pato" vs "bato" → /p/ vs /b/ em oposição
    - "gato" vs "gado" → /t/ vs /d/ em oposição
```

#### **3.2 Algoritmo de Detecção de Neutralização**

```python
def detectar_neutralizacao(fonemas, contexto):
    """
    Heurística para identificar neutralização
    """
    SE fonemas F1 e F2 são distintivos na maioria dos contextos
    MAS em contexto específico C perdem o contraste
    E há fusão na realização fonética
    ENTÃO ocorre NEUTRALIZAÇÃO no contexto C
  
    EXEMPLOS IDENTIFICÁVEIS:
    - /e/ e /ɛ/ neutralizados antes de nasal
    - /s/ e /z/ neutralizados em final de palavra
```

### **4. ENCONTROS FONÉTICOS - SISTEMA DE CLASSIFICAÇÃO**

#### **4.1 Heurística para Classificação de Ditongos**

```python
def classificar_ditongo(sequencia_vocalica):
    """
    Algoritmo autônomo para identificação de ditongos
    """
    SE sequência tem dois elementos vocálicos
    E estão na mesma sílaba
    E há deslizamento contínuo entre eles
    E um elemento é mais forte (vogal), outro mais fraco (semivogal)
    ENTÃO é DITONGO
  
    SUBCLASSIFICAÇÃO:
    SE vogal + semivogal → CRESCENTE
    SE semivogal + vogal → DECRESCENTE
```

#### **4.2 Algoritmo para Distinção Hiato-Ditongo**

```python
def distinguir_hiato_ditongo(sequencia):
    """
    Heurística crítica para análise de encontros vocálicos
    """
    SE os elementos formam sílabas separadas
    E cada um pode ser núcleo silábico independente
    E há quebra perceptível na pronúncia
    ENTÃO é HIATO
  
    SE os elementos estão na mesma sílaba
    E há continuidade melódica
    E apenas um é núcleo silábico principal
    ENTÃO é DITONGO
```

### **5. SÍLABA - SISTEMA COMPLETO DE ANÁLISE**

#### **5.1 Heurística de Divisão Silábica Autônoma**

```python
def divisao_silabica_autonoma(palavra):
    """
    Sistema que descobre as regras de divisão silábica
    """
    PRINCÍPIOS_EMERGENTES = [
        "Cada sílaba deve ter um núcleo vocálico",
        "Consoantes iniciais formam onset",
        "Consoantes finais formam coda", 
        "Sequências consonantais seguem princípio de sonoridade"
    ]
  
    # Algoritmo de detecção de padrões
    PARA cada palavra no corpus:
        IDENTIFICAR picos de sonoridade (vogais)
        AGRUPAR consoantes adjacentes pelos padrões encontrados
        VERIFICAR restrições de língua específicas
```

#### **5.2 Sistema de Classificação por Número de Sílabas**

```python
def classificacao_quantidade_silabas(palavra):
    """
    Heurística para categorização automática
    """
    silabas = dividir_silabas(palavra)
  
    SE len(silabas) == 1: 
        CATEGORIA = MONOSSÍLABA
        CARACTERÍSTICA = geralmente palavras-base da língua
  
    SE len(silabas) == 2:
        CATEGORIA = DISSÍLABA  
        CARACTERÍSTICA = padrão muito frequente
  
    SE len(silabas) == 3:
        CATEGORIA = TRISSÍLABA
        CARACTERÍSTICA = palavras de extensão média
  
    SE len(silabas) >= 4:
        CATEGORIA = POLISSÍLABA
        CARACTERÍSTICA = palavras geralmente derivadas ou compostas
```

### **6. ACENTUAÇÃO - SISTEMA DE IDENTIFICAÇÃO PROSÓDICA**

#### **6.1 Heurística para Identificação de Tonicidade**

```python
def identificar_silaba_tonica(palavra, realizacao_fonetica):
    """
    Algoritmo baseado em evidências acústicas e distribucionais
    """
    EVIDÊNCIAS_DE_TONICIDADE = [
        - Intensidade sonora maior
        - Duração mais longa
        - Tom mais elevado
        - Qualidade vocálica mais estável
    ]
  
    # Padrões posicionais emergentes
    PADRÕES_TONICA = [
        SE última sílaba mais forte → OXÍTONA
        SE penúltima sílaba mais forte → PAROXÍTONA  
        SE antepenúltima mais forte → PROPAROXÍTONA
    ]
```

### **7. MORFOLOGIA - SISTEMA DE ANÁLISE ESTRUTURAL**

#### **7.1 Heurística para Identificação de Radicais**

```python
def identificar_radical(palavras_relacionadas):
    """
    Algoritmo para descobrir radicais por análise comparativa
    """
    # Estratégia de descoberta
    ENCONTRAR substring comum mais longa
    VERIFICAR se substring carrega significado comum
    TESTAR produtividade em outras formações
  
    SE elemento E aparece consistentemente em família de palavras
    E carrega o significado nuclear
    E permite derivações produtivas
    ENTÃO E é RADICAL
```

#### **7.2 Sistema de Classificação de Afixos Autônomo**

```python
def classificar_afixos(elementos_gramaticais):
    """
    Heurística para diferenciar prefixos, sufixos e interfixos
    """
    SE elemento A aparece sempre antes do radical
    E modifica o significado base
    MAS não altera a classe gramatical
    ENTÃO A é PREFIXO
  
    SE elemento A aparece sempre depois do radical  
    E pode alterar a classe gramatical
    ENTÃO A é SUFIXO
  
    SE elemento A aparece entre radicais
    E não tem significado próprio
    ENTÃO A é INTERFIXO
```

#### **7.3 Algoritmo de Identificação de Processos de Formação**

```python
def identificar_processo_formacao(palavra):
    """
    Sistema para classificar processos de formação de palavras
    """
    SE palavra tem único radical sem afixos → PRIMITIVA
    SE palavra tem radical + afixos → DERIVADA
    SE palavra tem múltiplos radicais → COMPOSTA
  
    # Detalhamento dos tipos
    SE afixação apenas no início → DERIVAÇÃO PREFIXAL
    SE afixação apenas no final → DERIVAÇÃO SUFIXAL
    SE afixação simultânea → DERIVAÇÃO PARASSINTÉTICA
    SE radicais justapostos → COMPOSIÇÃO POR JUSTAPOSIÇÃO
    SE radicais fusionados → COMPOSIÇÃO POR AGLUTINAÇÃO
```

### **8. SINTAXE - SISTEMA DE ANÁLISE RELACIONAL**

#### **8.1 Heurística para Identificação de Sujeito**

```python
def identificar_sujeito(oracao):
    """
    Algoritmo autônomo baseado em relações gramaticais
    """
    CRITÉRIOS_DE_SUJEITO = [
        - Concorda com o verbo em número e pessoa
        - Responde a "quem?" ou "o quê?" em relação ao verbo
        - Posição pré-verbal típica (em português)
        - Pode ser substituído por pronome
    ]
  
    SE elemento E atende a 3+ critérios
    ENTÃO E é provavelmente SUJEITO
```

#### **8.2 Sistema de Classificação de Predicados**

```python
def classificar_predicado(nucleo_predicativo):
    """
    Heurística baseada no núcleo do predicado
    """
    SE núcleo é verbo de ação → PREDICADO VERBAL
    SE núcleo é verbo de ligação + característica → PREDICADO NOMINAL
    SE combina ação + estado → PREDICADO VERBO-NOMINAL
```

### **9. SEMÂNTICA - SISTEMA DE ANÁLISE DE SIGNIFICADO**

#### **9.1 Heurística para Identificação de Relações Semânticas**

```python
def identificar_relacao_semantica(par_palavras):
    """
    Algoritmo para detectar automaticamente relações semânticas
    """
    SE palavras tem significados similares em múltiplos contextos
    E são intercambiáveis em muitos casos
    ENTÃO relação de SINONÍMIA
  
    SE palavras tem significados opostos
    E formam pares complementares
    ENTÃO relação de ANTONÍMIA
  
    SE palavras tem mesma forma mas significados não relacionados
    ENTÃO relação de HOMONÍMIA
  
    SE palavras tem significados relacionados por extensão
    ENTÃO relação de POLISSEMIA
```

#### **9.2 Sistema de Distinção Denotação/Conotação**

```python
def distinguir_denotacao_conotacao(uso_palavra):
    """
    Heurística baseada em contexto e frequência
    """
    SE uso é literal e dicionarizado → DENOTAÇÃO
    SE uso é figurado e contextual → CONOTAÇÃO
  
    INDICADORES_DE_CONOTAÇÃO = [
        - Contexto metafórico
        - Valor afetivo adicional
        - Associação cultural específica
        - Dependência do contexto para interpretação
    ]
```

### **10. PRAGMÁTICA - SISTEMA DE ANÁLISE CONTEXTUAL**

#### **10.1 Heurística para Identificação de Atos de Fala**

```python
def classificar_ato_fala(enunciado, contexto):
    """
    Algoritmo baseado em intenção comunicativa
    """
    SE intenção é descrever estado de coisas → ATOS ASSERTIVOS
    SE intenção é fazer o ouvinte agir → ATOS DIRETIVOS
    SE intenção é comprometer-se com ação → ATOS COMPROMISSIVOS
    SE intenção é expressar estado psicológico → ATOS EXPRESSIVOS
    SE intenção é mudar estado institucional → ATOS DECLARATIVOS
```

#### **10.2 Sistema de Detecção de Implicaturas**

```python
def detectar_implicatura(enunciado, contexto_conversacional):
    """
    Heurística para identificar significados implícitos
    """
    SE há violação controlada das máximas conversacionais
    E o ouvinte precisa inferir significado adicional
    ENTÃO existe IMPLICATURA CONVERSACIONAL
  
    MÁXIMAS_DETECTÁVEIS:
    - Quantidade (muita/pouca informação)
    - Qualidade (veracidade duvidosa)
    - Relação (relevância questionável)
    - Modo (clareza comprometida)
```

### **11. FONÉTICA ACÚSTICA E AUDITIVA - SISTEMA DE ANÁLISE**

#### **11.1 Heurística de Análise Acústica Autônoma**

```python
def analise_acustica(som, propriedades_fisicas):
    """
    Sistema para detectar propriedades acústicas sem instrumentos
    """
    SE som tem frequência fundamental alta → SOM AGUDO
    SE som tem frequência fundamental baixa → SOM GRAVE
    SE som tem intensidade alta → SOM FORTE
    SE som tem formantes específicos → IDENTIFICAÇÃO DE VOGAIS
  
    # Heurística para sílaba tônica por evidências acústicas
    SE sílaba tem maior intensidade e duração → TÔNICA
    SE sílaba tem menor intensidade e duração → ÁTONA
```

#### **11.2 Heurística de Percepção Auditiva**

```python
def analise_auditiva(percepcao, expectativas_linguisticas):
    """
    Sistema baseado em como os sons são percebidos
    """
    SE ouvimos diferenças que são fonemicamente relevantes
    E ignoramos diferenças que são apenas alofônicas
    ENTÃO estamos aplicando FILTRO FONOLÓGICO
  
    EXEMPLOS DETECTÁVEIS:
    - Perceber /p/ e /b/ como diferentes (contrastivo)
    - Ignorar variações de /s/ entre falantes (não-contrastivo)
```

### **12. ENCONTROS VOCÁLICOS COMPLETOS - SISTEMA DE CLASSIFICAÇÃO**

#### **12.1 Heurística para Tritongo**

```python
def identificar_tritongo(sequencia_vocalica):
    """
    Algoritmo específico para tritongos
    """
    SE sequência tem três elementos vocálicos
    E estão na mesma sílaba
    E padrão é semivogal + vogal + semivogal
    E há continuidade melódica
    ENTÃO é TRITONGO
  
    EXEMPLOS IDENTIFICÁVEIS:
    - "Uruguai" → u-ru-guai (semi+vogal+semi)
    - "saguão" → sa-guão (semi+vogal+semi)
```

#### **12.2 Heurística para Encontro Consonantal Perfeito/Imperfeito**

```python
def classificar_encontro_consonantal(sequencia_consonantal):
    """
    Sistema para distinguir tipos de encontros consonantais
    """
    SE consoantes estão na mesma sílaba
    E são pronunciadas em sequência contínua
    ENTÃO ENCONTRO CONSONANTAL PERFEITO
  
    SE consoantes estão em sílabas diferentes  
    E há pausa perceptível entre elas
    ENTÃO ENCONTRO CONSONANTAL IMPERFEITO
```

### **13. DÍGRAFO - HEURÍSTICA ESPECÍFICA**

#### **13.1 Algoritmo de Identificação de Dígrafos**

```python
def identificar_digrafo(par_letras):
    """
    Heurística para detectar dígrafos
    """
    SE duas letras representam um único fonema
    E a combinação é fixa e convencionalizada
    E não segue as regras fonéticas normais de combinação
    ENTÃO é DÍGRAFO
  
    EXEMPLOS DETECTÁVEIS:
    - "ch" → /ʃ/ (único som)
    - "lh" → /ʎ/ (único som)
    - "nh" → /ɲ/ (único som)
```

### **14. PROSÓDIA E FONOTÁTICA - SISTEMA DE ANÁLISE**

#### **14.1 Heurística de Análise Prosódica**

```python
def analise_prosodica(entoado, estrutura_ritmica):
    """
    Sistema para detectar padrões melódicos e rítmicos
    """
    PADRÕES_ENTOACIONAIS = [
        SE tom sobe no final → INTERROGAÇÃO
        SE tom desce no final → AFIRMAÇÃO
        SE tom tem variação complexa → EXCLAMAÇÃO
    ]
  
    PADRÕES_RÍTMICOS = [
        SE alternância regular tônica-átona → RITMO REGULAR
        SE padrão irregular → RITMO IRREGULAR
    ]
```

#### **14.2 Heurística de Fonotática**

```python
def analise_fonotatica(combinacoes_fonemas):
    """
    Sistema para descobrir restrições de combinação de sons
    """
    PADRÕES_PERMITIDOS = combinacoes que ocorrem frequentemente
    PADRÕES_PROIBIDOS = combinacoes que nunca ocorrem
  
    EXEMPLOS DETECTÁVEIS:
    - "bl" é permitido ("bloco")
    - "bn" inicial é proibido
    - "str" é permitido ("stress")
```

### **15. PROCESSOS FONOLÓGICOS ESPECÍFICOS - SISTEMA DE IDENTIFICAÇÃO**

#### **15.1 Heurísticas para Cada Processo Fonológico**

```python
def identificar_processos_fonologicos(mudanca_fonetica, contexto):
    """
    Sistema para classificar processos fonológicos
    """
    SE som se torna mais parecido com som adjacente → ASSIMILAÇÃO
    SE sons similares se tornam diferentes → DISSIMILAÇÃO
    SE som é omitido na fala rápida → ELISÃO
    SE som é inserido para facilitar pronúncia → EPÊNTESE
    SE sons trocam de posição → METÁTESSE
  
    EXEMPLOS CONCRETOS:
    - "mesmo" → "me[s]mo" → "me[z]mo" → ASSIMILAÇÃO
    - "ad[v]ogado" → "ad[e]vogado" → EPÊNTESE
```

### **16. TRANSCRIÇÃO FONÉTICA E AFI - SISTEMA DE REPRESENTAÇÃO**

#### **16.1 Heurística para Transcrição Fonética**

```python
def sistema_transcricao_fonetica(palavra, realizacao_fonetica):
    """
    Algoritmo para criar sistema de transcrição
    """
    PRINCÍPIO_GERAL = Um símbolo para cada som distintivo
  
    # Descoberta do inventário fonético
    INVENTÁRIO = conjunto de sons distintivos da língua
    SÍMBOLOS = atribuir símbolos únicos para cada som do inventário
  
    EXEMPLO EMERGENTE:
    - /k/ para o som de "c" em "casa"
    - /s/ para o som de "s" em "sapo"
    - /ʃ/ para o som de "x" em "caixa"
```

### **17. ORTOGRAFIA - CONCEITOS ESPECÍFICOS**

#### **17.1 Heurísticas para Usos Específicos de Letras**

##### **17.1.1 Sistema para X vs CH**

```python
def distinguir_x_ch(som, contexto_ortografico):
    """
    Heurística baseada em padrões distribucionais
    """
    PADRÕES_DO_CH = [
        - Início de palavra antes de vogal ("chave")
        - Pronúncia sempre /ʃ/
    ]
  
    PADRÕES_DO_X = [
        - Pronúncia varia (/ʃ/, /s/, /ks/, /z/)
        - Posições específicas (início, meio, fim)
    ]
```

##### **17.1.2 Sistema para G vs J**

```python
def distinguir_g_j(som, vogal_seguinte):
    """
    Heurística baseada no contexto vocálico
    """
    SE som é /ʒ/ antes de E ou I → USAR G
    SE som é /ʒ/ antes de A, O, U → USAR J
    SE som é /g/ → SEMPRE USAR G
```

#### **17.2 Heurística para o Uso do Hífen**

```python
def regras_hifen(palavra_composta):
    """
    Sistema para descobrir regras de hifenização
    """
    PADRÕES_EMERGENTES = [
        SE elementos mantêm acentuação própria → USAR HÍFEN
        SE há fusão fonética → NÃO USAR HÍFEN
        SE segundo elemento começa com H → USAR HÍFEN
    ]
```

#### **17.3 Sistema para os Porquês**

```python
def distinguir_porques(uso, posicao_sintatica):
    """
    Heurística autônoma para os quatro porquês
    """
    SE em perguntas diretas ou indiretas → POR QUE
    SE em respostas e explicações → PORQUE
    SE no final de frases interrogativas → POR QUÊ
    SE como substantivo (com artigo) → PORQUÊ
```

### **18. ACENTUAÇÃO GRÁFICA - SISTEMA COMPLETO**

#### **18.1 Heurística para Acento Agudo vs Circunflexo**

```python
def distinguir_acentos(vogal_tonica, timbre):
    """
    Sistema baseado no timbre vocálico
    """
    SE vogal tônica é aberta → ACENTO AGUDO (´)
    SE vogal tônica é fechada → ACENTO CIRCUNFLEXO (^)
  
    EXEMPLOS:
    - "pé" [ɛ aberto] → agudo
    - "você" [e fechado] → circunflexo
```

#### **18.2 Heurística para Crase**

```python
def identificar_crase(preposicao_a, artigo_a):
    """
    Sistema para detectar casos de crase
    """
    TESTE_PRÁTICO = substituir por "ao" (se possível → crase)
  
    CASOS_OBRIGATÓRIOS = [
        - Antes de substantivo feminino específico
        - Locuções femininas consagradas
    ]
  
    CASOS_PROIBIDOS = [
        - Antes de masculino
        - Antes de verbo
        - Antes de palavra indeterminada
    ]
```

### **19. PONTUAÇÃO - SISTEMA COMPLETO**

#### **19.1 Heurística para Uso da Vírgula**

```python
def regras_virgula(estrutura_sintatica):
    """
    Sistema autônomo para regras de vírgula
    """
    OCASIÕES_OBRIGATÓRIAS = [
        - Separar elementos enumerados
        - Isolar vocativos
        - Separar orações coordenadas
        - Isolar aposto
    ]
  
    OCASIÕES_PROIBIDAS = [
        - Entre sujeito e verbo
        - Entre verbo e complementos essenciais
    ]
```

#### **19.2 Heurística para Pontuação Complexa**

```python
def usar_ponto_virgula(relacao_entre_oracoes):
    """
    Sistema para ponto e vírgula
    """
    SE orações têm relação próxima mas são complexas
    E já contêm vírgulas internamente
    ENTÃO USAR PONTO E VÍRGULA
  
    EXEMPLO DETECTADO:
    - "Na cidade, há trânsito; no campo, há paz."
```

### **20. MORFOLOGIA - CONCEITOS RESTANTES**

#### **20.1 Heurística para Alomorfes**

```python
def identificar_alomorfes(variantes_radical):
    """
    Sistema para detectar alomorfes
    """
    SE formas diferentes do mesmo radical
    E a variação é condicionada fonologicamente
    E mantêm o mesmo significado
    ENTÃO são ALOMORFES
  
    EXEMPLOS:
    - faz- (fazer), fez- (fez), fac- (faço)
```

#### **20.2 Heurística para Vogal Temática**

```python
def identificar_vogal_tematica(radical, desinencias):
    """
    Sistema para detectar vogal temática
    """
    SE vogal que aparece entre radical e desinências
    E indica a conjugação verbal
    E é -a-, -e- ou -i-
    ENTÃO é VOGAL TEMÁTICA
```

### **21. SINTAXE - CONCEITOS EXPANDIDOS**

#### **21.1 Heurística para Termos Integrantes**

```python
def identificar_termos_integrantes(funcao_sintatica):
    """
    Sistema para objetos e complementos
    """
    SE completa verbo transitivo sem preposição → OBJETO DIRETO
    SE completa verbo transitivo com preposição → OBJETO INDIRETO
    SE completa nome (substantivo, adjetivo) → COMPLEMENTO NOMINAL
```

#### **21.2 Heurística para Vozes Verbais**

```python
def identificar_voz_verbal(relacao_sujeito_acao):
    """
    Sistema para vozes do verbo
    """
    SE sujeito pratica a ação → VOZ ATIVA
    SE sujeito sofre a ação → VOZ PASSIVA
    SE sujeito pratica e sofre a ação → VOZ REFLEXIVA
```

### **22. ORTOGRAFIA - ITENS PENDENTES**

#### **22.1 Heurística para S, Z, SS, Ç**

```python
def regras_s_z_ss_c(posicao_palavra, contexto_fonetico):
    """
    Sistema autônomo para descobrir padrões de S, Z, SS, Ç
    """
    PADRÕES_EMERGENTES = [
        # Som /s/
        SE início de palavra → S ("sapo")
        SE entre vogais e som é /z/ → S ("casa")
        SE entre vogais e som é /s/ → SS ("passo")
        SE antes de consoante surda → S ("rastejar")
  
        # Som /z/
        SE início de palavra → Z ("zebra")
        SE em final de sílaba após sonora → Z ("razão")
  
        # Ç
        SE som /s/ antes de A, O, U → Ç ("ça", "ço", "çu")
    ]
```

#### **22.2 Heurística para o Uso do H**

```python
def regras_letra_h(posicao, etimologia):
    """
    Sistema para detectar quando usar H
    """
    PADRÕES_DETECTÁVEIS = [
        SE palavra vem do latim com H → MANTER H ("homem", "hora")
        SE é dígrafo (CH, NH, LH) → H OBRIGATÓRIO
        SE é interjeição → PODE TER H ("ah", "oh")
    ]
```

#### **22.3 Heurística para Maiúsculas/Minúsculas**

```python
def regras_maiusculas_minusculas(contexto, tipo_texto):
    """
    Sistema para descobrir regras de capitalização
    """
    OCASIÕES_MAIÚSCULA = [
        Início de frase após ponto
        Nomes próprios (pessoas, lugares, instituições)
        Títulos de obras
    ]
  
    OCASIÕES_MINÚSCULA = [
        Nomes comuns
        Texto contínuo após vírgula
    ]
```

#### **22.4 Heurística para M e N**

```python
def regras_m_n(contexto_fonetico):
    """
    Sistema para distinguir M e N
    """
    PADRÕES_EMERGENTES = [
        SE antes de P ou B → SEMPRE M ("campo", "tambor")
        SE antes de outras consoantes → N ("canto", "mundial")
        SE em final de palavra → M ou N (padrões específicos)
    ]
```

#### **22.5 Heurística para R e RR**

```python
def regras_r_rr(posicao, som):
    """
    Sistema para uso de R e RR
    """
    PADRÕES_DETECTÁVEIS = [
        SE som fraco entre vogais → R ("caro")
        SE som forte entre vogais → RR ("carro")
        SE início de palavra → R forte ("rato")
        SE após consoante → R ("prato")
    ]
```

#### **22.6 Heurística para E e I em Sílabas Átonas**

```python
def regras_e_i_silabas_atonas(positicao, etimologia):
    """
    Sistema para E e I em posição átona
    """
    PADRÕES = [
        SE final de ditongo decrescente → I ("pai")
        SE em sufixos específicos → E ("-mente")
        SE em palavras de origem específica → padrão etimológico
    ]
```

#### **22.7 Heurística para O e U**

```python
def regras_o_u(contexto_fonetico):
    """
    Sistema para O e U
    """
    PADRÕES = [
        SE após G ou Q e mudo → U ("guerra", "queijo")
        SE som /u/ em outras posições → U ou O (etimologia)
    ]
```

#### **22.8 Heurística para K, W, Y**

```python
def uso_k_w_y(origem_palavra):
    """
    Sistema para letras estrangeiras
    """
    PADRÕES = [
        SE palavra de origem estrangeira → MANTER K, W, Y
        SE unidade de medida → K ("kg", "km")
        SE nome próprio estrangeiro → MANTER grafia original
    ]
```

#### **22.9 Heurística para o Acordo Ortográfico**

```python
def mudanças_acordo_ortografico(regra_antiga, regra_nova):
    """
    Sistema para detectar mudanças do acordo
    """
    MUDANÇAS_PRINCIPAIS = [
        Eliminação do trema
        Novas regras de hífen
        Mudanças em acentuação de ditongos
        Uniformização com outros países lusófonos
    ]
```

### **23. ACENTUAÇÃO GRÁFICA - ITENS PENDENTES**

#### **23.1 Heurística para Ditongos Acentuados**

```python
def acentuacao_ditongos(ditongo, posicao_tonica):
    """
    Sistema específico para ditongos
    """
    REGRAS_EMERGENTES = [
        SE ditongo aberto (éi, ói, éu) É tônico → ACENTUAR
        SE ditongo nasal (ãe, õe) → ACENTUAÇÃO ESPECÍFICA
    ]
```

#### **23.2 Heurística para Hiatos Acentuados**

```python
def acentuacao_hiatos(hiato, vogais):
    """
    Sistema para hiatos
    """
    REGRAS = [
        SE i ou u tônicos formando hiato → ACENTUAR
        SE vogais iguais em hiato → REGRAS ESPECÍFICAS
    ]
```

#### **23.3 Heurística para Acentos Diferenciais**

```python
def acentos_diferenciais(palavra, homografa):
    """
    Sistema para acentos diferenciais
    """
    CASOS_RESTANTES = [
        "pôde" (pretérito) vs "pode" (presente)
        "pôr" (verbo) vs "por" (preposição)
        "têm" (plural) vs "tem" (singular)
    ]
```

### **24. PONTUAÇÃO - ITENS PENDENTES**

#### **24.1 Heurística para Ponto Final**

```python
def uso_ponto_final(estrutura_frase):
    """
    Sistema para ponto final
    """
    OCASIÕES = [
        Final de frase declarativa
        Após abreviaturas
        Indica conclusão de pensamento
    ]
```

#### **24.2 Heurística para Ponto de Interrogação/Exclamação**

```python
def pontos_interrogacao_exclamacao(intencao_comunicativa):
    """
    Sistema para ? e !
    """
    SE intenção é perguntar → PONTO DE INTERROGAÇÃO
    SE intenção é exclamar → PONTO DE EXCLAMAÇÃO
    SE ambas → ?! ou !?
```

#### **24.3 Heurística para Dois-Pontos**

```python
def uso_dois_pontos(funcao_textual):
    """
    Sistema para :
    """
    FUNÇÕES = [
        Introduzir enumeração
        Introduzir citação direta
        Introduzir explicação
        Antes de apostos explicativos
    ]
```

#### **24.4 Heurística para Travessão**

```python
def uso_travessao(funcao):
    """
    Sistema para —
    """
    USOS = [
        Marcar discurso direto
        Isolar elementos com ênfase
        Indicar interrupção
        Em pares para isolar informações
    ]
```

#### **24.5 Heurística para Parênteses**

```python
def uso_parenteses(natureza_informacao):
    """
    Sistema para ( )
    """
    SE informação é explicativa secundária → PARÊNTESES
    SE é citação com intervenção do editor → COLCHETES
```

#### **24.6 Heurística para Aspas**

```python
def uso_aspas(finalidade):
    """
    Sistema para " "
    """
    USOS = [
        Delimitar citações diretas
        Indicar ironia ou sentido especial
        Nomes de obras, títulos
        Estrangeirismos
    ]
```

#### **24.7 Heurística para Reticências**

```python
def uso_reticencias(efeito_desejado):
    """
    Sistema para ...
    """
    EFEITOS = [
        Indicar interrupção ou hesitação
        Sugerir continuação
        Criar suspense
        Indicar omissão em citação
    ]
```

#### **24.8 Heurística para Colchetes**

```python
def uso_colchetes(contexto_editorial):
    """
    Sistema para [ ]
    """
    USOS_EDITORIAIS = [
        Inserções em citações
        Correções em textos citados
        Informações complementares
    ]
```

### **25. MORFOLOGIA - CONCEITOS PENDENTES**

#### **25.1 Heurística para Radical Lexical vs Temático**

```python
def distinguir_radical_lexical_tematico(funcao):
    """
    Sistema para radicais lexical e temático
    """
    RADICAL_LEXICAL = carrega significado essencial
    RADICAL_TEMÁTICO = radical + vogal temática (prepara para flexão)
```

### **26. PRAGMÁTICA - DETALHAMENTO**

#### **26.1 Heurística Detalhada para Atos de Fala**

```python
def classificar_atos_fala_detalhado(enunciado, contexto):
    """
    Sistema mais detalhado para atos de fala
    """
    SUBTIPOS = [
        # Diretivos: ordens, pedidos, conselhos, sugestões
        # Compromissivos: promessas, juramentos, garantias
        # Expressivos: agradecimentos, desculpas, parabéns
        # Declarativos: batizar, demitir, declarar
        # Representativos: afirmar, negar, informar
    ]
```

---

## **MAPEAMENTO GERAL DO SISTEMA**

| Seção | Conceito                               | Heurísticas | Módulos Implementados                                                                                                                                                                                           |
| ------- | -------------------------------------- | -----------: | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1       | Fonologia Completa                     |           ✅ | Fonologia/AnalisadorFonema.cs, Fonologia/AnalisadorFonologico.cs, Fonologia/AnaliseArticulatoria.cs, Fonologia/ClassificadorSegmental.cs, Fonologia/DistinguirLetraFonema.cs, Fonologia/SilabificadorFonemico.cs |
| 2       | Fonética AcústicaAcústicae Auditiva |     Pendente | —                                                                                                                                                                                                               |
| 3       | Encontros Vocálicos Completos         |     Pendente | —                                                                                                                                                                                                               |
| 4       | Dígrafos                              |     Pendente | —                                                                                                                                                                                                               |
| 5       | Prosódia e Fonotática                |     Pendente | —                                                                                                                                                                                                               |
| 6       | Processos Fonológicos Específicos    |     Pendente | —                                                                                                                                                                                                               |
| 7       | Transcrição Fonética e AFI          |     Pendente | —                                                                                                                                                                                                               |
| 8       | Ortografia Completa                    |     Pendente | —                                                                                                                                                                                                               |
| 9       | Acentuação Gráfica Completa         |     Pendente | —                                                                                                                                                                                                               |
| 10      | Pontuação Completa                   |     Pendente | —                                                                                                                                                                                                               |
| 11      | Morfologia Completa                    |     Pendente | —                                                                                                                                                                                                               |
| 12      | Sintaxe Completa                       |     Pendente | —                                                                                                                                                                                                               |
| 13      | Semântica Completa                    |     Pendente | —                                                                                                                                                                                                               |
| 14      | Pragmática Completa                   |     Pendente | —                                                                                                                                                                                                               |
| 15      | Ortografia Pendente                    |     Pendente | —                                                                                                                                                                                                               |
| 16      | Acentuação Gráfica Pendente         |     Pendente | —                                                                                                                                                                                                               |
| 17      | Pontuação Pendente                   |     Pendente | —                                                                                                                                                                                                               |
| 18      | Morfologia Pendente                    |     Pendente | —                                                                                                                                                                                                               |
| 19      | Pragmática Detalhada                  |     Pendente | —                                                                                                                                                                                                               |

---

### **Correspondência com `docs/Linguistica.md`**

- **Seção 1 — Fonologia:** coberta pelas implementações em `Fonologia/`:

  - `AnalisadorFonema.cs` — transcrição fonética (IPA → TFD), identificação de fonemas e mapeamentos fonológico/ortográfico.
  - `AnalisadorFonologico.cs` — representação do sistema fonológico, oposição fonêmica, neutralização e regras fonotáticas.
  - `AnaliseArticulatoria.cs` — heurísticas de fonética articulatória (pontos e modos de articulação).
  - `ClassificadorSegmental.cs` — classificação segmental (vogal / consoante / semivogal) e operação sobre encontros consonantais.
  - `DistinguirLetraFonema.cs` — heurística letra vs fonema (mapeamento representação gráfica ↔ unidade fônica).
  - `SilabificadorFonemico.cs` — divisão silábica, classificação por número de sílabas, ditongos/hiatos.
- **Seções parcialmente implementadas:**

  - Transcrição fonética / AFI (seção relacionada em `docs/Linguistica.md`) — parcialmente atendida por `AnalisadorFonema.cs`.
- **Seções pendentes (nenhum módulo correspondente encontrado):**

  - Fonética acústica e auditiva (seção 1.2 completa), Encontros vocálicos avançados, Dígrafos específicos, Prosódia detalhada, Acentuação gráfica completa, Ortografia completa, Morfologia, Sintaxe, Semântica, Pontuação — não foram encontrados arquivos correspondentes em `Fonologia/` ou em outros diretórios do repositório.

Se desejar, posso gerar stubs (`AnalisadorDigrafos.cs`, `AnalisadorProsodia.cs`, `AnalisadorFoneticoAcusticoAuditivo.cs`, etc.) com as heurísticas mapeadas de `docs/Linguistica.md` para acelerar a implementação.

### **SISTEMA INTEGRADO DE ANÁLISE LINGUÍSTICA**

```python
def sistema_taxonimico_completo(enunciado):
    """
    Sistema final que aplica todas as heurísticas de forma integrada
    """
    analises = {}
  
    # 1. Análise Fonológica Completa
    analises['fonologica'] = aplicar_heuristicas_fonologicas_completas(enunciado)
  
    # 2. Análise Fonética Acústica e Auditiva
    analises['fonetica_acustica_auditiva'] = aplicar_heuristicas_foneticas_acusticas_auditivas(enunciado)
  
    # 3. Análise de Encontros Vocálicos Completos
    analises['encontros_vocalicos'] = aplicar_heuristicas_encontros_vocalicos_completos(enunciado)
  
    # 4. Análise de Dígrafos
    analises['digrafos'] = aplicar_heuristicas_digrafos(enunciado)
  
    # 5. Análise Prosódica e Fonotática
    analises['prosodia_fonotatica'] = aplicar_heuristicas_prosodicas_fonotaticas(enunciado)
  
    # 6. Análise de Processos Fonológicos Específicos
    analises['processos_fonologicos'] = aplicar_heuristicas_processos_fonologicos(enunciado)
  
    # 7. Análise de Transcrição Fonética e AFI
    analises['transcricao_fonetica'] = aplicar_heuristicas_transcricao_fonetica(enunciado)
  
    # 8. Análise Ortográfica Completa
    analises['ortografia'] = aplicar_heuristicas_ortograficas_completas(enunciado)
  
    # 9. Análise de Acentuação Gráfica Completa
    analises['acentuacao_grafica'] = aplicar_heuristicas_acentuacao_grafica_completa(enunciado)
  
    # 10. Análise de Pontuação Completa
    analises['pontuacao'] = aplicar_heuristicas_pontuacao_completa(enunciado)
  
    # 11. Análise Morfológica Completa
    analises['morfologica'] = aplicar_heuristicas_morfologicas_completas(enunciado)
  
    # 12. Análise Sintática Completa
    analises['sintatica'] = aplicar_heuristicas_sintaticas_completas(enunciado)
  
    # 13. Análise Semântica Completa
    analises['semantica'] = aplicar_heuristicas_semanticas_completas(enunciado)
  
    # 14. Análise Pragmática Completa
    analises['pragmatica'] = aplicar_heuristicas_pragmaticas_completas(enunciado)
  
    # 15. Análise Ortográfica Pendente
    analises['ortografia_pendente'] = aplicar_heuristicas_ortograficas_pendentes(enunciado)
  
    # 16. Análise de Acentuação Gráfica Pendente
    analises['acentuacao_grafica_pendente'] = aplicar_heuristicas_acentuacao_grafica_pendentes(enunciado)
  
    # 17. Análise de Pontuação Pendente
    analises['pontuacao_pendente'] = aplicar_heuristicas_pontuacao_pendentes(enunciado)
  
    # 18. Análise Morfológica Pendente
    analises['morfologia_pendente'] = aplicar_heuristicas_morfologicas_pendentes(enunciado)
  
    # 19. Análise Pragmática Detalhada
    analises['pragmatica_detalhada'] = aplicar_heuristicas_pragmaticas_detalhadas(enunciado)
  
    return analises
```

---

**CARACTERÍSTICAS DO SISTEMA CRIADO:**

1. **AUTONOMIA COMPLETA**: Cada heurística funciona independentemente, sem necessidade de conhecimento prévio
2. **DETECÇÃO BASEADA EM EVIDÊNCIAS**: Usa padrões distribuicionais, contrastes e relações sistêmicas
3. **CLASSIFICAÇÃO MULTINÍVEL**: Analisa desde o nível fônico até o pragmático
4. **IDENTIFICAÇÃO DE PADRÕES**: Descobre regularidades linguísticas automaticamente
5. **SISTEMA INTEGRADO**: Combina todas as análises em visão unificada
6. **COBERTURA TOTAL**: Inclui todos os 100+ conceitos da gramática portuguesa
7. **ELITIZAÇÃO COMPLETA**: Transforma qualquer análise linguística em categorização técnica precisa

Este sistema permite **"elitizar" qualquer análise linguística**, transformando observações superficiais em categorizações técnicas precisas usando as heurísticas taxonômicas específicas para cada conceito da gramática portuguesa.
