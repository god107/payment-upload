# CSV Payment Generator

Fast development tool for generating test payment CSV files with realistic data.

## Usage

### Basic Generation

```bash
# Generate 1,000 rows (default)
dotnet run --project src/UploadPayments.CsvGenerator

# Generate specific number of rows
dotnet run --project src/UploadPayments.CsvGenerator -- --rows 10000

# Specify output file
dotnet run --project src/UploadPayments.CsvGenerator -- --rows 50000 --output large-test.csv
```

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--rows` | `-r` | Number of payment rows to generate | 1000 |
| `--output` | `-o` | Output CSV file path | test-payments.csv |
| `--seed` | `-s` | Random seed for reproducible generation | Random |
| `--batch-size` | `-b` | Batch size for parallel processing | 5000 |

### Examples

```bash
# Generate 50,000 rows
dotnet run --project src/UploadPayments.CsvGenerator -- -r 50000 -o large-test.csv

# Generate reproducible data with seed
dotnet run --project src/UploadPayments.CsvGenerator -- -r 5000 -s 12345

# View all options
dotnet run --project src/UploadPayments.CsvGenerator -- --help
```

## Performance

Generates **~38,000+ records/second** using async I/O and batched processing.

## Generated CSV Structure

30 columns including:
- BeneficiaryName, UniqueReference, AccountHolder
- Address fields (Address1, Address2, City, County, Postcode, Country)
- Banking details (AccountNumber, SwiftCode, IbanNumber, BranchCode)
- Payment details (Amount, Currency, ArriveBy, Reference)
- Invoice information (InvoiceNumber, InvoiceDate)

All data is generated using the Bogus library for realistic fake data.
