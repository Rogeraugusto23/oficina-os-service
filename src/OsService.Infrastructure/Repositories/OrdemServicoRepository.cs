using Microsoft.EntityFrameworkCore;
using OsService.Domain.Entities;
using OsService.Domain.Interfaces;
using OsService.Infrastructure.Data;

namespace OsService.Infrastructure.Repositories;

public class OrdemServicoRepository : IOrdemServicoRepository
{
    private readonly OsServiceDbContext _context;

    public OrdemServicoRepository(OsServiceDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemServico?> ObterPorIdAsync(Guid id)
    {
        return await _context.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrdemServico?> ObterPorNumeroOSAsync(string numeroOS)
    {
        return await _context.OrdensServico
            .FirstOrDefaultAsync(o => o.NumeroOS == numeroOS);
    }

    public async Task<IEnumerable<OrdemServico>> ListarAsync()
    {
        return await _context.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .ToListAsync();
    }

    public async Task AdicionarAsync(OrdemServico ordemServico)
    {
        await _context.OrdensServico.AddAsync(ordemServico);
    }

    public async Task SalvarAlteracoesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
