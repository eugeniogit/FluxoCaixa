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

### Estrutura de Pastas

Cada projeto segue uma organização clara que combina VSA com separação de responsabilidades. Exemplo:

````
FluxoCaixa.Lancamento/
├── Configuration/               # Configurações e constantes
├── Domain/                      # Entidades de domínio e eventos
│   ├── Lancamento.cs
│   └── LancamentoEvent.cs
├── Features/                    # Vertical Slices (funcionalidades)
│   ├── CriarLancamento/
│   ├── ListarLancamentos/
│   └── ConsolidarLancamentos/
├── Infrastructure/              # Implementações técnicas
│   ├── Database/
│   └── Messaging/
├── Extensions/                  # Extension methods por tecnologia

### Integração

As aplicações são integradas através de **RabbitMQ**:

1. **FluxoCaixa.Lancamento** publica eventos na fila `lancamento_events` quando um lançamento é criado
2. **FluxoCaixa.Consolidado** consome os eventos da fila e atualiza as consolidações
3. **FluxoCaixa.Consolidado** também pode consultar lançamentos via API REST quando necessário

### Decisões Técnicas

Para entender as decisões arquiteturais e técnicas tomadas durante o desenvolvimento do projeto, consulte os documentos de Architecture Decision Records (ADRs):

📋 **[Documentos de Decisões Técnicas](docs/adrs/)**

- [ADR-001: Adoção de Arquitetura Serverless](docs/adrs/ADR-001.md) - Decisão sobre arquitetura serverless, banco de dados relacional e estratégias de resiliência

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
````

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

- **Swagger UI**: `https://localhost:60277/swagger`
- **Health Check**: `https://localhost:60277/health`

#### FluxoCaixa.Consolidado

- **Swagger UI**: `https://localhost:60278/swagger`
- **Health Check**: `https://localhost:60278/health`

## 🔐 Autenticação

O serviço **FluxoCaixa.Lancamento** utiliza autenticação por **API Key**.

### Justificativa da Escolha

A autenticação por API Key foi escolhida considerando que o cenário de integração será **exclusivamente em rede privada** (comunicação entre microsserviços internos).

**Cenário alvo:**

- **Rede privada/interna**: Serviços executando em ambiente controlado (Docker, Kubernetes, VPC)
- **Comunicação service-to-service**: Integração entre FluxoCaixa.Consolidado e FluxoCaixa.Lancamento
- **Ambiente confiável**: Sem exposição direta à internet pública

**Benefícios da API Key neste contexto:**

- ✅ **Simplicidade**: Implementação e configuração diretas
- ✅ **Performance**: Baixo overhead de validação
- ✅ **Auditoria**: Rastreabilidade de acesso por serviço
- ✅ **Rotação**: Fácil troca de chaves quando necessário

**Alternativas consideradas (não aplicáveis ao cenário):**

- **OAuth 2.0 / JWT**: Overhead desnecessário para comunicação interna
- **mTLS**: Complexidade adicional quando API Key é suficiente
- **Service Mesh Auth**: Não aplicável sem Istio/Linkerd

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

O projeto inclui uma suíte completa de testes unitários e de integração para garantir a qualidade e confiabilidade do código.

### Testes Unitários

Os testes unitários cobrem todas as features, handlers, validadores e modelos de domínio de ambas as aplicações:

- **FluxoCaixa.Lancamento.UnitTests** - Testes para todas as features do serviço de lançamentos
- **FluxoCaixa.Consolidado.UnitTests** - Testes para todas as features do serviço de consolidação

#### Executar Testes Unitários

```bash
# Executar todos os testes unitários
dotnet test tests/FluxoCaixa.Lancamento.UnitTests/ --verbosity normal
dotnet test tests/FluxoCaixa.Consolidado.UnitTests/ --verbosity normal
```

### Testes de Integração

O projeto inclui testes de integração abrangentes usando **TestContainers** para criar ambientes isolados.

#### Executar Testes de Integração

```bash
# Compilar solução
dotnet build --configuration Release

# Executar todos os testes (unitários + integração)
dotnet test --verbosity normal

# Executar apenas testes de integração
dotnet test tests/FluxoCaixa.Lancamento.IntegrationTests/FluxoCaixa.Lancamento.IntegrationTests.csproj --verbosity normal
dotnet test tests/FluxoCaixa.Consolidado.IntegrationTests/FluxoCaixa.Consolidado.IntegrationTests.csproj --verbosity normal
```

## 📊 Observabilidade

O projeto possui configuração **inicial** de observabilidade utilizando OpenTelemetry, Prometheus e Grafana, mas está **incompleta** por questões de tempo e complexidade de configuração.

### Estado Atual

#### ✅ **Implementado**

**OpenTelemetry Configuration:**

- **Tracing**: Instrumentação automática para ASP.NET Core, HttpClient e Entity Framework
- **Metrics**: Coleta de métricas básicas de performance das aplicações
- **Resource Builder**: Identificação correta dos serviços ("FluxoCaixa.Lancamento" e "FluxoCaixa.Consolidado")
- **OTLP Exporter**: Configurado para enviar traces para collector (porta 4318)
- **Prometheus Exporter**: Métricas expostas no endpoint `/metrics`

**Infraestrutura (Docker Compose):**

- **Prometheus**: Configurado na porta 9090 para coleta de métricas
- **Grafana**: Interface de visualização na porta 3000 (admin/admin)
- **OpenTelemetry Collector**: Preparado para receber e processar telemetria

#### ⚠️ **Incompleto/Pendente**

1. **OpenTelemetry Collector**

   - Arquivo `otel-collector-config.yaml` existe mas não está totalmente configurado
   - Falta integração completa entre collector, Prometheus e outros backends

2. **Dashboards Grafana**

   - Pasta `grafana/provisioning` existe mas dashboards não foram criados
   - Não há visualizações prontas para métricas de negócio

3. **Métricas Customizadas**

   - Apenas métricas padrão do OpenTelemetry estão sendo coletadas
   - Faltam métricas de negócio específicas (ex: lançamentos por período, consolidações por comerciante)
   - Não há instrumentação manual para eventos críticos

4. **Logging Estruturado**

   - Logs não estão integrados com stack de observabilidade
   - Falta correlação entre logs, traces e métricas
   - Não há coleta centralizada de logs

5. **Tracing Distribuído Completo**
   - Propagação de trace context entre serviços via RabbitMQ não configurada
   - Falta instrumentação manual para operações críticas
   - Correlation IDs não implementados

### Como Acessar (Estado Atual)

```bash
# Após iniciar a infraestrutura com docker-compose up -d

# Métricas Prometheus das aplicações
curl http://localhost:60280/metrics  # FluxoCaixa.Lancamento
curl http://localhost:60281/metrics  # FluxoCaixa.Consolidado

# Prometheus UI
http://localhost:9090

# Grafana (admin/admin)
http://localhost:3000
```
