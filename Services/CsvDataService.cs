using StainSelector.Models;

namespace StainSelector.Services;

public class CsvDataService
{
    private List<Stain> _stains = new();
    private List<Ingredient> _ingredients = new();

    public List<Stain> Stains => _stains;
    public List<Ingredient> Ingredients => _ingredients;

    public async Task LoadDataAsync()
    {
        await LoadStainsAsync();
        await LoadIngredientsAsync();
    }

        private async Task LoadStainsAsync()
    {
        try
        {
            var lines = await File.ReadAllLinesAsync("exported_csv/Stains.csv");

            _stains = new List<Stain>();

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var values = ParseCsvLine(line);

                if (values.Length >= 6)
                {
                    var stain = new Stain
                    {
                        Color = values[0].Trim('"'),
                        FormulaName = values[1].Trim('"'),
                        Date = DateTime.TryParse(values[2].Trim('"'), out var date) ? date : DateTime.MinValue,
                        Time = values[3].Trim('"'),
                        Number = int.TryParse(values[4], out var number) ? number : 0,
                        Comments = values.Length > 5 ? values[5].Trim('"') : string.Empty
                    };
                    _stains.Add(stain);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading stains: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

        private async Task LoadIngredientsAsync()
    {
        try
        {
            var lines = await File.ReadAllLinesAsync("exported_csv/Ingred.csv");

            _ingredients = new List<Ingredient>();

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var values = ParseCsvLine(line);

                if (values.Length >= 6)
                {
                    var ingredient = new Ingredient
                    {
                        Rex = values[0].Trim('"'),
                        Label = values[1].Trim('"'),
                        Density = double.TryParse(values[2], out var density) ? density : 0,
                        Grams = double.TryParse(values[3], out var grams) ? grams : 0,
                        Cost = double.TryParse(values[4], out var cost) ? cost : 0,
                        FormulaNumber = values[5].Trim('"')
                    };
                    _ingredients.Add(ingredient);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading ingredients: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public List<Ingredient> GetIngredientsForStain(Stain stain)
    {
        return _ingredients.Where(i => i.FormulaNumber == stain.Number.ToString()).ToList();
    }

    public List<BatchCalculation> CalculateBatch(Stain stain, double batchSize, BatchType batchType)
    {
        var ingredients = GetIngredientsForStain(stain);
        var calculations = new List<BatchCalculation>();

        // Find the total grams in the original formula
        var totalOriginalGrams = ingredients.Sum(i => i.Grams);

        if (totalOriginalGrams <= 0) return calculations;

        // Calculate the scaling factor
        var scalingFactor = batchSize / totalOriginalGrams;

        foreach (var ingredient in ingredients)
        {
            var calculation = new BatchCalculation
            {
                Ingredient = ingredient,
                OriginalGrams = ingredient.Grams,
                CalculatedAmount = ingredient.Grams * scalingFactor,
                BatchType = batchType,
                BatchSize = batchSize
            };

            calculations.Add(calculation);
        }

        return calculations;
    }

        public List<Stain> SearchStains(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _stains;

        return _stains.Where(s =>
            s.Color.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.FormulaName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result.ToArray();
    }
}
