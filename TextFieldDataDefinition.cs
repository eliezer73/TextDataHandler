using System.Diagnostics;

namespace TextDataHandler;

/// <summary>
/// Initializes a new TextFieldDataDefinition.
/// </summary>
/// <param name="name">Unique name for identifying the data field for response.</param>
/// <param name="dataType">(OPTIONAL) The data type of the field contents. Default: Text.</param>
/// <param name="formatProvider">(OPTIONAL) Format information for interpreting the data. Not applicable for text fields.</param>
/// <param name="regexPattern">(OPTIONAL) Custom regular expression pattern for which the field contents must be a match before parsing the data type.</param>
/// <param name="minLength">(OPTIONAL) Minimum length of the expected string in the source field.</param>
/// <param name="maxLength">(OPTIONAL) Maximum length of the expected string in the source field.</param>
[DebuggerDisplay("{Name} ({DataType})")]
public class TextFieldDataDefinition(string name, FieldDataType dataType = FieldDataType.Text, IFormatProvider? formatProvider = null, string? regexPattern = null, uint? minLength = null, uint? maxLength = null)
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
    public IFormatProvider? FormatProvider { get; set; } = formatProvider;
    /// <summary>
    /// Custom regular expression pattern for which the field contents must be a match before parsing the data type
    /// </summary>
    public string? RegexPattern { get; set; } = regexPattern;
    /// <summary>
    /// Minimum length of the expected string in the source field
    /// </summary>
    public uint? MinLength { get; set; } = minLength;
    /// <summary>
    /// Maximum length of the expected string in the source field
    /// </summary>
    public uint? MaxLength { get; set; } = maxLength;


}
