using StainSelector.Models;
using StainSelector.Services;
using System.Text;
using System.IO;
using System.Linq;

namespace StainSelector;

public partial class MainForm : Form
{
    private readonly CsvDataService _dataService;
    private List<Stain> _allStains = new();
    private List<Stain> _filteredStains = new();
    private Stain? _selectedStain;
    private List<BatchCalculation> _currentBatchCalculations = new();

    // UI Controls
    private ListView stainListView = null!;
    private ListView ingredientListView = null!;
    private TextBox searchTextBox = null!;
    private TextBox batchAmountTextBox = null!;
    private ComboBox batchTypeComboBox = null!;
    private Button calculateButton = null!;
    private Label statusLabel = null!;


    public MainForm()
    {
        InitializeComponent();
        _dataService = new CsvDataService();
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            await _dataService.LoadDataAsync();
            _allStains = _dataService.Stains;
            _filteredStains = _allStains;
            PopulateStainList();
            UpdateStatusLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void InitializeComponent()
    {
        this.Text = "Stain Selector";
        this.Size = new Size(1200, 700); // Wider window with appropriate height
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 240, 240);

        // Create main container with status bar at bottom
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        // Configure rows - content area and status bar
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content area
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Status bar height

        // Main split container for left/right panels
        var mainSplitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 45, // With FixedPanel.None, this is a percentage of total width (not pixels)
            FixedPanel = FixedPanel.None, // Allow both panels to resize proportionally
            Margin = new Padding(0)
        };

        // Set panel1MinSize to ensure the left panel doesn't get too small
        // mainSplitContainer.Panel1MinSize = 200;

        // --- Left Panel ---
        var leftPanel = mainSplitContainer.Panel1;
        leftPanel.BackColor = Color.White;
        leftPanel.Padding = new Padding(5);

        CreateLeftPanelControls(leftPanel);

        // --- Right Panel ---
        var rightPanel = mainSplitContainer.Panel2;
        rightPanel.BackColor = Color.White;
        rightPanel.Padding = new Padding(5);

        CreateRightPanelControls(rightPanel);

        // --- Status Bar ---
        var statusPanel = CreateStatusPanel();

        // Add components to main container
        mainContainer.Controls.Add(mainSplitContainer, 0, 0);
        mainContainer.Controls.Add(statusPanel, 0, 1);

