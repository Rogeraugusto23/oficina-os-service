# Mensageria e bancos NoSQL compartilhados pela Saga da Fase 4.
# Um único tópico SNS recebe todos os eventos de integração; cada serviço tem
# sua fila SQS, filtrada por atributo EventType (raw delivery: o corpo da
# mensagem é o JSON do evento e EventType chega como atributo SQS).

resource "aws_sns_topic" "saga_events" {
  name = "oficina-wagyu-saga-events"
}

locals {
  queues = {
    os = {
      name   = "oficina-wagyu-os-queue"
      events = ["PagamentoAprovadoEvent", "PagamentoRejeitadoEvent", "ExecucaoFinalizadaEvent"]
    }
    billing = {
      name   = "oficina-wagyu-billing-queue"
      events = ["OrcamentoEnviadoEvent", "OrcamentoRejeitadoEvent", "OrdemServicoCanceladaEvent"]
    }
    execucao = {
      name   = "oficina-wagyu-execucao-queue"
      events = ["OrcamentoAprovadoEvent"]
    }
  }
}

resource "aws_sqs_queue" "q" {
  for_each                   = local.queues
  name                       = each.value.name
  visibility_timeout_seconds = 60
  receive_wait_time_seconds  = 10
}

resource "aws_sqs_queue_policy" "allow_sns" {
  for_each  = local.queues
  queue_url = aws_sqs_queue.q[each.key].id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.q[each.key].arn
      Condition = { ArnEquals = { "aws:SourceArn" = aws_sns_topic.saga_events.arn } }
    }]
  })
}

resource "aws_sns_topic_subscription" "sub" {
  for_each             = local.queues
  topic_arn            = aws_sns_topic.saga_events.arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.q[each.key].arn
  raw_message_delivery = true
  filter_policy        = jsonencode({ EventType = each.value.events })
  depends_on           = [aws_sqs_queue_policy.allow_sns]
}

resource "aws_dynamodb_table" "orcamentos" {
  name         = "oficina-wagyu-orcamentos"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"
  attribute {
    name = "Id"
    type = "S"
  }
}

resource "aws_dynamodb_table" "execucoes" {
  name         = "oficina-wagyu-execucoes"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "Id"
  attribute {
    name = "Id"
    type = "S"
  }
}
