// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TextDataHandler;

public static class CSVManager
{
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
