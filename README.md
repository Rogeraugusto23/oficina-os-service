# oficina-os-service

Microsserviço de **Ordens de Serviço** da Oficina Mecânica Wagyu — Tech
Challenge Fase 4. Extraído do monólito da Fase 2/3 (`OficinaMecanicaWagyu`),
mantendo a mesma Clean Architecture (Domain → Application → Infrastructure →
API), agora como serviço independente com banco próprio.

Ver [ADR-006](docs/adr/ADR-006-decomposicao-em-microsservicos.md) para a
justificativa completa da decomposição em microsserviços.

## Responsabilidades

- Ciclo de vida da **Ordem de Serviço** (abrir, diagnosticar, orçar, aprovar/
  rejeitar orçamento, avançar status, atualizar status via webhook de
  e-mail — mesma regra de negócio da Fase 2/3)
- Cadastro de **Clientes** e **Veículos**
- **Orquestrador da Saga**: publica os eventos que disparam o Billing
  Service (orçamento/pagamento) e o Execução Service (execução mecânica), e
  reage a eventos desses serviços para avançar ou compensar a OS

## Arquitetura

```
src/
  OsService.Domain/          Entidades, enums, interfaces de repositório,
                              eventos de integração (sem dependência de nada)
  OsService.Application/     Use Cases, DTOs, OperationResult<T>,
                              IEventPublisher (depende só do Domain)
  OsService.Infrastructure/  EF Core (SQL Server), publisher SNS
  OsService.API/             Controllers, Program.cs, JWT, Serilog
tests/
  OsService.Tests/           xUnit + FluentAssertions + NSubstitute
```

## Saga (orquestrada pelo OS Service)

```
POST /api/OrdensServico                  → OrdemServicoAbertaEvent     ──┐
                                                                          ▼
                                                        Billing Service gera orçamento
POST /api/OrdensServico/{id}/enviar-orcamento → OrcamentoEnviadoEvent
POST /api/OrdensServico/{id}/aprovar-orcamento → OrcamentoAprovadoEvent ──┐
                                                                          ▼
                                                        Execução Service inicia execução
POST /api/OrdensServico/{id}/rejeitar-orcamento → OrcamentoRejeitadoEvent
                                                   (compensação: Billing cancela cobrança)

(consumido) ExecucaoFinalizadaEvent ← Execução Service
```

Todos os eventos carregam `CorrelationId = OrdemServico.Id`, propagado junto
com o `X-Correlation-Id` HTTP (middleware reaproveitado da Fase 3), para
rastrear a saga inteira nos logs estruturados dos três serviços.

Os eventos são publicados via **Amazon SNS** (`IEventPublisher` →
`SnsEventPublisher`), com fan-out para as filas SQS de cada serviço
consumidor — nenhum serviço acessa o banco de outro diretamente.

## Banco de dados

SQL Server dedicado (RDS), **não compartilhado** com Billing Service ou
Execução Service (que usam DynamoDB). Migrations do EF Core rodam
automaticamente na inicialização (`db.Database.Migrate()`), mesmo padrão da
Fase 3.

## Rodando localmente

```bash
cd src/OsService.API
dotnet restore
dotnet run
```

Requer SQL Server local/Docker (connection string em `appsettings.json`,
sobrescrita por `ConnectionStrings__DefaultConnection` em produção). Sem um
tópico SNS configurado (`Messaging__SnsTopicArn`), os eventos são apenas
logados — útil para desenvolvimento local sem AWS.

## Testes

```bash
dotnet test
```

Use cases que publicam eventos de Saga são testados com um `FakeEventPublisher`
(dublê de teste, sem dependência de AWS) — ver `tests/OsService.Tests/Fakes/`.

## Deploy

CI/CD em `.github/workflows/ci-cd.yml`: build + testes + (futuramente) gate
de qualidade via SonarQube, e deploy automático por SSH no mesmo cluster K3s
(EC2) provisionado na Fase 3 — namespace Kubernetes próprio
(`oficina-os-service`), reaproveitando a infraestrutura já paga/provisionada
em vez de um cluster dedicado.

Segredos necessários no repositório (GitHub Secrets): `EC2_HOST`,
`EC2_SSH_KEY` (mesmos da Fase 3) e, quando o quality gate for ligado,
`SONAR_TOKEN`.

O Secret do Kubernetes com os valores reais (connection string, segredo JWT
compartilhado, ARN do tópico SNS) é criado manualmente — nunca commitado —
seguindo `k8s/02-secret.example.yaml`.

## Status

🚧 Scaffolding e código portado do monólito (Dias 1-3 do roadmap da Fase 4).
Pendente: provisionar o tópico SNS/filas SQS reais via Terraform, aplicar o
Secret no cluster e validar o deploy de ponta a ponta — próximos passos do
roadmap, junto com os serviços Billing e Execução.
