// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

using System.Linq.Expressions;
using System.Text;

namespace TextDataHandler;

public static class CSVManager
{
    public static List<Dictionary<TextFieldDataDefinition, object>> ReadFields(List<string> sourceLines,
                                                                               List<TextFieldDataDefinition> definitions,
                                                                               out bool isSuccess,
                                                                               out List<int>? errorLines,
                                                                               string[]? fieldSeparators = null,
                                                                               string[]? possibleQuotes = null,
                                                                               Encoding? expectedEncoding = null,
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
                string? sourceField = null;
                int maxFieldLength = maxIndex - currentIndex + 1;
                if (definition.MaxLength.HasValue && maxFieldLength > definition.MaxLength.Value)
                {
                    maxFieldLength = (int) definition.MaxLength.Value;
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
                        separatorIndex = sourceLine.IndexOf(fieldSeparator, currentIndex);
                        if (separatorIndex >= 0)
                        {
                            currentSeparator = fieldSeparator;
                            break;
                        }
                    }
                    if (separatorIndex > currentIndex)
                    {
                        sourceField = sourceLine[currentIndex..separatorIndex];
                    }
                }
                bool areQuotesRemoved = false;
                if (possibleQuotes != null)
                {
                    foreach (string quote in possibleQuotes)
                    {
                        if (string.IsNullOrEmpty(quote))
                        {
                            continue;
                        }
                        if (sourceField != null)
                        {
                            if (sourceField.StartsWith(quote) && sourceField.EndsWith(quote))
                            {
                                int secondQuoteIndex = sourceField.Length - quote.Length;
                                sourceField = sourceField[quote.Length..secondQuoteIndex];
                                areQuotesRemoved = true;
                            }
                            break;
                        }
                        else
                        {

                        }
                    }
                }
                sourceField ??= maxFieldLength > 0
                    ? sourceLine[currentIndex..maxFieldLength]
                    : string.Empty;
                if (definition.MinLength.HasValue && sourceField.Length < definition.MinLength.Value)
                {
                    isSuccess = false;
                    errorLines ??= [];
                    errorLines.Add(sourceIndex);
                    if (stopAtFirstError)
                    {
                        return result;
                    }
                }
                else
                {
                }
                if (currentSeparator != null && separatorIndex >= 0)
                {
                    currentIndex = separatorIndex + currentSeparator.Length;
                }
                else
                {
                    currentIndex += sourceField.Length;
                }
            }
        }
        return result;
    }

}
