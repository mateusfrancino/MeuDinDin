# MeuDinDin

Aplicacao web para controle financeiro familiar, com foco em visibilidade do que ja esta comprometido no mes e do que ainda sobra para investir.

## Stack

- ASP.NET Core
- Blazor Server
- Entity Framework Core
- PostgreSQL
- Docker Compose

## O que o projeto faz

- cadastro e login por usuario
- separacao dos dados por familia
- compartilhamento da mesma base entre membros da mesma familia
- cadastro de receitas recorrentes e avulsas
- controle de despesas fixas e variaveis
- acompanhamento de compras no cartao e parcelas futuras
- simulacao de compras antes de assumir novos compromissos
- painel com saldo, metas, alertas e projecao dos proximos meses

## Variaveis de ambiente

O app procura a conexao do banco nesta ordem:

1. `ConnectionStrings__DefaultConnection`
2. `MEUDINDIN_DATABASE_URL`

Outras variaveis relevantes:

- `MEUDINDIN_KEYS_DIR`: pasta persistente para as chaves de autenticacao
- `ASPNETCORE_ENVIRONMENT`: use `Production` no servidor
- `ASPNETCORE_URLS`: por padrao o container sobe em `http://+:8080`

## Rodando localmente com .NET

Pre-requisitos:

- .NET SDK 10
- PostgreSQL disponivel

Exemplo em PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=meudindin;Username=meudindin;Password=troque-a-senha"
dotnet run --project .\MeuDinDin\MeuDinDin.csproj
```

## Rodando com Docker Compose

1. Copie o arquivo de exemplo:

```powershell
Copy-Item .\.env.example .\.env
```

2. Edite a senha do Postgres em `.env`.

3. Suba os containers:

```powershell
docker compose up -d --build
```

Configuracao atual do compose:

- app em `127.0.0.1:8080`
- Postgres em container separado
- volume persistente para o banco
- volume persistente para as chaves de autenticacao

## Publicando no Debian 12

Exemplo de fluxo no servidor:

```bash
mkdir -p /opt/meudindin
cd /opt/meudindin
git clone <SEU-REPOSITORIO> .
cp .env.example .env
nano .env
docker compose up -d --build
```

Comandos uteis:

```bash
docker compose ps
docker compose logs -f meudindin
docker compose logs -f postgres
docker compose pull
docker compose up -d --build
```

## Expondo para a internet

Recomendacao para o seu cenario:

- manter o app publicado apenas em `127.0.0.1:8080`
- usar `cloudflared` no host Debian apontando para `http://localhost:8080`
- exemplo de configuracao em `deploy/cloudflared/config.yml.example`

Assim voce nao expoe a porta do app diretamente na internet.

## Primeiro uso

Quando o banco estiver vazio:

- o primeiro usuario cria a conta
- escolhe criar uma nova familia
- o sistema gera um codigo da familia
- os demais membros criam seus logins e entram com esse codigo

## Observacoes importantes

- esta versao ja esta preparada para PostgreSQL
- o projeto antigo em SQLite nao e migrado automaticamente para Postgres
- se houver dados no SQLite que voce queira preservar, a migracao precisa ser feita separadamente
- as chaves de autenticacao precisam ficar em volume persistente para nao invalidar login a cada restart do container
