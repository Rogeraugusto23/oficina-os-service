public class ServicoItem
{
    public int Id { get; set; }
    public string Descricao { get; private set; }
    public decimal Preco { get; private set; }

    private ServicoItem() { } // ✅ construtor para o EF Core

    public ServicoItem(string descricao, decimal preco)
    {
        Descricao = descricao;
        Preco = preco;
    }
}