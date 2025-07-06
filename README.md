# FluxoCaixa - Sistema de Controle de Caixa

## 📋 Visão Geral

O **FluxoCaixa** é um sistema distribuído de controle de caixa composto por duas aplicações independentes que se comunicam através de mensageria:

- **FluxoCaixa.Lancamento** - Serviço responsável pelo registro e gerenciamento de lançamentos financeiros
- **FluxoCaixa.Consolidado** - Serviço responsável pela consolidação e agregação dos lançamentos

## 🏗️ Arquitetura

### Padrão Arquitetural

Os serviços foram desenvolvidos utilizando **Vertical Slice Architecture** (VSA), uma abordagem que organiza o código por funcionalidades completas (slices) em vez de camadas técnicas tradicionais.

**Por que VSA?**

- **Simplicidade**: Aplicação pequena e focada, sem necessidade de complexidade desnecessária
- **Coesão**: Cada feature agrupa todos os elementos relacionados (request, handler, validação, etc.)
- **Manutenibilidade**: Mudanças em uma funcionalidade ficam isoladas em um slice específico
- **Sem Over-engineering**: Evita a criação de projetos em camadas desnecessárias ou DDD complexo

### Aplicações

#### FluxoCaixa.Lancamento

- **Propósito**: Cadastro e consulta de lançamentos financeiros
- **Banco de Dados**: MongoDB
- **Porta**: 60280 (HTTP)
- **Tecnologias**: ASP.NET Core 8, Minimal API, MongoDB.Driver
- **Autenticação**: API Key Authentication

#### FluxoCaixa.Consolidado

- **Propósito**: Consolidação diária dos lançamentos por comerciante
- **Banco de Dados**: PostgreSQL
- **Porta**: 60281 (HTTP)
- **Tecnologias**: ASP.NET Core 8, Minimal API, Entity Framework Core
- **Processamento**: Background jobs com Quartz.NET

### Integração

As aplicações são integradas através de **RabbitMQ**:

1. **FluxoCaixa.Lancamento** publica eventos na fila `lancamento_events` quando um lançamento é criado
2. **FluxoCaixa.Consolidado** consome os eventos da fila e atualiza as consolidações
3. **FluxoCaixa.Consolidado** também pode consultar lançamentos via API REST quando necessário

## 🚀 Como Executar Localmente

### Pré-requisitos

- **.NET 8 SDK**
- **Docker Desktop** (para bancos de dados e RabbitMQ)

### 1. Iniciar Infraestrutura

```bash
# Navegar para o diretório src
cd src

# Iniciar MongoDB, PostgreSQL e RabbitMQ
docker-compose up -d
```

Serviços disponíveis:

- **MongoDB**: `localhost:27017` (admin/password)
- **PostgreSQL**: `localhost:5432` (admin/password)
- **RabbitMQ**: `localhost:5672` (admin/password)
- **RabbitMQ Management**: `http://localhost:15672` (admin/password)

### 2. Executar Aplicações

#### FluxoCaixa.Lancamento

```bash
# Terminal 1
cd src/FluxoCaixa.Lancamento
dotnet run
```

#### FluxoCaixa.Consolidado

```bash
# Terminal 2
cd src/FluxoCaixa.Consolidado
dotnet run
```

### 3. Verificar Funcionamento

#### FluxoCaixa.Lancamento

- **Swagger UI**: `http://localhost:60280/swagger`
- **Health Check**: `http://localhost:60280/health`

#### FluxoCaixa.Consolidado

- **Swagger UI**: `http://localhost:60281/swagger`
- **Health Check**: `http://localhost:60281/health`

## 🔐 Autenticação

O serviço **FluxoCaixa.Lancamento** utiliza autenticação por API Key.

### Cabeçalho Obrigatório

```http
X-API-Key: sua-api-key-aqui
```

### API Keys Configuradas

| Nome                | Chave                                        | Uso                        |
| ------------------- | -------------------------------------------- | -------------------------- |
| Consolidado Service | `fluxocaixa-consolidado-2024-api-key-secure` | Comunicação entre serviços |
| Admin Client        | `fluxocaixa-admin-2024-api-key-secure`       | Clientes administrativos   |

### Endpoints Protegidos

- `POST /api/lancamentos` - Criar lançamento
- `GET /api/lancamentos` - Listar lançamentos

### Endpoints Públicos

- `GET /health` - Health check
- `GET /health/ready` - Readiness check

### Processamento Automático

O sistema inclui um **job automático** que executa diariamente às **01:00 AM**:

1. **Deleta** todas as consolidações existentes do dia anterior
2. **Reconsolida** todos os lançamentos do dia anterior
3. **Garante** que os dados estão sempre atualizados e corretos

## 🧪 Testes

### Testes de Integração

O projeto inclui testes de integração abrangentes usando **TestContainers** para criar ambientes isolados.

#### Executar Testes

```bash
# Compilar solução
dotnet build --configuration Release

# Executar todos os testes
dotnet test --verbosity normal

# Executar testes específicos
dotnet test tests/FluxoCaixa.Lancamento.IntegrationTests/FluxoCaixa.Lancamento.IntegrationTests.csproj --verbosity normal
dotnet test tests/FluxoCaixa.Consolidado.IntegrationTests/FluxoCaixa.Consolidado.IntegrationTests.csproj --verbosity normal
```

## 🔧 Configuração

### Variáveis de Ambiente

As aplicações podem ser configuradas via `appsettings.json` ou variáveis de ambiente:
