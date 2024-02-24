// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

using System.Text;

namespace LineLoader
{
    /// <summary>
    /// Class for loading text lines from file
    /// </summary>
    public static class LoadLines
    {
        /// <summary>
        /// Gets the specified lines from file.
        /// </summary>
        /// <param name="fileNameWithPath">The file name with path to read from.</param>
        /// <param name="fileEncoding">Encoding to use (may detect others).</param>
        /// <param name="isSuccess">(OUT) Were the lines successfully read?</param>
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
        /// <returns>List of lines</returns>
        public static List<string> GetSelectedLinesFromFile(string fileNameWithPath,
                                                            Encoding fileEncoding,
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
            Encoding? usedEncoding = fileEncoding;
            List<string> lines = LoadLines.FromFile(fileNameWithPath, ref usedEncoding);
            return lines.Filter(out isSuccess, out numberOfSkippedLines, requiredLineBeforeFirstData,
                lineAfterLastData, skipEmptyLines, requiredLinePrefix, lineMustInclude, requiredLineSuffix,
                requiredLineLength, firstLineIndex, lastLineIndex, stopAtError);
        }

        public static List<string> FromFile(string fileNameWithPath, ref Encoding? encoding)
        {
            return File.ReadAllBytes(fileNameWithPath).GetLines(ref encoding);
        }

    }
}
