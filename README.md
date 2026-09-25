# Lojinha RPG

Aplicativo desktop para administrar lojas de uma campanha de RPG de mundo aberto, com um
**Painel do Mestre** (biblioteca de lojas, itens, estoque, falas e visual) e uma **Janela da Loja**
separada, pensada para ser capturada no OBS como cena de transmissão (1920×1080).

- Moeda única: **Sucata**.
- Itens sem imagem (só nome, preço, estoque e informações adicionais).
- Estoque persiste entre sessões; reposição é sempre manual.
- Sets de loja e presets visuais são importáveis/exportáveis, incluindo imagens e áudios.

## Sumário

1. [Abrindo o aplicativo no Windows](#abrindo-o-aplicativo-no-windows)
2. [Configurando a captura no OBS](#configurando-a-captura-no-obs)
3. [Fluxo de uso rápido](#fluxo-de-uso-rápido)
4. [Estrutura do projeto](#estrutura-do-projeto)
5. [Compilando a partir do código-fonte](#compilando-a-partir-do-código-fonte)
6. [O que foi testado e como](#o-que-foi-testado-e-como)
7. [Limitações conhecidas](#limitações-conhecidas)

## Abrindo o aplicativo no Windows

1. Baixe [`dist/LojinhaRPG-win-x64.exe`](dist/LojinhaRPG-win-x64.exe) deste repositório (no GitHub,
   abra o arquivo e clique em "Download raw file") e copie-o para qualquer pasta no seu PC com
   Windows 10/11 de 64 bits. É um executável único e autocontido — **não precisa instalar .NET
   nem nada além do Windows**. (Se preferir compilar você mesmo, veja
   [Compilando a partir do código-fonte](#compilando-a-partir-do-código-fonte); o resultado sai em
   `publish/win-x64/LojinhaRPG.exe`.)
2. Dê duplo clique em `LojinhaRPG.exe`. O **Painel do Mestre** abre primeiro.
3. O Windows Defender SmartScreen pode avisar "Windows protegeu seu PC" por ser um executável
   novo e sem assinatura digital paga. Clique em **Mais informações → Executar assim mesmo**.
4. Todos os seus dados (sets, presets, imagens, áudios) ficam salvos em
   `%AppData%\LojinhaRPG\` — eles continuam lá mesmo depois de fechar e reabrir o programa, ou
   de mover o `.exe` para outra pasta.

Não há instalador: é só um arquivo. Para desinstalar, apague o `.exe` e, se quiser apagar também
os dados salvos, a pasta `%AppData%\LojinhaRPG\`.

## Configurando a captura no OBS

1. No Painel do Mestre, selecione uma loja na biblioteca e clique em **Abrir loja**. Isso abre a
   **Janela da Loja** — uma janela separada, sem barra de título, com exatamente 1920×1080 pixels.
2. No OBS, adicione uma fonte **Captura de Janela** (Window Capture).
3. Em "Janela", escolha **Lojinha RPG — Loja**. Como o Painel do Mestre é uma janela totalmente
   separada, ele nunca aparece nessa captura.
4. Se necessário, use **Redimensionar saída para a origem** ou ajuste manualmente a cena para
   1920×1080 — a janela já nasce nesse tamanho exato.
5. Para reposicionar a janela da loja na sua tela (por exemplo, para um segundo monitor), clique
   e arraste qualquer área de fundo da janela (fora dos botões e itens) — ela não tem barra de
   título, então o arraste funciona clicando direto no cenário.
6. Para encerrar a sessão, use o botão **Sair da loja** no Painel do Mestre (não feche a janela da
   loja pelo Windows) — isso toca a despedida cadastrada antes de fechar.

## Fluxo de uso rápido

**Painel do Mestre**

- **Biblioteca de lojas** (esquerda): criar, duplicar, excluir, importar (`.zip`) e exportar (`.zip`) sets.
- Aba **Dados**: nome da loja, nome do vendedor e as 4 imagens (vendedor, reação, despedida, fundo).
- Aba **Itens**: adicionar/editar/remover itens e **repor estoque** manualmente.
- Aba **Falas**: legenda de texto e/ou áudio para chegada, venda e saída.
- Aba **Visual**: a prévia já mostra o resultado final (fundo real, vendedor sem moldura e a
  grade de itens já estilizada). Escolha/edite/crie presets visuais e arraste a posição/tamanho
  do vendedor e da grade de itens (também dá para digitar os valores em pixels). Um preset pode
  ter uma textura de imagem para a moldura de cada item, além das cores.
- **Abrir loja** / **Sair da loja**: controla a sessão ao vivo.

**Janela da Loja (visão dos jogadores / OBS)**

- O vendedor aparece como uma imagem solta sobre o fundo, sem nenhuma moldura atrás.
- Grade 3×3 (9 itens por página), cada item na sua própria moldura (cor ou textura do preset),
  com paginação quando há mais itens.
- Passar o mouse sobre um item mostra as informações adicionais.
- Clicar em um item abre estoque disponível, controle de quantidade e confirmação de compra.
- Após confirmar, aparece por ~2s um indicador vermelho ("− X Sucata"), toca a imagem de reação
  e a fala de venda (se cadastradas), e o estoque é descontado e salvo imediatamente.
- Falas com legenda aparecem como um balão de fala estilo mangá, ancorado perto do vendedor.

## Estrutura do projeto

```
src/
  LojinhaRPG.Core/     Modelos e serviços (sem dependência de UI): sets, itens, presets,
                       persistência em JSON, import/export em .zip, regra de compra.
  LojinhaRPG.App/      Aplicativo Avalonia (multiplataforma): Painel do Mestre, Janela da
                       Loja, editor visual arrastável, diálogos, reprodução de áudio.
tests/
  LojinhaRPG.Tests/    Testes automatizados (xUnit) do núcleo: compra, persistência,
                       duplicação e round-trip de exportação/importação.
publish/win-x64/       Saída do build para Windows (gerada localmente, não versionada).
```

Arquitetura: **.NET 8 + Avalonia UI** (interface multiplataforma baseada em Skia), com MVVM via
CommunityToolkit.Mvvm. Cada loja ("set") é uma pasta própria em
`%AppData%\LojinhaRPG\Sets\<id>\`, com `set.json` (dados) e uma subpasta `media\` com as imagens e
áudios copiados para dentro do set — por isso eles continuam funcionando mesmo que o arquivo
original seja movido ou apagado depois de importado. Presets visuais ficam em
`%AppData%\LojinhaRPG\Presets\`. Áudio no Windows é tocado com NAudio.

## Compilando a partir do código-fonte

Pré-requisito: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
# Rodar localmente (Windows, Linux ou macOS, para desenvolvimento):
dotnet run --project src/LojinhaRPG.App/LojinhaRPG.App.csproj

# Rodar os testes automatizados:
dotnet test tests/LojinhaRPG.Tests/LojinhaRPG.Tests.csproj

# Gerar o executável único e autocontido para Windows 64 bits
# (funciona mesmo compilando a partir de Linux/macOS):
dotnet publish src/LojinhaRPG.App/LojinhaRPG.App.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

O resultado é `publish/win-x64/LojinhaRPG.exe` (~90 MB, já inclui o runtime do .NET — não precisa
que o usuário final instale nada).

## O que foi testado e como

Este projeto foi desenvolvido e testado neste ambiente **Linux** (o Avalonia UI roda de forma
nativa no Linux, não só no Windows), usando um servidor X virtual (Xvfb) e automação de mouse/
teclado para exercitar a interface de verdade — não apenas compilar o código. Foram verificados
manualmente, com capturas de tela em cada passo:

- Criar uma loja nova, editar seus dados e reabrir o app do zero (processo encerrado e
  reiniciado) confirmando que tudo persistiu.
- Cadastrar 11 itens e verificar a paginação 3×3 (página 1/2 e 2/2) na Janela da Loja.
- Comprar múltiplas unidades de um item (5×), com o total calculado corretamente e o indicador
  "− 25 Sucata" aparecendo.
- Tentar comprar um item com estoque zerado (bloqueado com "Quantidade inválida").
- Repor estoque manualmente pelo Painel do Mestre.
- Carregar uma imagem real para o vendedor e ver a prévia atualizando.
- Exportar um set para `.zip` (conferido que contém `set.json` + a imagem em `media/`) e
  importá-lo de volta como um novo set independente.
- Abrir e fechar a Janela da Loja (1920×1080, sem decoração) a partir do Painel do Mestre,
  confirmando que o painel nunca aparece nela.
- Arrastar e redimensionar as áreas do vendedor e da grade de itens no editor visual, com os
  campos numéricos atualizando em tempo real.
- Alternar entre os presets "Loja de feira" e "Loja improvisada com musgo".

Além disso, `dotnet test` roda 11 testes automatizados cobrindo a regra de compra (sucesso,
estoque insuficiente, quantidade inválida, item inexistente) e a camada de persistência (criar →
recarregar, estoque sobrevivendo a um "reinício" simulado, duplicar, listar e o round-trip
completo de exportar → importar com mídia).

**O que não pôde ser verificado neste ambiente:** o `.exe` final do Windows não pôde ser
executado aqui, porque este é um container Linux sem Windows disponível — ele foi apenas
compilado (cross-compile) e confirmado como um executável PE32+ válido para Windows x64. A
reprodução de **som** também não pôde ser confirmada "de ouvido" (o ambiente de teste não tem
saída de áudio); o carregamento do arquivo e a chamada de reprodução foram validados em código,
e no Windows o app usa a biblioteca NAudio (WASAPI/WinMM), uma solução padrão e madura para
tocar `.wav`/`.mp3` em aplicativos desktop .NET.

Recomendo, ao testar no seu PC Windows, conferir especialmente: SmartScreen ao abrir o `.exe`
pela primeira vez, e se o som das falas toca corretamente.

## Limitações conhecidas

- Não há sistema de carteira/dinheiro dos jogadores — por decisão de design, é descontado
  manualmente nas fichas.
- Não há restauração automática de estoque (ex: por tempo) — reposição é sempre manual, também
  por decisão de design.
- O SmartScreen do Windows pode alertar sobre o executável por ele não ter assinatura digital
  paga (certificado de editor). Isso é normal para `.exe` distribuídos fora da Microsoft Store e
  não indica um problema no aplicativo.
