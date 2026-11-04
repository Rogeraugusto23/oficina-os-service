using OsService.Application.Common;
using OsService.Application.DTOs.OrdensServico;
using OsService.Domain.Enums;
using OsService.Domain.Interfaces;

namespace OsService.Application.UseCases.OrdensServico;

public class AtualizarStatusPorEmailUseCase
{
    private readonly IOrdemServicoRepository _repository;

    public AtualizarStatusPorEmailUseCase(IOrdemServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<StatusAtualizadoPorEmailOutput>> ExecutarAsync(AtualizacaoStatusEmailInput input)
    {
        var ordem = await _repository.ObterPorNumeroOSAsync(input.NumeroOS);
        if (ordem == null)
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(
                TipoErro.NaoEncontrado, $"OS {input.NumeroOS} não encontrada.");

        if (!Enum.TryParse<StatusOrdemServico>(input.NovoStatus, true, out var novoStatus))
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(
                TipoErro.ValidacaoFalhou, $"Status '{input.NovoStatus}' inválido.");

        try
        {
            ordem.DefinirStatus(novoStatus);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<StatusAtualizadoPorEmailOutput>.Falha(TipoErro.OperacaoInvalida, ex.Message);
        }

        await _repository.SalvarAlteracoesAsync();

        return OperationResult<StatusAtualizadoPorEmailOutput>.Ok(new StatusAtualizadoPorEmailOutput
        {
            Mensagem = "Status atualizado via e-mail com sucesso.",
            NumeroOS = ordem.NumeroOS,
            StatusAnterior = input.NovoStatus,
            StatusAtual = ordem.Status.ToString(),
            OrigemEmail = input.RemetenteEmail
        });
    }
}
