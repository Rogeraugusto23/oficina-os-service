# ADR-006: Decomposição em microsserviços (Fase 4)

## Status
Aceito

## Contexto
A Fase 4 exige evoluir o monólito (já em Clean Architecture, rodando em
Kubernetes na AWS desde a Fase 3) para uma arquitetura de microsserviços,
com Saga Pattern para orquestrar o fluxo de ponta a ponta, mensageria
assíncrona entre serviços, e a regra explícita de que **nenhum serviço pode
acessar diretamente o banco de dados de outro serviço**.

## Decisão
O domínio foi dividido em três microsserviços, seguindo os limites naturais
do processo de negócio (abrir OS → orçar → aprovar → executar → finalizar):

1. **OS Service** (este repositório) — dono do ciclo de vida da Ordem de
   Serviço, de Clientes e de Veículos. Reaproveita a implementação Clean
   Architecture já existente no monólito (Domain/Application/Infrastructure/
   API), com banco SQL Server (RDS) próprio. Inicia a Saga ao abrir uma OS e
   avança/encerra a Saga ao aprovar ou rejeitar o orçamento.
2. **Billing Service** — dono do orçamento e do pagamento (integração
   Mercado Pago). Consome `OrdemServicoAbertaEvent`/`OrcamentoEnviadoEvent` e
   publica eventos de cobrança. Banco NoSQL (DynamoDB) próprio.
3. **Execução Service** — dono da fila/execução do serviço mecânico em si.
   Consome `OrcamentoAprovadoEvent` e publica `ExecucaoFinalizadaEvent` (
   sucesso ou falha) de volta para o OS Service. Banco NoSQL (DynamoDB)
   próprio.

Cada serviço:
- tem seu próprio repositório Git, pipeline de CI/CD independente e banco de
  dados — comunicação entre serviços é **sempre** via eventos assíncronos
  (SNS/SQS), nunca via chamada direta a banco ou API síncrona para dados que
  pertencem a outro serviço;
- reutiliza o segredo JWT compartilhado (Fase 3) para autenticação, e o
  padrão de CorrelationId/Serilog para rastreabilidade — essencial aqui
  porque uma única Saga atravessa logs de três serviços diferentes;
- é implantado no mesmo cluster K3s (EC2) provisionado na Fase 3, em um
  namespace Kubernetes próprio, evitando o custo e a complexidade de operar
  três clusters separados dentro do prazo e do escopo do desafio.

## Por que orquestrado (e não coreografado)
Ver [RFC correspondente] (a ser detalhado nos Dias 10-11): um orquestrador
central no OS Service simplifica o raciocínio sobre falhas e compensação
(cancelar OS, estornar cobrança) comparado a uma coreografia pura, com o
trade-off de um acoplamento um pouco maior do OS Service ao fluxo da Saga —
aceitável dado o tamanho do domínio.

## Consequências
- Cliente e Veículo, que não são um dos três serviços "oficiais" do desafio,
  ficam sob responsabilidade do OS Service (é o serviço que mais depende
  deles para abrir uma OS).
- Qualquer alteração de orçamento/pagamento ou de execução deixa de ser uma
  transação local — passa a depender de mensageria confiável e de lógica de
  compensação explícita, tratada nos Dias 10-11 do roadmap da Fase 4.
