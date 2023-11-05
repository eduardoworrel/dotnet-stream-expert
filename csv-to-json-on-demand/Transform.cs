
using System.Text;
using System.Text.Json;
public static class Transform
{

    /// <summary>
    /// Transforms a CSV stream to a NDJSON stream.
    /// </summary>
    /// <returns>A function that converts a CSV formatted memory segment to a NDJSON byte array.</returns>
    public static Func<ReadOnlyMemory<byte>, Task<ReadOnlyMemory<byte>>> StreamCsvToNDJson()
    {
        string?[] headers = null;

        return async (ReadOnlyMemory<byte> segment) =>
        {
            var line = Encoding.UTF8.GetString(segment.Span);

            if (headers == null)
            {
                headers = line.Split(',');
                return Array.Empty<byte>();
            }
            
            var jsonBytes = Transform.CsvLineToJsonUtf8Bytes(headers, line);
            var newline = Encoding.UTF8.GetBytes("\n");

            var result = new byte[jsonBytes.Length + newline.Length];
            jsonBytes.CopyTo(result, 0);
            newline.CopyTo(result, jsonBytes.Length);
            return result;
        };
    }

    /// <summary>
    /// Converts a CSV line to a JSON formatted byte array using UTF8 encoding.
    /// </summary>
    /// <param name="headers">The CSV headers.</param>
    /// <param name="line">The CSV line.</param>
    /// <returns>The JSON formatted byte array.</returns>
    public static byte[] CsvLineToJsonUtf8Bytes(string?[] headers, string line)
    {
        var values = Transform.ParseCsvLine(line).ToArray();
        var obj = new Dictionary<string, string>();
        for (int j = 0; j < headers.Length; j++)
        {
            obj[headers[j]] = values.Length > j ? values[j] : null;
        }

        return JsonSerializer.SerializeToUtf8Bytes(obj);
    }
    
    /// <summary>
    /// Parses a single line of CSV data into fields.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <returns>An enumeration of field values.</returns>
    public static IEnumerable<string> ParseCsvLine(string line)
    {
        var field = new StringBuilder();
        var fields = new List<string>();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
                {
                    
                    field.Append('"');
                    i++; 
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());

        return fields;
    }
}