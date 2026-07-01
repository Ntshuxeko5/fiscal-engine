namespace Fiscal.Core.Domain;

/// <summary>
/// Generic, schema-agnostic container for data crossing a client boundary -
/// POS check data, fiscal device responses, payment results. Field shape
/// is determined by configuration/database schema at runtime, not by a
/// fixed C# class, so a new client never requires a new type or a
/// code change here.
/// </summary>
public class DynamicRecord
{
    private readonly Dictionary<string, object?> _fields;

    public DynamicRecord() => _fields = new Dictionary<string, object?>();

    public DynamicRecord(IDictionary<string, object?> fields) =>
        _fields = new Dictionary<string, object?>(fields);

    /// <summary>
    /// Reads a field by name. Returns null if the field doesn't exist -
    /// callers (e.g. the payload builder) decide how to handle a missing field.
    /// </summary>
    public object? Get(string fieldName) =>
        _fields.TryGetValue(fieldName, out var value) ? value : null;

    /// <summary>
    /// Strongly-typed read, for callers who know what type to expect
    /// even though the field name is dynamic.
    /// </summary>
    public T? Get<T>(string fieldName)
    {
        var raw = Get(fieldName);
        if (raw is null)
        {
            return default;
        }

        if (raw is T typed)
        {
            return typed;
        }

        // Handles cases like a number coming through as decimal but
        // requested as int, or similar safe conversions.
        return (T)Convert.ChangeType(raw, typeof(T));
    }

    public void Set(string fieldName, object? value) => _fields[fieldName] = value;

    public bool Has(string fieldName) => _fields.ContainsKey(fieldName);

    /// <summary>
    /// Nested records, e.g. a line item array, are stored as a list of
    /// DynamicRecord. This reads one of those collections by field name.
    /// </summary>
    public IReadOnlyList<DynamicRecord> GetRecordList(string fieldName)
    {
        if (Get(fieldName) is IReadOnlyList<DynamicRecord> list)
        {
            return list;
        }

        return Array.Empty<DynamicRecord>();
    }
}

/// <summary>
/// The raw transaction read from the POS in step 1. Wraps a DynamicRecord
/// rather than declaring fixed properties, since the actual field names
/// and shape come from the client's database schema / config, not from
/// a hardcoded C# contract.
/// </summary>
public class PosCheck
{
    public DynamicRecord Data { get; }

    public PosCheck(DynamicRecord data) => Data = data;
}
