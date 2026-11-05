# Roteiro do vídeo (alvo: 10–12 min, máximo 15)

Antes de gravar: Learner Lab ativo e `GET /health` respondendo nas 3 portas (30081/30082/30083). Rodar `scripts/e2e-saga.ps1` uma vez para validar.

| Tempo | O que mostrar | O que falar |
|---|---|---|
| 0:00–1:00 | Slide/README: nome, grupo, objetivo | Contexto da oficina e da Fase 4 |
| 1:00–3:00 | `docs/img/arquitetura.png` | 3 microsserviços, banco por serviço (SQL + DynamoDB), SNS/SQS, K3s |
| 3:00–4:30 | `docs/img/saga.png` | Saga orquestrada, por que orquestrada, onde ficam as compensações |
| 4:30–5:30 | AWS Console: SNS (tópico + 3 assinaturas), SQS (3 filas), DynamoDB (2 tabelas), RDS | Mensageria e bancos reais provisionados |
| 5:30–6:30 | GitHub Actions dos 3 repos (runs verdes), job de testes com cobertura | CI/CD por serviço, quality gate |
| 6:30–7:30 | Terminal: `kubectl get pods -A` na EC2 | 3 namespaces, 2 réplicas cada, HPA |
| 7:30–9:30 | **Demo caminho feliz** (`scripts/e2e-saga.ps1` ou Postman) | OS → orçamento → pagamento → execução → Finalizada |
| 9:30–11:30 | **Demo compensação** (cenários A e B) | Pagamento rejeitado e falha na execução -> OS Cancelada e cobrança cancelada |
| 11:30–12:30 | Logs: `kubectl logs` com o mesmo CorrelationId nos 3 serviços | Rastreabilidade |
| 12:30–13:30 | Sonar/cobertura, testes BDD no CI | Qualidade |
| 13:30–14:30 | Débitos técnicos (seção 8 do relatório) | Honestidade sobre limitações |

Dica: mostrar a resposta de cada etapa (status numérico: 1 Recebida, 2 EmDiagnostico, 3 AguardandoAprovacao, 4 EmExecucao, 5 Finalizada, 7 Cancelada).
