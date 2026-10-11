# Tibia Hunt Analyzer

[English](README.md)

Tibia Hunt Analyzer é um aplicativo desktop para registrar, armazenar, consultar e analisar sessões de hunt do Tibia localmente no computador do usuário. A aplicação foi pensada para uso offline e mantém os dados em um banco SQLite local, permitindo revisar sessões históricas, detalhes e métricas agregadas sem depender de serviços externos.

## Status do projeto

Este repositório contém uma implementação funcional do aplicativo desktop com persistência em banco de dados, gerenciamento de configurações, fluxos de importação, consulta de sessões, analytics e um script de publicação para Windows. A build e a suíte automatizada de testes foram verificadas no ambiente atual antes desta atualização da documentação.

## Funcionalidades

- Importação de dados do Tibia Hunt Analyzer a partir de arquivos JSON.
- Importação de conteúdo colado como texto.
- Cadastro e gerenciamento de personagens e locais de hunt.
- Registro de sessões de hunt com associação a personagem e local.
- Consulta de sessões históricas e inspeção de detalhes da sessão.
- Visualização de análises baseadas nos dados das sessões.
- Configuração do caminho do banco de dados local na área de configurações.
- Armazenamento local em SQLite, preservando registros históricos.
- Publicação de um executável Windows autônomo para o desktop.

A implementação atual não expõe funcionalidade de importação ou exportação em CSV.

## Capturas de tela

Nenhuma captura de tela está atualmente incluída no repositório. Se o projeto adicionar ativos visuais no futuro, eles devem ser incluídos como caminhos relativos com texto alternativo descritivo e mantidos pequenos e relevantes.

## Stack tecnológica

- C# e .NET 10
- Avalonia UI e XAML
- MVVM com CommunityToolkit.Mvvm
- SQLite com Entity Framework Core
- System.Text.Json para leitura e serialização em JSON
- Injeção de dependência do Microsoft.Extensions.DependencyInjection
- xUnit para testes automatizados
- Scripts em PowerShell para build e publicação

## Arquitetura e estrutura do projeto

```text
TibiaHuntAnalyzer/
├─ README.md
├─ README.pt-BR.md
├─ TibiaHuntAnalyzer.slnx
├─ .gitignore
├─ scripts/
│  ├─ build.ps1
│  ├─ build.sh
│  ├─ run.ps1
│  ├─ run.sh
│  ├─ test.ps1
│  ├─ test.sh
│  └─ publish-windows.ps1
├─ src/
│  ├─ TibiaHuntAnalyzer.Desktop/
│  │  ├─ ViewModels/
│  │  ├─ Views/
│  │  ├─ App.axaml
│  │  ├─ Program.cs
│  │  └─ TibiaHuntAnalyzer.Desktop.csproj
│  ├─ TibiaHuntAnalyzer.Application/
│  ├─ TibiaHuntAnalyzer.Domain/
│  └─ TibiaHuntAnalyzer.Infrastructure/
├─ tests/
│  ├─ TibiaHuntAnalyzer.Tests.Unit/
│  └─ TibiaHuntAnalyzer.Tests.Integration/
├─ docs/
│  ├─ tasks.md
│  ├─ Project Specification.md
│  └─ Feature Changes and Improvements.md
├─ artifacts/
│  └─ windows/
└─ dotnet-tools.json
```

A aplicação está dividida nas camadas padrão do repositório:

- `src/TibiaHuntAnalyzer.Desktop` contém a UI em Avalonia, as views e os ViewModels de tela.
- `src/TibiaHuntAnalyzer.Application` contém serviços de aplicação, DTOs, imports, analytics, configurações e validações.
- `src/TibiaHuntAnalyzer.Domain` contém as entidades principais e regras de negócio.
- `src/TibiaHuntAnalyzer.Infrastructure` contém persistência em SQLite, configuração do EF Core, inicialização do banco e serviços de infraestrutura.
- `tests/` contém os testes automatizados de unidade e integração.
- `docs/` contém a especificação do projeto e a documentação de acompanhamento de tarefas.
- `scripts/` contém a automação para desenvolvimento e publicação.

## Pré-requisitos

### Ambiente de desenvolvimento

- .NET 10 SDK
- PowerShell 5.1+ ou PowerShell 7+
- Máquina Windows para o fluxo oficial de publicação

### Tempo de execução do usuário final

O aplicativo Windows publicado é autônomo e não exige a instalação do SDK .NET no sistema de destino, porém a saída publicada precisa ser executada a partir do diretório gerado.

## Configuração do ambiente e desenvolvimento

1. Clone o repositório.
2. Abra uma sessão do PowerShell na raiz do repositório.
3. Restaure as dependências:

```powershell
dotnet restore .\TibiaHuntAnalyzer.slnx
```

4. Compile a solução:

```powershell
./scripts/build.ps1
```

5. Execute a aplicação:

```powershell
./scripts/run.ps1
```

6. Execute os testes automatizados:

