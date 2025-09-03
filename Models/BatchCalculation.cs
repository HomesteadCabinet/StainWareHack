namespace StainSelector.Models;

public enum BatchType
{
    Grams,
    Gallons,
    Ounces,
    Lbs
}

public class BatchCalculation
{
    public Ingredient Ingredient { get; set; } = null!;
    public double OriginalGrams { get; set; }
    public double CalculatedAmount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public BatchType BatchType { get; set; }
    public double BatchSize { get; set; }

    public double GetCalculatedAmount()
    {
        // First, adjust for density if needed (for volume measurements like gallons/ounces)
        double density = Ingredient.Density > 0 ? Ingredient.Density : 1.0;

        // Convert based on batch type
        return BatchType switch
        {
            BatchType.Grams => CalculatedAmount,
            BatchType.Gallons => CalculatedAmount / (density * 3785.41),// Convert grams to gallons using density and standard ml per gallon
                                                                        // gallons = grams / (density * 3785.41)
            BatchType.Ounces => CalculatedAmount / (density * 29.5735),// Convert grams to fluid ounces using density and standard ml per fl oz
                                                                       // fl oz = grams / (density * 29.5735)
            BatchType.Lbs => CalculatedAmount / 456.0,// Convert grams to pounds using legacy factor retained for parity
            _ => CalculatedAmount,
        };
    }

    public string GetUnitLabel()
    {
        return BatchType switch
        {
            BatchType.Grams => "g",
            BatchType.Gallons => "gal",
            BatchType.Ounces => "oz",
            BatchType.Lbs => "lbs",
            _ => "g"
        };
    }
}
