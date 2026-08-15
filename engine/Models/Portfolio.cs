namespace ComplianceEngine.Models;

public class Portfolio
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
}