        this.Controls.Add(mainContainer);
    }

    private void CreateLeftPanelControls(Panel panel)
    {
        // Create a TableLayoutPanel for vertical stacking
        var tableLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        // Configure row styles
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Search panel height
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Label height
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ListView fills remaining space

        // Search section using TableLayoutPanel for better alignment
        var searchPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(0),
            RowCount = 2,
            ColumnCount = 1
        };

        // Configure rows
        searchPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Label row
        searchPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // TextBox row

        // Search label
        var searchLabel = new Label {
            Text = "Search Stains:",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        // Search textbox with proper margins
        searchTextBox = new TextBox {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 3, 0, 3)
        };

        searchTextBox.TextChanged += (s, e) => {
            _filteredStains = _dataService.SearchStains(searchTextBox.Text);
            PopulateStainList();
        };

        // Add controls to the panel
        searchPanel.Controls.Add(searchLabel, 0, 0);
        searchPanel.Controls.Add(searchTextBox, 0, 1);

        // Stains list header
        var stainsLabel = new Label {
            Text = "Available Stains",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(10, 0, 0, 0)
        };

        // Stains list
        stainListView = new ListView {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        stainListView.Columns.Add("Color", 210);
        stainListView.Columns.Add("Formula", 100);
        // Number column is now hidden (width 0)
        var numberColumn = stainListView.Columns.Add("Number", 20);
        // numberColumn.Width = 0;

        stainListView.SelectedIndexChanged += (s, e) => {
            if (stainListView.SelectedItems.Count > 0)
            {
                var selectedStain = (Stain?)stainListView.SelectedItems[0].Tag;
                if (selectedStain != null)
                {
                    _selectedStain = selectedStain;
                    DisplayIngredients(selectedStain);
                    // No need to call UpdateBatchCalculations() here as DisplayIngredients now handles calculations
                }
            }
        };

        // Add controls to table layout in order
        tableLayout.Controls.Add(searchPanel, 0, 0);
        tableLayout.Controls.Add(stainsLabel, 0, 1);
        tableLayout.Controls.Add(stainListView, 0, 2);

        // Add table layout to panel
        panel.Controls.Add(tableLayout);
    }

    private void CreateRightPanelControls(Panel panel)
    {
        // Create a TableLayoutPanel for vertical stacking
        var tableLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        // Configure row styles
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Batch controls height
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Label height
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ListView fills remaining space

        // Batch controls panel - using TableLayoutPanel for better alignment
        var batchControlsPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 248, 255),
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(0),
            RowCount = 2,
            ColumnCount = 7
        };

        // Configure rows and columns
        batchControlsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Header row
        batchControlsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Controls row

        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // "Batch Amount:" label
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // Amount textbox
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // "Batch Type:" label
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // Type combobox
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // filler
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Calculate button
        batchControlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // Export button

        // Header spanning all columns
        var batchLabel = new Label {
            Text = "Batch Calculator",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var amountLabel = new Label {
            Text = "Batch Amount:",
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        batchAmountTextBox = new TextBox {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            Text = "1",
            Margin = new Padding(0, 5, 5, 5)
        };

        var typeLabel = new Label {
            Text = "Batch Type:",
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        batchTypeComboBox = new ComboBox {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 5, 5, 5)
        };

        batchTypeComboBox.Items.AddRange(new object[] { "Grams", "Gallons", "Ounces", "Lbs" });
        batchTypeComboBox.SelectedIndex = 1;

        calculateButton = new Button {
            Text = "Calculate Batch",
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Margin = new Padding(5, 5, 0, 5),
            Width = 100
        };

        calculateButton.Click += (s, e) => UpdateBatchCalculations();

        // Export CSV button
        var exportButton = new Button {
            Text = "Export CSV",
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Margin = new Padding(5, 5, 0, 5),
            Width = 100
        };
        exportButton.Click += (s, e) => ExportIngredientsToCsv();

        // Group action buttons in a panel so they share the same cell
        var actionsPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 5, 0, 5)
        };
        actionsPanel.Controls.Add(calculateButton);
        actionsPanel.Controls.Add(exportButton);

        // Add header spanning all columns
        batchControlsPanel.Controls.Add(batchLabel, 0, 0);
        batchControlsPanel.SetColumnSpan(batchLabel, 7);

        // Add controls to the second row
        batchControlsPanel.Controls.Add(amountLabel, 0, 1);
        batchControlsPanel.Controls.Add(batchAmountTextBox, 1, 1);
        batchControlsPanel.Controls.Add(typeLabel, 2, 1);
        batchControlsPanel.Controls.Add(batchTypeComboBox, 3, 1);
        // column 4 is filler to push buttons to the right
        batchControlsPanel.Controls.Add(calculateButton, 5, 1);
        batchControlsPanel.Controls.Add(exportButton, 6, 1);

        // Ingredients header
        var ingredientsLabel = new Label {
            Text = "Ingredients & Batch Calculations",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(10, 0, 0, 0)
        };

        // Ingredients list
        ingredientListView = new ListView {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9),
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };

        ingredientListView.Columns.Add("Color", 120);
        ingredientListView.Columns.Add("REX/cost", 100);
        ingredientListView.Columns.Add("Density", 80);
        ingredientListView.Columns.Add("Grams", 80);
        ingredientListView.Columns.Add("Gallons", 80);
        ingredientListView.Columns.Add("FL. Ounces", 80);
        ingredientListView.Columns.Add("Lbs.", 80);
        ingredientListView.Columns.Add("Add", 80);

        // Add controls to table layout in order
        tableLayout.Controls.Add(batchControlsPanel, 0, 0);
        tableLayout.Controls.Add(ingredientsLabel, 0, 1);
        tableLayout.Controls.Add(ingredientListView, 0, 2);

        // Add table layout to panel
        panel.Controls.Add(tableLayout);
    }

    private Panel CreateStatusPanel()
    {
        var statusPanel = new Panel {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(0)
        };

        statusLabel = new Label {
            Text = "Ready",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            AutoSize = true,
            Location = new Point(10, 5)
        };

        statusPanel.Controls.Add(statusLabel);
        return statusPanel;
    }

    private void PopulateStainList()
    {
        stainListView.Items.Clear();
        foreach (var stain in _filteredStains)
        {
            var item = new ListViewItem(stain.Color);
            item.SubItems.Add(stain.FormulaName);
            // Still add the Number data to the hidden column
            item.SubItems.Add(stain.Number.ToString());
            item.Tag = stain;
            stainListView.Items.Add(item);
        }
        UpdateStatusLabel();
    }

        private void DisplayIngredients(Stain stain)
    {
        ingredientListView.Items.Clear();
        var ingredients = _dataService.GetIngredientsForStain(stain);

        // Get batch amount and type for calculations
        double batchAmount = 1.0; // Default to 1
        if (double.TryParse(batchAmountTextBox.Text, out var amount))
        {
            batchAmount = amount;
        }
        var batchType = (BatchType)batchTypeComboBox.SelectedIndex;

        // Calculate batch for all ingredients
        var batchCalculations = _dataService.CalculateBatch(stain, batchAmount, batchType);
        _currentBatchCalculations = batchCalculations;

        statusLabel.Text = $"Loading {ingredients.Count} ingredients for stain {stain.Color} (Formula #{stain.Number})";

        // Track totals
        double totalGrams = 0;
        double totalGallons = 0;
        double totalOunces = 0;
        double totalLbs = 0;
        double totalAdd = 0;

        foreach (var calculation in batchCalculations)
        {
            var ingredient = calculation.Ingredient;

            // Get original values
            double grams = ingredient.Grams;

            // Get calculated amount for the current batch
            double calculatedAmount = calculation.CalculatedAmount;

            // Create batch calculation for each unit type to get proper conversions
            var gramsCalc = new BatchCalculation {
                Ingredient = ingredient,
                OriginalGrams = grams,
                BatchType = BatchType.Grams,
                BatchSize = batchAmount,
                CalculatedAmount = calculatedAmount
            };

            var gallonsCalc = new BatchCalculation {
                Ingredient = ingredient,
                OriginalGrams = grams,
                BatchType = BatchType.Gallons,
                BatchSize = batchAmount,
                CalculatedAmount = calculatedAmount
            };

            var ouncesCalc = new BatchCalculation {
                Ingredient = ingredient,
                OriginalGrams = grams,
                BatchType = BatchType.Ounces,
                BatchSize = batchAmount,
                CalculatedAmount = calculatedAmount
            };

            var lbsCalc = new BatchCalculation {
                Ingredient = ingredient,
                OriginalGrams = grams,
                BatchType = BatchType.Lbs,
                BatchSize = batchAmount,
                CalculatedAmount = calculatedAmount
            };

            // Calculate values using the proper method
            double gallons = gallonsCalc.GetCalculatedAmount();
            double flOunces = ouncesCalc.GetCalculatedAmount();
            double lbs = lbsCalc.GetCalculatedAmount();

            // Get the "Add" value based on the current batch calculation
            double addValue = calculation.GetCalculatedAmount();

            // Create list item with the ingredient color
            var item = new ListViewItem(ingredient.Label);

            // Add the remaining columns
            item.SubItems.Add(ingredient.Rex);
            item.SubItems.Add(ingredient.Density.ToString("F3")); // Add density column
            item.SubItems.Add(calculatedAmount.ToString("F1")); // Display calculated grams instead of original
            item.SubItems.Add(gallons.ToString("F3"));
            item.SubItems.Add(flOunces.ToString("F3"));
            item.SubItems.Add(lbs.ToString("F3"));
            item.SubItems.Add(addValue.ToString("F3")); // Add column shows calculated amount

            item.Tag = ingredient;
            ingredientListView.Items.Add(item);


            // Update totals
            totalGrams += calculatedAmount; // Use calculated amount for total
            totalGallons += gallons;
            totalOunces += flOunces;
            totalLbs += lbs;
            totalAdd += addValue;
        }

        // Add the TOTAL row
        var totalItem = new ListViewItem("Total");
        totalItem.Font = new Font(totalItem.Font, FontStyle.Bold);

        // Add empty REX/cost cell
        totalItem.SubItems.Add("");

        // Add empty Density cell
        totalItem.SubItems.Add("");

        // Add the total values
        totalItem.SubItems.Add(totalGrams.ToString("F1"));
        totalItem.SubItems.Add(totalGallons.ToString("F3"));
        totalItem.SubItems.Add(totalOunces.ToString("F3"));
        totalItem.SubItems.Add(totalLbs.ToString("F3"));
        totalItem.SubItems.Add(totalAdd.ToString("F3")); // Add total for Add column

        ingredientListView.Items.Add(totalItem);
    }

        private void UpdateBatchCalculations()
    {
        if (_selectedStain == null) return;
        if (!double.TryParse(batchAmountTextBox.Text, out var batchAmount))
        {
            MessageBox.Show("Please enter a valid batch amount.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Simply re-display ingredients with the new batch amount
        // DisplayIngredients now handles all the calculations
        DisplayIngredients(_selectedStain);

        UpdateStatusLabel();
    }

    private void ExportIngredientsToCsv()
    {
        if (ingredientListView.Items.Count == 0)
        {
            MessageBox.Show("There are no ingredients to export.", "Nothing to Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string defaultName = "ingredients.csv";
        if (_selectedStain != null)
        {
            var baseName = $"{_selectedStain.Color}_{_selectedStain.Number}_ingredients";
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(ch, '_');
            }
            defaultName = baseName + ".csv";
        }

        using var sfd = new SaveFileDialog
        {
            Title = "Export Ingredients to CSV",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = defaultName,
            AddExtension = true,
            DefaultExt = "csv",
            OverwritePrompt = true,
            RestoreDirectory = true
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            using var writer = new StreamWriter(sfd.FileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Write header
            var headerCells = new List<string>();
            foreach (ColumnHeader col in ingredientListView.Columns)
            {
                headerCells.Add(EscapeCsv(col.Text));
            }
            writer.WriteLine(string.Join(',', headerCells));

            // Write rows
            foreach (ListViewItem item in ingredientListView.Items)
            {
                var cells = new List<string> { EscapeCsv(item.Text) };
                // Include all subitems in the same order as displayed
                for (int i = 1; i < item.SubItems.Count; i++)
                {
                    cells.Add(EscapeCsv(item.SubItems[i].Text));
                }
                writer.WriteLine(string.Join(',', cells));
            }

            statusLabel.Text = $"Exported ingredients to {sfd.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (mustQuote)
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
        return value;
    }

    private void UpdateStatusLabel()
    {
        var totalStains = _filteredStains.Count;
        var totalIngredients = _selectedStain != null ? _dataService.GetIngredientsForStain(_selectedStain).Count : 0;
        statusLabel.Text = $"Showing {totalStains} stains. Selected stain has {totalIngredients} ingredients.";
    }
}
