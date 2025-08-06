namespace StainSelector.Models;

public class Stain
{
    public string Color { get; set; } = string.Empty;
    public string FormulaName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Comments { get; set; } = string.Empty;
}
