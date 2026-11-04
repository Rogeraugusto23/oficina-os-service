using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsService.Application.Common;
using OsService.Application.DTOs.Clientes;
using OsService.Application.UseCases.Clientes;

namespace OsService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly CriarClienteUseCase _criarCliente;
        private readonly ListarClientesUseCase _listarClientes;
        private readonly ConsultarClienteUseCase _consultarCliente;
        private readonly InativarClienteUseCase _inativarCliente;
        private readonly ReativarClienteUseCase _reativarCliente;

        public ClientesController(
            CriarClienteUseCase criarCliente,
            ListarClientesUseCase listarClientes,
            ConsultarClienteUseCase consultarCliente,
            InativarClienteUseCase inativarCliente,
            ReativarClienteUseCase reativarCliente)
        {
            _criarCliente = criarCliente;
            _listarClientes = listarClientes;
            _consultarCliente = consultarCliente;
            _inativarCliente = inativarCliente;
            _reativarCliente = reativarCliente;
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var resultado = await _listarClientes.ExecutarAsync();
            return Ok(resultado.Dados);
        }

        // GET: api/Clientes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var resultado = await _consultarCliente.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/Clientes
        [HttpPost]
        public async Task<IActionResult> Post(CriarClienteInput input)
        {
            var resultado = await _criarCliente.ExecutarAsync(input);
            if (!resultado.Sucesso) return TraduzirResultado(resultado);

            return CreatedAtAction(nameof(GetById), new { id = resultado.Dados!.Id }, resultado.Dados);
        }

        // POST: api/Clientes/{id}/inativar
        [HttpPost("{id}/inativar")]
        public async Task<IActionResult> Inativar(Guid id)
        {
            var resultado = await _inativarCliente.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

        // POST: api/Clientes/{id}/reativar
        [HttpPost("{id}/reativar")]
        public async Task<IActionResult> Reativar(Guid id)
        {
            var resultado = await _reativarCliente.ExecutarAsync(id);
            return TraduzirResultado(resultado);
        }

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