```powershell
./scripts/test.ps1
```

O repositório também inclui scripts shell em `scripts/` para ambientes tipo Linux quando aplicável, porém o fluxo de publicação é documentado como um fluxo do PowerShell no Windows.

## Publicação do executável Windows

O projeto inclui um script de publicação em [`scripts/publish-windows.ps1`](scripts/publish-windows.ps1).

Execute-o em uma sessão do PowerShell no Windows:

```powershell
./scripts/publish-windows.ps1
```

O script publica o projeto desktop em configuração `Release` para `win-x64`, gera um pacote autônomo e grava a saída em:

```text
artifacts/windows/win-x64/
```

O executável gerado é:

```text
artifacts/windows/win-x64/TibiaHuntAnalyzer.Desktop.exe
```

O processo de publicação foi configurado intencionalmente para manter o resultado como um pacote padrão de aplicativo Windows (`PublishSingleFile=false`) e valida a criação do `.exe` antes de retornar sucesso.

## Armazenamento de dados e configuração do banco

A aplicação armazena o banco SQLite e a configuração de execução no diretório local de dados do usuário, em vez do diretório do repositório ou da pasta de instalação.

A localização efetiva é baseada na pasta `LocalApplicationData` do sistema operacional e no diretório específico do aplicativo `TibiaHuntAnalyzer`. A aplicação usa um arquivo de configuração separado para persistir o caminho selecionado do banco e cria ou valida o banco conforme necessário durante a inicialização.

Esse desenho preserva os dados do usuário entre reinicializações do aplicativo e evita gravações em diretórios protegidos de instalação. A aplicação não substitui silenciosamente o banco atual quando o caminho é inválido ou incompatível; ela reporta o problema e mantém a configuração ativa estável até que um caminho válido seja selecionado.

## Guia de uso

Um fluxo típico é:

1. Iniciar o aplicativo.
2. Abrir a tela de configurações para revisar ou alterar o caminho ativo do banco.
3. Gerenciar personagens e locais de hunt.
4. Abrir o formulário de inserção e selecionar um arquivo JSON ou colar o conteúdo textual exportado pelo Tibia Hunt Analyzer.
5. Enviar a sessão após a validação.
6. Usar a tela de consulta para revisar sessões anteriores.
7. Abrir uma visualização de detalhes da sessão para inspecionar estatísticas, monstros mortos e itens lootados.
8. Usar as análises para revisar resumos e métricas derivadas.

O aplicativo foi planejado para operar offline; não há dependência de banco remoto na implementação atual.

## Testes e qualidade

O repositório inclui testes de unidade e integração. Os scripts de teste estão configurados para executar a solução do projeto e a suíte automatizada do repositório.

### Resultados verificados

Os seguintes resultados foram verificados no ambiente atual:

- Build: concluída com sucesso
- Testes de unidade: 80 aprovados, 0 falhos
- Testes de integração: 17 aprovados, 0 falhos
- Total: 97 aprovados, 0 falhos

O projeto também inclui scripts de build e teste em [`scripts/`](scripts) para execução local consistente.

## Solução de problemas

### SDK do .NET não encontrado

Instale o SDK do .NET 10 e verifique que o comando `dotnet` está disponível no `PATH` antes de compilar ou publicar.

### Problemas de caminho do banco ou configuração

Se o caminho configurado do banco for inválido, inacessível ou incompatível, o aplicativo exibirá uma mensagem clara de inicialização ou de configuração em vez de continuar silenciosamente com uma fallback insegura.

### Saída publicada ausente ou falha na publicação

Use uma máquina Windows com o SDK do .NET 10 instalado e execute o script de publicação na raiz do repositório. O script falha com erro se o projeto estiver ausente ou se o executável esperado não for gerado.

### Executando a aplicação a partir do código-fonte

O aplicativo desktop foi pensado para ser executado a partir do projeto de origem durante o desenvolvimento ou a partir da pasta publicada para execução do usuário final. A pasta gerada pela publicação é o artefato de execução esperado para implantação no Windows.

## Contribuição e convenções de desenvolvimento

Este repositório mantém a documentação de especificação e tarefas em [`docs/`](docs). O fluxo atual enfatiza a leitura da especificação antes da implementação, a atualização da documentação relevante quando necessário e a validação do comportamento com testes focados antes de finalizar mudanças.

Arquivos relevantes:

- [docs/Project Specification.md](docs/Project Specification.md)
- [docs/tasks.md](docs/tasks.md)
- [docs/Feature Changes and Improvements.md](docs/Feature Changes and Improvements.md)

## Licença

Este projeto está licenciado sob a MIT License. Consulte [LICENSE](LICENSE) para ver o texto completo.

## Agradecimentos

Este projeto usa os componentes atuais do ecossistema .NET, Avalonia, EF Core e xUnit definidos nos arquivos de projeto do repositório. Não foi identificada nenhuma outra atribuição de terceiros além dessas dependências no material de documentação do repositório.
