namespace TextDataHandler;

/// <summary>
/// Initializes a new TextFieldDataDefinition.
/// </summary>
/// <param name="name">Unique name for identifying the data field for response.</param>
/// <param name="dataType">The data type of the field contents.</param>
/// <param name="formatProvider">Format information for interpreting the data.</param>
/// <param name="minLength">(OPTIONAL) Minimum length of the expected string in the source field.</param>
/// <param name="maxLength">(OPTIONAL) Maximum length of the expected string in the source field.</param>
public class TextFieldDataDefinition(string name, FieldDataType dataType, IFormatProvider formatProvider, uint? minLength = null, uint? maxLength = null)
{
    /// <summary>
    /// Unique name for identifying the data field for response
    /// </summary>
    public string Name { get; set; } = name;
    /// <summary>
    /// The data type of the contents
    /// </summary>
    public FieldDataType DataType { get; set; } = dataType;
    /// <summary>
    /// Format information for interpreting the data
    /// </summary>
    public IFormatProvider FormatProvider { get; set; } = formatProvider;
    /// <summary>
    /// Minimum length of the expected string in the source field
    /// </summary>
    public uint? MinLength { get; set; } = minLength;
    /// <summary>
    /// Maximum length of the expected string in the source field
    /// </summary>
    public uint? MaxLength { get; set; } = maxLength;
}
