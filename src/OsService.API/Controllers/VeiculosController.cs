using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OsService.Domain.Entities;
using OsService.Infrastructure.Data;

namespace OsService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VeiculosController : ControllerBase
{
    private readonly OsServiceDbContext _context;

    public VeiculosController(OsServiceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Veiculo>>> GetVeiculos()
    {
        return await _context.Veiculos.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Veiculo>> GetVeiculo(Guid id)
    {
        var veiculo = await _context.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();
        return veiculo;
    }

    [HttpGet("cliente/{clienteId}")]
    public async Task<ActionResult<IEnumerable<Veiculo>>> GetVeiculosPorCliente(Guid clienteId)
    {
        return await _context.Veiculos
            .Where(v => v.ClienteId == clienteId)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Veiculo>> PostVeiculo(VeiculoInputModel input)
    {
        try
        {
            var veiculo = new Veiculo(input.Placa, input.Marca, input.Modelo, input.Ano, input.ClienteId);
            _context.Veiculos.Add(veiculo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVeiculo), new { id = veiculo.Id }, veiculo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException)
        {
            // ✅ Trata erro de placa duplicada no banco
            return Conflict("Já existe um veículo cadastrado com esta placa.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutVeiculo(Guid id, VeiculoInputModel input)
    {
        var veiculo = await _context.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        try
        {
            veiculo.Atualizar(input.Marca, input.Modelo, input.Ano);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVeiculo(Guid id)
    {
        var veiculo = await _context.Veiculos.FindAsync(id);
        if (veiculo == null) return NotFound();

        _context.Veiculos.Remove(veiculo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public class VeiculoInputModel
    {
        public string Placa { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Ano { get; set; }
        public Guid ClienteId { get; set; }
    }
}