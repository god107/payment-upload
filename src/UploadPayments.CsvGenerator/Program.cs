using System.CommandLine;
using System.Globalization;
using Bogus;
using CsvHelper;
using CsvHelper.Configuration;

namespace UploadPayments.CsvGenerator;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rowsOption = new Option<int>(
            name: "--rows",
            description: "Number of payment rows to generate",
            getDefaultValue: () => 1000);
        rowsOption.AddAlias("-r");

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output CSV file path",
            getDefaultValue: () => "test-payments.csv");
        outputOption.AddAlias("-o");

        var seedOption = new Option<int?>(
            name: "--seed",
            description: "Random seed for reproducible generation (optional)");
        seedOption.AddAlias("-s");

        var batchSizeOption = new Option<int>(
            name: "--batch-size",
            description: "Batch size for parallel processing",
            getDefaultValue: () => 5000);
        batchSizeOption.AddAlias("-b");

        var rootCommand = new RootCommand("Fast CSV payment data generator for development/testing")
        {
            rowsOption,
            outputOption,
            seedOption,
            batchSizeOption
        };

        rootCommand.SetHandler(async (rows, output, seed, batchSize) =>
        {
            await GenerateCsvAsync(rows, output, seed, batchSize);
        }, rowsOption, outputOption, seedOption, batchSizeOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task GenerateCsvAsync(int rows, string outputPath, int? seed, int batchSize)
    {
        var startTime = DateTime.Now;
        Console.WriteLine($"Generating {rows:N0} payment records...");
        Console.WriteLine($"Output: {outputPath}");
        if (seed.HasValue)
        {
            Console.WriteLine($"Using seed: {seed.Value}");
        }
        Console.WriteLine();

        // Configure CSV writer
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        await using var writer = new StreamWriter(outputPath);
        await using var csv = new CsvWriter(writer, config);

        // Write headers
        csv.WriteField("BeneficiaryName");
        csv.WriteField("UniqueReference");
        csv.WriteField("AccountHolder");
        csv.WriteField("Address1");
        csv.WriteField("Address2");
        csv.WriteField("City");
        csv.WriteField("County");
        csv.WriteField("Postcode");
        csv.WriteField("Country");
        csv.WriteField("PaymentCurrency");
        csv.WriteField("BankName");
        csv.WriteField("BankCountry");
        csv.WriteField("AccountNumber");
        csv.WriteField("SwiftCode");
        csv.WriteField("IbanNumber");
        csv.WriteField("OtherRouting");
        csv.WriteField("BranchCode");
        csv.WriteField("AccountType");
        csv.WriteField("Telephone");
        csv.WriteField("Email");
        csv.WriteField("SettlementCurrency");
        csv.WriteField("Reference");
        csv.WriteField("Amount");
        csv.WriteField("ArriveBy");
        csv.WriteField("TransactionCode");
        csv.WriteField("OwningSide");
        csv.WriteField("Method");
        csv.WriteField("CountryCode");
        csv.WriteField("InvoiceNumber");
        csv.WriteField("InvoiceDate");
        await csv.NextRecordAsync();

        // Generate data in batches for better performance
        var recordsWritten = 0;
        var faker = CreateFaker(seed);

        for (int batchStart = 0; batchStart < rows; batchStart += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, rows - batchStart);
            var batch = faker.Generate(currentBatchSize);

            foreach (var record in batch)
            {
                csv.WriteField(record.BeneficiaryName);
                csv.WriteField(record.UniqueReference);
                csv.WriteField(record.AccountHolder);
                csv.WriteField(record.Address1);
                csv.WriteField(record.Address2);
                csv.WriteField(record.City);
                csv.WriteField(record.County);
                csv.WriteField(record.Postcode);
                csv.WriteField(record.Country);
                csv.WriteField(record.PaymentCurrency);
                csv.WriteField(record.BankName);
                csv.WriteField(record.BankCountry);
                csv.WriteField(record.AccountNumber);
                csv.WriteField(record.SwiftCode);
                csv.WriteField(record.IbanNumber);
                csv.WriteField(record.OtherRouting);
                csv.WriteField(record.BranchCode);
                csv.WriteField(record.AccountType);
                csv.WriteField(record.Telephone);
                csv.WriteField(record.Email);
                csv.WriteField(record.SettlementCurrency);
                csv.WriteField(record.Reference);
                csv.WriteField(record.Amount);
                csv.WriteField(record.ArriveBy);
                csv.WriteField(record.TransactionCode);
                csv.WriteField(record.OwningSide);
                csv.WriteField(record.Method);
                csv.WriteField(record.CountryCode);
                csv.WriteField(record.InvoiceNumber);
                csv.WriteField(record.InvoiceDate);
                await csv.NextRecordAsync();

                recordsWritten++;
            }

            Console.Write($"\rProgress: {recordsWritten:N0} / {rows:N0} ({(double)recordsWritten / rows * 100:F1}%)");
        }

        await csv.FlushAsync();
        
        var elapsed = DateTime.Now - startTime;
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"✓ Successfully generated {recordsWritten:N0} records in {elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"  ({recordsWritten / elapsed.TotalSeconds:N0} records/second)");
    }

    private static Faker<PaymentRecord> CreateFaker(int? seed)
    {
        var randomSeed = seed ?? Random.Shared.Next();
        Randomizer.Seed = new Random(randomSeed);

        var countries = new[] { "GB", "DE", "FR", "US", "NL", "BE", "IT", "ES" };
        var currencies = new[] { "GBP", "EUR", "USD" };
        var arriveByOptions = new[] { "ASAP", "STANDARD", "NEXT_DAY" };
        var accountTypes = new[] { "CHECKING", "SAVINGS", "BUSINESS" };
        var methods = new[] { "WIRE", "ACH", "SEPA", "SWIFT" };

        return new Faker<PaymentRecord>()
            .RuleFor(p => p.BeneficiaryName, f => f.Name.FullName())
            .RuleFor(p => p.UniqueReference, f => $"REF{f.Random.Number(100000, 999999)}")
            .RuleFor(p => p.AccountHolder, f => f.Name.FullName())
            .RuleFor(p => p.Address1, f => f.Address.StreetAddress())
            .RuleFor(p => p.Address2, f => f.Random.Bool(0.3f) ? f.Address.SecondaryAddress() : "")
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.County, f => f.Address.County())
            .RuleFor(p => p.Postcode, f => f.Address.ZipCode())
            .RuleFor(p => p.Country, f => f.PickRandom(countries))
            .RuleFor(p => p.PaymentCurrency, f => f.PickRandom(currencies))
            .RuleFor(p => p.BankName, f => f.Company.CompanyName() + " Bank")
            .RuleFor(p => p.BankCountry, f => f.PickRandom(countries))
            .RuleFor(p => p.AccountNumber, f => f.Finance.Account(10))
            .RuleFor(p => p.SwiftCode, f => f.Finance.Bic())
            .RuleFor(p => p.IbanNumber, f => GenerateIban(f))
            .RuleFor(p => p.OtherRouting, f => f.Random.Bool(0.2f) ? f.Random.AlphaNumeric(9) : "")
            .RuleFor(p => p.BranchCode, f => f.Random.Number(100000, 999999).ToString())
            .RuleFor(p => p.AccountType, f => f.PickRandom(accountTypes))
            .RuleFor(p => p.Telephone, f => f.Phone.PhoneNumber("+## ### ### ####"))
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.SettlementCurrency, "USD") // Always USD per original script
            .RuleFor(p => p.Reference, f => $"INV-{f.Random.Number(10000, 99999)}")
            .RuleFor(p => p.Amount, f => f.Finance.Amount(1, 10000, 2).ToString("F2"))
            .RuleFor(p => p.ArriveBy, f => f.PickRandom(arriveByOptions))
            .RuleFor(p => p.TransactionCode, f => $"TXN{f.Random.Number(1000, 9999)}")
            .RuleFor(p => p.OwningSide, "SELL") // Always SELL per original script
            .RuleFor(p => p.Method, f => f.PickRandom(methods))
            .RuleFor(p => p.CountryCode, f => f.PickRandom(countries))
            .RuleFor(p => p.InvoiceNumber, f => $"INV{f.Random.Number(100000, 999999)}")
            .RuleFor(p => p.InvoiceDate, f => GenerateDate(f));
    }

    private static string GenerateIban(Faker f)
    {
        var countries = new[] { "GB", "DE", "FR", "NL", "BE", "IT", "ES" };
        var countryCode = f.PickRandom(countries);
        var checkDigits = f.Random.Number(10, 99);
        var bban = f.Random.Long(100000000000000000L, 999999999999999999L).ToString();
        return $"{countryCode}{checkDigits}{bban}";
    }

    private static string GenerateDate(Faker f)
    {
        var date = f.Date.Between(new DateTime(2020, 1, 1), new DateTime(2024, 12, 31));
        return date.ToString("dd.MM.yyyy");
    }
}

public class PaymentRecord
{
    public string BeneficiaryName { get; set; } = string.Empty;
    public string UniqueReference { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PaymentCurrency { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankCountry { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IbanNumber { get; set; } = string.Empty;
    public string OtherRouting { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SettlementCurrency { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string ArriveBy { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public string OwningSide { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
}
