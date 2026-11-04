using System;
using System.Collections.Generic;
using System.Text;
using OsService.Domain.Enums;

namespace OsService.Domain.Entities;

public class OrdemServico
{
    public Guid Id { get; private set; }
    public string NumeroOS { get; private set; }
    public DateTime DataAbertura { get; private set; }
    public StatusOrdemServico Status { get; private set; }

    // Relacionamentos
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }

    private readonly List<ServicoItem> _servicos;
    public IReadOnlyCollection<ServicoItem> Servicos => _servicos.AsReadOnly();

    private readonly List<PecaItem> _pecas;
    public IReadOnlyCollection<PecaItem> Pecas => _pecas.AsReadOnly();

    public decimal ValorTotal { get; private set; }

    private OrdemServico()
    {
        _servicos = new List<ServicoItem>();
        _pecas = new List<PecaItem>();
    }

    // Construtor para nova OS
    public OrdemServico(Guid clienteId, Guid veiculoId)
    {
        _servicos = new List<ServicoItem>(); 
        _pecas = new List<PecaItem>();
        Id = Guid.NewGuid();
        NumeroOS = DateTime.Now.ToString("yyyyMMddHHmm");
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOrdemServico.Recebida;
        DataAbertura = DateTime.UtcNow;
    }

    public void AdicionarServico(string descricao, decimal preco)
    {
        _servicos.Add(new ServicoItem(descricao, preco));
        CalcularOrcamento();
    }

    public void AdicionarPeca(string nome, int quantidade, decimal precoUnitario)
    {
        _pecas.Add(new PecaItem(nome, quantidade, precoUnitario));
        CalcularOrcamento();
    }

    public void CalcularOrcamento()
    {
        decimal totalServicos = Servicos?.Sum(s => s.Preco) ?? 0;
        decimal totalPecas = Pecas?.Sum(p => p.Quantidade * p.PrecoUnitario) ?? 0;
        ValorTotal = totalServicos + totalPecas;
    }

    public void CancelarOS()
    {
        if (Status == StatusOrdemServico.Entregue || Status == StatusOrdemServico.Cancelada)
            throw new InvalidOperationException($"Não é possível cancelar uma OS com status {Status}.");

        Status = StatusOrdemServico.Cancelada;
    }

    public void AvancarStatus()
    {
        if (Status == StatusOrdemServico.Cancelada)
            throw new InvalidOperationException("Não é possível avançar uma OS cancelada.");

        if (Status < StatusOrdemServico.Entregue)
            Status++;
    }

    // Usado pelo webhook de atualização de status via e-mail (Fase 2):
    // permite setar o status recebido no e-mail diretamente, com as mesmas
    // travas de negócio de uma OS que já foi finalizada/cancelada.
    public void DefinirStatus(StatusOrdemServico novoStatus)
    {
        if (Status == StatusOrdemServico.Cancelada)
            throw new InvalidOperationException("Não é possível alterar o status de uma OS cancelada.");

        if (Status == StatusOrdemServico.Entregue)
            throw new InvalidOperationException("Não é possível alterar o status de uma OS já entregue.");

        Status = novoStatus;
    }
}