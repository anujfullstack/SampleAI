namespace NLToSQLApp.Models;

/// <summary>
/// Represents information about a database column
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Column name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Column data type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column allows null values
    /// </summary>
    public bool NotNull { get; set; }

    /// <summary>
    /// Whether the column is a primary key
    /// </summary>
    public bool PrimaryKey { get; set; }
}