<#
 Teste ponta a ponta da Saga (Fase 4) - caminho feliz + 2 compensações.
 Uso:  .\e2e-saga.ps1 -Ip 100.56.16.190
 Requer PowerShell 5+ e acesso às portas 30081/30082/30083 da EC2.
#>
param(
  [Parameter(Mandatory=$true)][string]$Ip,
  [string]$JwtSecret = "ChaveSecretaOficinaWagyu2026_MuitoLonga",
  [string]$WebhookSecret = "wagyu-mp-webhook-2026"
)
$ErrorActionPreference = "Stop"
$uOS="http://${Ip}:30081"; $uBI="http://${Ip}:30082"; $uEX="http://${Ip}:30083"

function B64($b){[Convert]::ToBase64String($b).TrimEnd('=').Replace('+','-').Replace('/','_')}
$jh=B64([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
$exp=[DateTimeOffset]::UtcNow.AddHours(2).ToUnixTimeSeconds()
$jp=B64([Text.Encoding]::UTF8.GetBytes("{`"sub`":`"e2e`",`"role`":`"Admin`",`"exp`":$exp}"))
$hm=New-Object Security.Cryptography.HMACSHA256 (,[Text.Encoding]::ASCII.GetBytes($JwtSecret))
$js=B64($hm.ComputeHash([Text.Encoding]::ASCII.GetBytes("$jh.$jp")))
$hdr=@{Authorization="Bearer $jh.$jp.$js"}
function J($o){$o|ConvertTo-Json -Depth 4 -Compress}
function Post($url,$body){ if($body){Invoke-RestMethod $url -Method Post -Headers $hdr -ContentType application/json -Body (J $body)} else {Invoke-RestMethod $url -Method Post -Headers $hdr} }
function StatusOS($id){ (Invoke-RestMethod "$uOS/api/OrdensServico/$id" -Headers $hdr).status }
function Check($nome,$obtido,$esperado){ $ok = ($obtido -eq $esperado); $cor = if($ok){"Green"}else{"Red"}; Write-Host ("{0,-55} obtido={1}  esperado={2}  {3}" -f $nome,$obtido,$esperado,$(if($ok){"OK"}else{"FALHOU"})) -ForegroundColor $cor; if(-not $ok){$script:falhas++} }
$script:falhas=0

# Massa de teste
$cli=@(Invoke-RestMethod "$uOS/api/Clientes" -Headers $hdr)[0]
if(-not $cli){ $cli=Post "$uOS/api/Clientes" @{nome="Cliente E2E";documento="52998224725"} }
$letras=-join ((65..90)|Get-Random -Count 3|ForEach-Object{[char]$_})
$placa="$letras$(Get-Random -Min 0 -Max 9)$([char](Get-Random -Min 65 -Max 90))$(Get-Random -Min 10 -Max 99)"
$vei=Post "$uOS/api/Veiculos" @{placa=$placa;marca="Fiat";modelo="Uno";ano=2020;clienteId=$cli.id}

function NovaOS {
  $o=Post "$uOS/api/OrdensServico" @{clienteId=$cli.id;veiculoId=$vei.id;servicos=@(@{descricao="Revisao";preco=300})}
  Invoke-RestMethod "$uOS/api/OrdensServico/$($o.id)/status" -Method Put -Headers $hdr | Out-Null
  Post "$uOS/api/OrdensServico/$($o.id)/enviar-orcamento" $null | Out-Null
  Start-Sleep 12
  return $o.id
}
function Pagar($id,$ok,$motivo){
  $b=Invoke-RestMethod "$uBI/api/Orcamentos/ordem-servico/$id" -Headers $hdr
  Invoke-RestMethod "$uBI/webhook/mercadopago?secret=$WebhookSecret" -Method Post -ContentType application/json -Body (J @{preferenceId=$b.mercadoPagoPreferenceId;paymentId="PAY-$id";aprovado=$ok;motivoRejeicao=$motivo}) | Out-Null
  Start-Sleep 15
}
function Executar($id,$sucesso,$motivo){
  $e=Invoke-RestMethod "$uEX/api/Execucoes/ordem-servico/$id" -Headers $hdr
  Post "$uEX/api/Execucoes/$($e.id)/iniciar" $null | Out-Null
  Post "$uEX/api/Execucoes/$($e.id)/finalizar" @{sucesso=$sucesso;motivoFalha=$motivo} | Out-Null
  Start-Sleep 15
}

Write-Host "`n=== 1. Caminho feliz ===" -ForegroundColor Cyan
$a=NovaOS; Write-Host "OS: $a"
Pagar $a $true $null
Check "OS apos pagamento aprovado (4=EmExecucao)" (StatusOS $a) 4
Executar $a $true $null
Check "OS apos execucao concluida (5=Finalizada)" (StatusOS $a) 5

Write-Host "`n=== 2. Compensacao: pagamento rejeitado ===" -ForegroundColor Cyan
$b=NovaOS; Write-Host "OS: $b"
Pagar $b $false "Cartao recusado"
Check "OS apos pagamento rejeitado (7=Cancelada)" (StatusOS $b) 7
Check "Cobranca (4=Cancelado)" (Invoke-RestMethod "$uBI/api/Orcamentos/ordem-servico/$b" -Headers $hdr).status 4

Write-Host "`n=== 3. Compensacao: falha na execucao ===" -ForegroundColor Cyan
$c=NovaOS; Write-Host "OS: $c"
Pagar $c $true $null
Executar $c $false "Peca indisponivel"
Check "OS apos falha de execucao (7=Cancelada)" (StatusOS $c) 7
Check "Cobranca estornada (4=Cancelado)" (Invoke-RestMethod "$uBI/api/Orcamentos/ordem-servico/$c" -Headers $hdr).status 4

Write-Host ""
if($script:falhas -eq 0){ Write-Host "SAGA OK - todos os cenarios passaram" -ForegroundColor Green } else { Write-Host "$($script:falhas) verificacao(oes) falharam" -ForegroundColor Red; exit 1 }
