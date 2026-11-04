using Microsoft.EntityFrameworkCore;
using OsService.Domain.Entities;
using OsService.Domain.Interfaces;
using OsService.Infrastructure.Data;

namespace OsService.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly OsServiceDbContext _context;

    public ClienteRepository(OsServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(Guid id) =>
        await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cliente?> ObterPorDocumentoAsync(string documento)
    {
        var somenteDigitos = new string(documento.Where(char.IsDigit).ToArray());
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Documento == somenteDigitos);
    }

    public async Task<IEnumerable<Cliente>> ListarAsync() =>
        await _context.Clientes.ToListAsync();

    public async Task AdicionarAsync(Cliente cliente) =>
        await _context.Clientes.AddAsync(cliente);

    public async Task SalvarAlteracoesAsync() =>
        await _context.SaveChangesAsync();
}
