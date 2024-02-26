// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - mailto:eliezer@programmer.net?subject=TextDataHandler
// https://github.com/eliezer73/TextDataHandler
// Licensed under the EUPL: https://joinup.ec.europa.eu/licence/european-union-public-licence-version-12-or-later-eupl

using System.Globalization;
using System.Text.RegularExpressions;

namespace TextDataHandler;

/// <summary>
/// Class for handling data records where each record is one line in a file - data fields are
/// either in standard range positions within the line or separated by standard separator(s)
/// </summary>
public static class CSVManager
{
    /// <summary>
    /// Reads the data records from the list of lines and returns them in a structured format.
    /// </summary>
    /// <param name="sourceLines">The source lines - each line defining a single record.</param>
    /// <param name="definitions">The definitions for the fields within the record.</param>
    /// <param name="isSuccess">(OUT) Returns <c>true</c>, if the data interpretation was completely successful according to <paramref name="definitions"/>, otherwise, <c>false</c>.</param>
    /// <param name="errorLines">(OUT) If <paramref name="isSuccess"/> value was false, returns the list of zero-based line indexes on <paramref name="sourceLines"/> where there were problems. Contains only the first error line if <paramref name="stopAtFirstError"/> = <c>true</c>.</param>
    /// <param name="fieldSeparators">(OPTIONAL) Defines a field separator or multiple alternative separators that may separate record fields from each other.</param>
    /// <param name="possibleQuoteSigns">(OPTIONAL) If a field can be surrounded by quote signs, defines which character(s) can be used as quote signs for this purpose. NB! If the same quote sign can also exist within the data it must be escaped by either replacing the single sign with two consecutive same signs or adding \ sign as an escape sign in front of it.</param>
    /// <param name="stopAtFirstError">(OPTIONAL) If set to <c>true</c>, stops interpretation immediately when a line or field that cannot be handled according to <paramref name="definitions"/> is found. If set to <c>false</c> (= default behaviour), the problematic field or record is left out of the result but the handling continues on next field/record.</param>
    /// <returns>List of records in the format: a dictionary of field definition and the target object of the field in the data format specified in the definition.</returns>
    public static List<Dictionary<TextFieldDataDefinition, object>> ReadFields(List<string> sourceLines,
                                                                               List<TextFieldDataDefinition> definitions,
                                                                               out bool isSuccess,
                                                                               out List<int>? errorLines,
                                                                               string[]? fieldSeparators = null,
                                                                               char[]? possibleQuoteSigns = null,
                                                                               bool stopAtFirstError = false)
    {
        List<Dictionary<TextFieldDataDefinition, object>> result = [];
        isSuccess = true;
        errorLines = null;
        for (int sourceIndex = 0; sourceIndex < sourceLines.Count; sourceIndex++)
        {
            string sourceLine = sourceLines[sourceIndex];
            Dictionary<TextFieldDataDefinition, object> resultRecord = [];
            int maxIndex = sourceLine.Length - 1;
            int currentIndex = 0;
            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                TextFieldDataDefinition definition = definitions[definitionIndex];
                definition.FormatProvider ??= CultureInfo.InvariantCulture;
                int maxFieldLength = maxIndex - currentIndex + 1;
                if (definition.MaxLength.HasValue && maxFieldLength > definition.MaxLength.Value)
                {
                    maxFieldLength = (int) definition.MaxLength.Value;
                }
                string? sourceField = null;
                int endQuoteSignIndex = -1;
                if (possibleQuoteSigns != null && possibleQuoteSigns.Length > 0)
                {
                    if (possibleQuoteSigns.Contains(sourceLine[currentIndex]))
                    {
                        char quoteSign = sourceLine[currentIndex];
                        int beginQuoteSignCount = 0;
                        for (int i = currentIndex; i < sourceLine.Length && sourceLine[i] == quoteSign; i++)
                        {
                            beginQuoteSignCount++;
                        }
                        // Only one quote sign or odd number of consequtive quote signs can be considered a field-separator
                        if (beginQuoteSignCount % 2 == 1)
                        {
                            // Ignore quote signs escaped with backslash or another quote sign
                            for (endQuoteSignIndex = sourceLine.IndexOf(quoteSign, currentIndex + beginQuoteSignCount);
                                endQuoteSignIndex > currentIndex + beginQuoteSignCount
                                    && (sourceLine[endQuoteSignIndex - 1] == '\\'
                                        || (endQuoteSignIndex + 1 < sourceLine.Length && sourceLine[endQuoteSignIndex + 1] == '\"'));
                                endQuoteSignIndex = (endQuoteSignIndex + 1 < sourceLine.Length && sourceLine[endQuoteSignIndex - 1] == '\\')
                                    ? sourceLine.IndexOf(quoteSign, endQuoteSignIndex + 1)
                                    : ((endQuoteSignIndex + 2 < sourceLine.Length && sourceLine[endQuoteSignIndex + 1] == '\"')
                                        ? sourceLine.IndexOf(quoteSign, endQuoteSignIndex + 2)
                                        : -1));
                            if (endQuoteSignIndex > currentIndex + beginQuoteSignCount)
                            {
                                sourceField = sourceLine[(currentIndex + 1)..endQuoteSignIndex].Replace($"\\{quoteSign}", $"{quoteSign}").Replace($"{quoteSign}{quoteSign}", $"{quoteSign}");
                            }
                        }
                    }
                }
                int separatorIndex = -1;
                string? currentSeparator = null;
                if (fieldSeparators != null)
                {
                    foreach (string fieldSeparator in fieldSeparators)
                    {
                        if (string.IsNullOrEmpty(fieldSeparator))
                        {
                            continue;
                        }
                        separatorIndex = sourceLine.IndexOf(fieldSeparator, endQuoteSignIndex > currentIndex ? endQuoteSignIndex + 1 : currentIndex);
                        if (separatorIndex >= 0)
                        {
                            currentSeparator = fieldSeparator;
                            break;
                        }
                    }
                    if (sourceField == null && separatorIndex > currentIndex)
                    {
                        sourceField = sourceLine[currentIndex..separatorIndex];
                    }
                }
                sourceField ??= maxFieldLength > 0
                    ? sourceLine[currentIndex..(currentIndex + maxFieldLength)]
                    : string.Empty;
                if (currentSeparator != null && separatorIndex >= currentIndex)
                {
                    currentIndex = separatorIndex + currentSeparator.Length;
                }
                else if (endQuoteSignIndex > currentIndex)
                {
                    currentIndex = endQuoteSignIndex + 1;
                }
                else
                {
                    currentIndex += sourceField.Length;
                }
                if (definition.MaxLength.HasValue && definition.MaxLength.Value < sourceField.Length)
                {
                    sourceField = sourceField[..(int) definition.MaxLength.Value];
                }
                if (definition.MinLength.HasValue && sourceField.Length < definition.MinLength.Value
                    || (!string.IsNullOrEmpty(definition.RegexPattern)
                        && !Regex.IsMatch(sourceField, definition.RegexPattern)))
                {
                    isSuccess = false;
                    errorLines ??= [];
                    if (!errorLines.Contains(sourceIndex))
                    {
                        errorLines.Add(sourceIndex);
                    }
                    if (stopAtFirstError)
                    {
                        return result;
                    }
                    else
                    {
                        continue;
                    }
                }
                object? fieldValue;
                if (definition.DataType == FieldDataType.Text)
                {
                    fieldValue = sourceField;
                }
                else if (definition.DataType == FieldDataType.Boolean
                    && bool.TryParse(sourceField, out bool boolValue))
                {
                    fieldValue = boolValue;
                }
                else if ((definition.DataType == FieldDataType.Integer || definition.DataType == FieldDataType.Boolean)
                    && int.TryParse(sourceField, definition.FormatProvider, out int intValue))
                {
                    fieldValue = definition.DataType == FieldDataType.Integer
                        ? intValue
                        : (intValue != 0); // Non-zero: Boolean true, zero: Boolean false
                }
                else if (definition.DataType == FieldDataType.Decimal
                    && decimal.TryParse(sourceField, definition.FormatProvider, out decimal decimalValue))
                {
                    fieldValue = decimalValue;
                }
                else if (definition.DataType == FieldDataType.DateTime
                    && DateTime.TryParse(sourceField, definition.FormatProvider,
                        DateTimeStyles.AssumeLocal|DateTimeStyles.AllowWhiteSpaces|DateTimeStyles.NoCurrentDateDefault,
                        out DateTime dateTimeValue))
                {
                    fieldValue = dateTimeValue;
                }
                else
                {
                    isSuccess = false;
                    errorLines ??= [];
                    if (!errorLines.Contains(sourceIndex))
                    {
                        errorLines.Add(sourceIndex);
                    }
                    if (stopAtFirstError)
                    {
                        return result;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (fieldValue != null)
                {
                    if (!resultRecord.TryAdd(definition, fieldValue))
                    {
                        resultRecord[definition] = fieldValue;
                    }
                }
            }
            result.Add(resultRecord);
        }
        return result;
    }

}
