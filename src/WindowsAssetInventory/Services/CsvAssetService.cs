using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Models;

namespace WindowsAssetInventory.Services;

public sealed class CsvAssetService
{
    private static readonly string[] RequiredHeaders =
    [
        "AssetTag",
        "Type",
        "Manufacturer",
        "Model",
        "SerialNumber",
        "Status"
    ];

    public async Task<IReadOnlyList<CreateAssetRequest>> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidDataException("The CSV file is empty.");
        }

        var headers = ParseLine(headerLine);
        var positions = headers
            .Select((name, index) => new { Name = name.Trim(), Index = index })
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);
        var missingHeaders = RequiredHeaders.Where(header => !positions.ContainsKey(header)).ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidDataException($"Missing required CSV columns: {string.Join(", ", missingHeaders)}.");
        }

        var results = new List<CreateAssetRequest>();
        var lineNumber = 1;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (results.Count >= 500)
            {
                throw new InvalidDataException("A single import is limited to 500 assets.");
            }

            var columns = ParseLine(line);
            string Value(string name)
            {
                if (!positions.TryGetValue(name, out var index))
                {
                    return string.Empty;
                }

                return index < columns.Count ? columns[index].Trim() : string.Empty;
            }

            var request = new CreateAssetRequest
            {
                AssetTag = Value("AssetTag"),
                Type = ParseAssetType(Value("Type"), lineNumber),
                Manufacturer = Value("Manufacturer"),
                Model = Value("Model"),
                SerialNumber = Value("SerialNumber"),
                Hostname = Optional(Value("Hostname")),
                Status = ParseAssetStatus(Value("Status"), lineNumber),
                PurchaseDate = ParseDate(Value("PurchaseDate"), "PurchaseDate", lineNumber),
                WarrantyUntil = ParseDate(Value("WarrantyUntil"), "WarrantyUntil", lineNumber)
            };

            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(
                    request,
                    new ValidationContext(request),
                    validationResults,
                    validateAllProperties: true))
            {
                throw new InvalidDataException(
                    $"CSV line {lineNumber}: {string.Join(" ", validationResults.Select(item => item.ErrorMessage))}");
            }

            results.Add(request);
        }

        return results;
    }

    public string Export(IEnumerable<AssetDto> assets)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "AssetTag,Type,Manufacturer,Model,SerialNumber,Hostname,Status,PurchaseDate,WarrantyUntil,AssignedUser");
        foreach (var asset in assets)
        {
            builder.AppendLine(string.Join(",",
                Escape(asset.AssetTag),
                Escape(asset.Type.ToString()),
                Escape(asset.Manufacturer),
                Escape(asset.Model),
                Escape(asset.SerialNumber),
                Escape(asset.Hostname),
                Escape(StatusName(asset.Status)),
                Escape(asset.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Escape(asset.WarrantyUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Escape(asset.AssignedUserName)));
        }

        return builder.ToString();
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (quoted)
        {
            throw new InvalidDataException("The CSV contains an unterminated quoted field.");
        }

        values.Add(current.ToString());
        return values;
    }

    private static AssetType ParseAssetType(string value, int lineNumber)
    {
        if (Enum.TryParse<AssetType>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidDataException($"CSV line {lineNumber}: unknown asset type '{value}'.");
    }

    private static AssetStatus ParseAssetStatus(string value, int lineNumber)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<AssetStatus>(normalized, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidDataException($"CSV line {lineNumber}: unknown asset status '{value}'.");
    }

    private static DateOnly? ParseDate(string value, string field, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result;
        }

        throw new InvalidDataException($"CSV line {lineNumber}: {field} must use yyyy-MM-dd.");
    }

    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string StatusName(AssetStatus status) =>
        status == AssetStatus.InStock ? "In Stock" : status.ToString();

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}
