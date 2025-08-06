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
        // Convert based on batch type
        switch (BatchType)
        {
            case BatchType.Grams:
                return CalculatedAmount;
            case BatchType.Gallons:
                // Convert grams to gallons (approximate density conversion)
                return CalculatedAmount / (Ingredient.Density * 3785.41); // 1 gallon = 3785.41 ml
            case BatchType.Ounces:
                // Convert grams to ounces
                return CalculatedAmount / 28.3495; // 1 ounce = 28.3495 grams
            case BatchType.Lbs:
                // Convert grams to pounds
                return CalculatedAmount / 453.592; // 1 pound = 453.592 grams
            default:
                return CalculatedAmount;
        }
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
