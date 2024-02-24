// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

namespace LineLoader
{
    public static class StringListExtensions
    {
        /// <summary>
        /// Filters the list of lines with specified options.
        /// </summary>
        /// <param name="originalLines">The original lines to filter.</param>
        /// <param name="isSuccess">(OUT) Was the filtering successfully done?</param>
        /// <param name="numberOfSkippedLines">(OUT) Number of lines skipped because they did not fill line requirements.</param>
        /// <param name="requiredLineBeforeFirstData">(OPTIONAL) The exact line (without LF or CR+LF) which must be before the first data line.</param>
        /// <param name="lineAfterLastData">(OPTIONAL) The exact line (without LF or CR+LF) that will stop reading lines (will stop at end of file anyway)</param>
        /// <param name="skipEmptyLines">(OPTIONAL) Should empty files be excluded from results? Default: true - empty lines are excluded and added to <paramref name="numberOfSkippedLines"/>.</param>
        /// <param name="requiredLinePrefix">(OPTIONAL) A string that must be at the start of an included line.</param>
        /// <param name="lineMustInclude">(OPTIONAL) A string that must exist as part of an included line.</param>
        /// <param name="requiredLineSuffix">(OPTIONAL) A string that must be at the end of an included line.</param>
        /// <param name="requiredLineLength">(OPTIONAL) Exact line length that must match an included line.</param>
        /// <param name="firstLineIndex">(OPTIONAL) Exclude all lines before this 0-based line index (requiredLineBeforeFirstData may be the last line before this one)</param>
        /// <param name="lastLineIndex">(OPTIONAL) Exclude all lines after this 0-based line index.</param>
        /// <param name="stopAtError">(OPTIONAL) Stop at first error (any line not matching line requirements except empty lines).</param>
        /// <returns>List of lines passing the filter.</returns>
        public static List<string> Filter(this List<string> originalLines,
                                          out bool isSuccess,
                                          out int numberOfSkippedLines,
                                          string? requiredLineBeforeFirstData = null,
                                          string? lineAfterLastData = null,
                                          bool skipEmptyLines = true,
                                          string? requiredLinePrefix = null,
                                          string? lineMustInclude = null,
                                          string? requiredLineSuffix = null,
                                          int? requiredLineLength = null,
                                          int? firstLineIndex = null,
                                          int? lastLineIndex = null,
                                          bool stopAtError = false)
        {
            isSuccess = true;
            List<string> selectedLines = [];
            int startFromIndex = 0;
            if (firstLineIndex.HasValue)
            {
                startFromIndex = firstLineIndex.Value;
            }
            if (requiredLineBeforeFirstData != null)
            {
                int startAfterIndex = originalLines.IndexOf(requiredLineBeforeFirstData,
                    startFromIndex > 0 ? startFromIndex - 1 : 0); // Start from previous line to detect the required line
                if (startAfterIndex >= startFromIndex)
                {
                    startFromIndex = startAfterIndex + 1;
                }
                else if (startAfterIndex < 0)
                {
                    isSuccess = false;
                    numberOfSkippedLines = 0;
                    return selectedLines;
                }
            }
            int endToIndex = originalLines.Count - 1;
            if (lastLineIndex.HasValue && lastLineIndex.Value < endToIndex)
            {
                endToIndex = lastLineIndex.Value;
            }
            if (lineAfterLastData != null)
            {
                int endBeforeIndex = originalLines.IndexOf(lineAfterLastData, startFromIndex);
                if (endBeforeIndex >= 0 && endBeforeIndex <= endToIndex)
                {
                    endToIndex = endBeforeIndex - 1;
                }
            }
            numberOfSkippedLines = 0;
            if (endToIndex < startFromIndex)
            {
                isSuccess = false;
                return selectedLines;
            }
            for (int lineIndex = startFromIndex; lineIndex <= endToIndex; lineIndex++)
            {
                string line = originalLines[lineIndex];
                if ((requiredLineLength.HasValue && line.Length != requiredLineLength.Value)
                    || (!string.IsNullOrEmpty(requiredLinePrefix) && !line.StartsWith(requiredLinePrefix))
                    || (!string.IsNullOrEmpty(lineMustInclude) && !line.Contains(lineMustInclude))
                    || (!string.IsNullOrEmpty(requiredLineSuffix) && !line.EndsWith(requiredLineSuffix)))
                {
                    isSuccess = false;
                    if (stopAtError)
                    {
                        if (lineAfterLastData != null)
                        {

                        }
                        numberOfSkippedLines = endToIndex - lineIndex + 1;
                        break;
                    }
                    else
                    {
                        numberOfSkippedLines++;
                        continue;
                    }
                }
                if (skipEmptyLines && string.IsNullOrWhiteSpace(line))
                {
                    numberOfSkippedLines++;
                    continue;
                }
                selectedLines.Add(line);
            }
            return selectedLines;
        }
    }
}
