# MDB to CSV Converter (.NET)

This .NET application converts Microsoft Access database (.mdb) files to CSV format, equivalent to the original Python script.

## Prerequisites

1. **.NET 6.0 SDK** - Download and install from [Microsoft's website](https://dotnet.microsoft.com/download)
2. **Microsoft Access Database Engine** - Required for reading .mdb files
   - Download from Microsoft's website or install via Office installation

## Building and Running

### Build the application:
```bash
dotnet build
```

### Run the application:
```bash
dotnet run
```

## Configuration

Edit the file paths in `MdbToCsvConverter.cs`:

```csharp
string mdbFilePath = @"C:\Users\JuicyJerry\Dev.local\StainWare\StainFormulas.mdb";
string outputDirectory = @"C:\Users\JuicyJerry\Dev.local\StainWare\exported_csv";
```

## Features

- Exports all tables from the MDB file to individual CSV files
- Proper CSV escaping for fields containing commas, quotes, or newlines
- UTF-8 encoding for output files
- Console output showing progress and any errors
- Creates output directory if it doesn't exist

## Troubleshooting

### Common Issues:

1. **"Provider not found" error**: Install Microsoft Access Database Engine
2. **File not found**: Ensure the MDB file path is correct
3. **Permission denied**: Run as administrator or check file permissions

## Comparison with Python Version

The .NET version provides the same functionality as the original Python script:
- Connects to Microsoft Access databases
- Extracts all tables
- Exports each table to a separate CSV file
- Handles errors gracefully
- Provides console feedback

## Advantages of .NET Version

- Better performance for large datasets
- Native Windows integration
- No external dependencies (except Access Database Engine)
- Strongly typed with better error handling
- Can be easily compiled to a standalone executable
