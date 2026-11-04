public class PecaItem
{
    public int Id { get; set; }
    public string Nome { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }

    private PecaItem() { } // ✅ construtor para o EF Core

    public PecaItem(string nome, int quantidade, decimal precoUnitario)
    {
        Nome = nome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}