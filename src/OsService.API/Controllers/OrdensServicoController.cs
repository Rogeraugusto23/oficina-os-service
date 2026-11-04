using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Application.UseCases.OrdensServico;

namespace OsService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdensServicoController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AbrirOrdemServicoUseCase _abrirOrdemServico;
        private readonly ListarOrdensServicoUseCase _listarOrdensServico;
        private readonly ConsultarOrdemServicoUseCase _consultarOrdemServico;
        private readonly AvancarStatusUseCase _avancarStatus;
        private readonly EnviarOrcamentoUseCase _enviarOrcamento;
        private readonly AprovarOrcamentoUseCase _aprovarOrcamento;
        private readonly RejeitarOrcamentoUseCase _rejeitarOrcamento;
        private readonly AtualizarStatusPorEmailUseCase _atualizarStatusPorEmail;

        public OrdensServicoController(
            IConfiguration config,
            AbrirOrdemServicoUseCase abrirOrdemServico,
            ListarOrdensServicoUseCase listarOrdensServico,
            ConsultarOrdemServicoUseCase consultarOrdemServico,
            AvancarStatusUseCase avancarStatus,
            EnviarOrcamentoUseCase enviarOrcamento,
            AprovarOrcamentoUseCase aprovarOrcamento,
            RejeitarOrcamentoUseCase rejeitarOrcamento,
            AtualizarStatusPorEmailUseCase atualizarStatusPorEmail)
        {
            _config = config;
            _abrirOrdemServico = abrirOrdemServico;
            _listarOrdensServico = listarOrdensServico;
            _consultarOrdemServico = consultarOrdemServico;
            _avancarStatus = avancarStatus;
            _enviarOrcamento = enviarOrcamento;
            _aprovarOrcamento = aprovarOrcamento;
            _rejeitarOrcamento = rejeitarOrcamento;
            _atualizarStatusPorEmail = atualizarStatusPorEmail;
        }

        // GET: api/OrdensServico
        [HttpGet]
        public async Task<IActionResult> GetOrdensServico()
        {
            var resultado = await _listarOrdensServico.ExecutarAsync();
            return Ok(resultado.Dados);
        }

        // GET: api/OrdensServico/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrdemServico(Guid id)
        {
            var resultado = await _consultarOrdemServico.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/OrdensServico
        [HttpPost]
        public async Task<IActionResult> PostOrdemServico(AbrirOrdemServicoInput input)
        {
            var resultado = await _abrirOrdemServico.ExecutarAsync(input);
            return CreatedAtAction(nameof(GetOrdemServico), new { id = resultado.Dados!.Id }, resultado.Dados);
        }

        // PUT: api/OrdensServico/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id)
        {
            var resultado = await _avancarStatus.ExecutarAsync(id);
            if (!resultado.Sucesso) return TraduzirResultado(resultado);
            return NoContent();
        }

        // POST: api/OrdensServico/{id}/enviar-orcamento
        [HttpPost("{id}/enviar-orcamento")]
        public async Task<IActionResult> EnviarOrcamento(Guid id)
        {
            var resultado = await _enviarOrcamento.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/OrdensServico/{id}/aprovar-orcamento
        [HttpPost("{id}/aprovar-orcamento")]
        public async Task<IActionResult> AprovarOrcamento(Guid id)
        {
            var resultado = await _aprovarOrcamento.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/OrdensServico/{id}/rejeitar-orcamento
        [HttpPost("{id}/rejeitar-orcamento")]
        public async Task<IActionResult> RejeitarOrcamento(Guid id)
        {
            var resultado = await _rejeitarOrcamento.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/OrdensServico/webhook/email-status
        // Webhook de atualização de status via e-mail. A validação do segredo
        // compartilhado (X-Webhook-Secret) é uma preocupação de transporte/segurança
        // HTTP, então fica aqui no Controller — não na camada de Application.
        [HttpPost("webhook/email-status")]
        [AllowAnonymous]
        public async Task<IActionResult> AtualizarStatusPorEmail(
            [FromBody] AtualizacaoStatusEmailInput input,
            [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret)
        {
            var secretEsperado = _config["EmailWebhook:Secret"];
            if (string.IsNullOrEmpty(secretEsperado) || webhookSecret != secretEsperado)
                return Unauthorized("Webhook secret inválido.");

            var resultado = await _atualizarStatusPorEmail.ExecutarAsync(input);
            return TraduzirResultado(resultado);
        }

        // Traduz o resultado de um Use Case (independente de HTTP) para a resposta HTTP correta.
        private IActionResult TraduzirResultado<T>(OperationResult<T> resultado)
        {
            if (resultado.Sucesso)
                return Ok(resultado.Dados);

            return resultado.Erro switch
            {
                TipoErro.NaoEncontrado => NotFound(resultado.Mensagem),
                TipoErro.ValidacaoFalhou => BadRequest(resultado.Mensagem),
                TipoErro.OperacaoInvalida => BadRequest(resultado.Mensagem),
                TipoErro.NaoAutorizado => Unauthorized(resultado.Mensagem),
                _ => BadRequest(resultado.Mensagem)
            };
        }
    }
}
