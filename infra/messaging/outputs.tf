output "sns_topic_arn" {
  value = aws_sns_topic.saga_events.arn
}

output "queue_urls" {
  value = { for k, q in aws_sqs_queue.q : k => q.url }
}
