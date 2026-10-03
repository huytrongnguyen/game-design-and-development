using System.Text.Json;

namespace Course.Properties;

/// <summary>
/// Loads a JSON table of the form {"rows":[{"id":"fighter","str":70,...}]} against a schema.
/// It collects every error instead of stopping at the first, so one run shows a designer the whole list.
/// </summary>
public static class TableLoader
{
    public static LoadResult Load(TableSchema schema, string json)
    {
        var errors = new List<ValidationError>();
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add(new ValidationError(schema.Name, "", "", $"file is not valid JSON ({ex.Message})"));
            return new LoadResult(null, errors);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("rows", out var rowsEl) ||
                rowsEl.ValueKind != JsonValueKind.Array)
            {
                errors.Add(new ValidationError(schema.Name, "", "rows", "expected a top-level array named 'rows'"));
                return new LoadResult(null, errors);
            }

            var rows = new List<DataRow>();
            var seen = new HashSet<string>();
            var index = 0;
            foreach (var el in rowsEl.EnumerateArray())
            {
                var row = ReadRow(schema, el, index++, errors);
                if (row is null) continue;
                if (!seen.Add(row.Id))
                    errors.Add(new ValidationError(schema.Name, row.Id, "id", "duplicate id"));
                else
                    rows.Add(row);
            }

            return errors.Count > 0
                ? new LoadResult(null, errors)
                : new LoadResult(new DataTable(schema.Name, rows), errors);
        }
    }

    private static DataRow? ReadRow(TableSchema schema, JsonElement el, int index, List<ValidationError> errors)
    {
        if (el.ValueKind != JsonValueKind.Object ||
            !el.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
        {
            errors.Add(new ValidationError(schema.Name, $"#{index}", "id", "every row needs a string 'id'"));
            return null;
        }

        var id = idEl.GetString()!;
        var values = new Dictionary<string, object>();
        var known = schema.Fields.Select(f => f.Name).ToHashSet();

        foreach (var prop in el.EnumerateObject())
            if (prop.Name != "id" && !known.Contains(prop.Name))
                errors.Add(new ValidationError(schema.Name, id, prop.Name, "unknown field (typo?)"));

        foreach (var field in schema.Fields)
        {
            if (!el.TryGetProperty(field.Name, out var v))
            {
                if (field.Required)
                    errors.Add(new ValidationError(schema.Name, id, field.Name, "required field is missing"));
                continue;
            }

            if (field.Kind == FieldKind.Number)
            {
                if (v.ValueKind != JsonValueKind.Number)
                {
                    errors.Add(new ValidationError(schema.Name, id, field.Name, "expected a number"));
                    continue;
                }

                var n = v.GetDouble();
                if ((field.Min is { } lo && n < lo) || (field.Max is { } hi && n > hi))
                {
                    errors.Add(new ValidationError(schema.Name, id, field.Name,
                        $"{n} is outside the allowed range {field.Min?.ToString() ?? "-inf"}..{field.Max?.ToString() ?? "+inf"}"));
                    continue;
                }

                values[field.Name] = n;
            }
            else
            {
                if (v.ValueKind != JsonValueKind.String)
                {
                    errors.Add(new ValidationError(schema.Name, id, field.Name, "expected text"));
                    continue;
                }

                values[field.Name] = v.GetString()!;
            }
        }

        return new DataRow(id, values);
    }
}
