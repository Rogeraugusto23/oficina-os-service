# language: pt
Funcionalidade: Saga de Ordem de Serviço
  Como oficina, quero que o fluxo de aprovação, pagamento e execução de uma
  ordem de serviço seja coordenado por eventos, com compensação automática
  quando o pagamento ou a execução falham.

  Cenário: Pagamento aprovado leva a OS para execução
    Dado uma ordem de serviço com status "AguardandoAprovacao"
    Quando o evento de pagamento aprovado é recebido
    Então a ordem de serviço deve estar com status "EmExecucao"
    E o evento "OrcamentoAprovadoEvent" deve ter sido publicado

  Cenário: Pagamento rejeitado cancela a OS (compensação)
    Dado uma ordem de serviço com status "AguardandoAprovacao"
    Quando o evento de pagamento rejeitado é recebido com o motivo "Cartão recusado"
    Então a ordem de serviço deve estar com status "Cancelada"
    E o evento "OrcamentoRejeitadoEvent" deve ter sido publicado

  Cenário: Execução concluída finaliza a OS
    Dado uma ordem de serviço com status "EmExecucao"
    Quando a execução é finalizada com sucesso
    Então a ordem de serviço deve estar com status "Finalizada"

  Cenário: Falha na execução cancela a OS e pede o estorno ao Billing (compensação)
    Dado uma ordem de serviço com status "EmExecucao"
    Quando a execução é finalizada com falha "Peça indisponível"
    Então a ordem de serviço deve estar com status "Cancelada"
    E o evento "OrdemServicoCanceladaEvent" deve ter sido publicado

  Esquema do Cenário: Eventos fora de ordem ou duplicados são ignorados
    Dado uma ordem de serviço com status "<status>"
    Quando o evento de pagamento aprovado é recebido
    Então a ordem de serviço deve estar com status "<status>"
    E nenhum evento deve ter sido publicado

    Exemplos:
      | status      |
      | Recebida    |
      | EmExecucao  |
      | Finalizada  |
