# Infra de mensageria e DynamoDB (Fase 4)

Provisiona: tópico SNS `oficina-wagyu-saga-events`, 3 filas SQS (os, billing,
execucao) com filtro por `EventType`, e as tabelas DynamoDB
`oficina-wagyu-orcamentos` e `oficina-wagyu-execucoes`.

```bash
terraform init && terraform apply
terraform output   # ARN do tópico e URLs das filas -> vão para os Secrets do K8s
```
