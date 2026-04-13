# MeuDinDin

Aplicacao web para controle financeiro familiar, com foco em visibilidade do que ja esta comprometido no mes e do que ainda sobra para investir.

## O que o projeto faz

O MeuDinDin ajuda a organizar a vida financeira da familia em um fluxo simples:

- configuracao inicial da familia no primeiro acesso
- cadastro de receitas recorrentes e avulsas
- controle de despesas fixas e variaveis
- acompanhamento de compras no cartao e parcelas futuras
- simulacao de compras antes de assumir novos compromissos
- painel com saldo, metas, alertas e projecao dos proximos meses

## Stack

- ASP.NET Core
- Blazor Server
- Entity Framework Core
- SQLite

## Como rodar localmente

Pre-requisitos:

- .NET SDK 10

Na raiz do repositorio:

```powershell
dotnet run --project .\MeuDinDin\MeuDinDin.csproj
```

Depois abra a URL exibida no terminal.

## Primeiro uso

O sistema usa um banco SQLite local em `MeuDinDin/App_Data/meudindin.db`.

Quando o banco esta vazio, o sistema abre no fluxo de configuracao inicial para criar:

- grupo familiar
- membros iniciais
- metas mensais
- saldos iniciais
- categorias padrao

## Resetar os dados locais

Para voltar o sistema ao estado de primeiro uso, apague o banco local:

```powershell
Remove-Item .\MeuDinDin\App_Data\meudindin.db -Force
```

Se existir algum backup local com dados reais, apague tambem antes de compartilhar o projeto.

Na proxima execucao, a estrutura do banco sera recriada automaticamente e o app voltara para a configuracao inicial.

## Rodando com Docker

O projeto tambem pode ser executado com Docker Compose:

```powershell
docker compose up --build
```

Configuracao atual:

- porta `8080`
- volume persistente para `/app/App_Data`

## Estrutura principal

- `MeuDinDin/Program.cs`: inicializacao da aplicacao e do banco
- `MeuDinDin/Data`: contexto, entidades e schema do SQLite
- `MeuDinDin/Services`: regra de negocio e agregacao financeira
- `MeuDinDin/Components`: paginas e componentes da interface

## Objetivo

O foco do projeto e ser uma base simples e util para uso pessoal ou familiar, sem depender de servicos externos para comecar.
