// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

using System.Text;

namespace TextDataHandler;

/// <summary>
/// Class for loading text lines from file
/// </summary>
public class LineLoader
{
    /// <summary>
    /// Non-public default constructor
    /// </summary>
    private LineLoader()
    {
    }

    /// <summary>
    /// Create a new instance of <see cref="LineLoader"/>.
    /// </summary>
    /// <param name="fileNameWithPath">The file name with path to read from.
    /// Leave out to auto-detect.
    /// </param>
    /// <exception cref="ArgumentNullException">File name with path must be specified</exception>
    public LineLoader(string fileNameWithPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithPath))
        {
            throw new ArgumentNullException(nameof(fileNameWithPath), "File name with path must be specified");
        }
        this.FileNameWithPath = fileNameWithPath;
    }

    /// <summary>
    /// Create a new instance of <see cref="LineLoader"/>.
    /// </summary>
    /// <param name="bytes">The byte array to read lines from.</param>
    public LineLoader(byte[] bytes)
    {
        this.Bytes = bytes;
    }

    /// <summary>
    /// Create a new instance of <see cref="LineLoader"/>.
    /// </summary>
    /// <param name="allLines">All lines to filter from.</param>
    public LineLoader(List<string> allLines)
    {
        this.AllLines = allLines;
    }

    public string? FileNameWithPath { get; private set; }

    public byte[]? Bytes { get; private set; }

    public Encoding? Encoding { get; private set; }

    public List<string>? AllLines { get; private set; }

    /// <summary>
    /// Gets a byte array of the contents of the file specified in constructor. Populates <see cref="Bytes"/>.
    /// </summary>
    /// <returns>Array of bytes read from file.</returns>
    /// <exception cref="Exception">File name with path was not specified in constructor</exception>
    /// <exception cref="FileNotFoundException">File not found: "{fileNameWithPath}"</exception>
    public byte[] GetFileBytes()
    {
        if (string.IsNullOrWhiteSpace(this.FileNameWithPath))
        {
            throw new Exception("File name with path was not specified in constructor");
        }
        if (!File.Exists(this.FileNameWithPath))
        {
            throw new FileNotFoundException($"File not found: \"{this.FileNameWithPath}\"", this.FileNameWithPath);
        }
        this.Bytes = File.ReadAllBytes(this.FileNameWithPath);
        return this.Bytes;
    }

    /// <summary>
    /// Gets all the lines from the source specified in constructor. Just returns <see cref="AllLines"/>
    /// if list of lines was specified in constructor or was already retrieved.
    /// Populates <see cref="Bytes"/>, if file name was specified in constructor.
    /// </summary>
    /// <param name="encoding">(OPTIONAL) Encoding to use primarily if lines have not already been decoded (may detect others individually for lines). Leave out to auto-detect. No effect if <see cref="AllLines"/> already populated.</param>
    /// <param name="tryAgainInCaseOfEncodingConflict">(OPTIONAL) If set to <c>true</c> (default) and there are multiple encodings for different lines,
    /// try multiple times to get the least number of encoding conflicts. If <c>false</c>, only return the first results.</param>
    /// <returns>List of lines read.</returns>
    /// <exception cref="ArgumentNullException">File name with path must be specified as file name was not specified in constructor</exception>
    /// <exception cref="FileNotFoundException">File not found: "{fileNameWithPath}"</exception>
    /// <exception cref="Exception">Error while reading file bytes</exception>
    public List<string> GetAllLines(Encoding? encoding = null,
                                    bool tryAgainInCaseOfEncodingConflict = true)
    {
        if (this.AllLines != null)
        {
            return this.AllLines;
        }
        if (this.Bytes == null)
        {
            this.GetFileBytes();
        }
        Byte[] sourceBytes = this.Bytes ?? [];
        Encoding? defaultEncoding = encoding != null ? Encoding.GetEncoding(encoding.CodePage) : null;
        int? originalCodePage = defaultEncoding?.CodePage;
        List<int> codePagesToCheck = [originalCodePage ?? 0];
        Dictionary<int, Dictionary<int, int>> codePagesChecked = [];
        Dictionary<int, List<string>> resultsByCodePage = [];
        do
        {
            originalCodePage = defaultEncoding?.CodePage;
            List<byte[]> byteLines = sourceBytes.SplitIntoLines();
            if (defaultEncoding == null && sourceBytes.Length > 10)
            {
                // Test encoding by possible byte order mark
                using MemoryStream testStream = new(sourceBytes[..10]);
                using StreamReader testReader = new(testStream, true);
                defaultEncoding = testReader.CurrentEncoding;
            }
            int lineIndex = 0;
            Dictionary<int, int> numberOfEncodedLinesByCodePage = [];
            List<string> lines = [];
            foreach (byte[] lineBytes in byteLines)
            {
                Encoding currentEncoding;
                if (defaultEncoding != null)
                {
                    currentEncoding = defaultEncoding;
                }
                else if (numberOfEncodedLinesByCodePage.Count > 0)
                {
                    currentEncoding = Encoding.GetEncoding(numberOfEncodedLinesByCodePage.OrderByDescending(x => x.Value).First().Key);
                    defaultEncoding = currentEncoding;
                }
                else
                {
                    currentEncoding = Encoding.UTF8;
                }
                string line = currentEncoding.GetString(lineBytes);
                // Either there is no detected encoding or there was a question mark that could indicate a
                // decoding failure (or just a question mark, but let's check).
                if ((defaultEncoding == null || line.Contains('?'))
                    && (lineBytes.CheckCharacterEncoding(currentEncoding, out Encoding? detectedEncoding) ?? true) // Detect encoding, suggest current
                    && detectedEncoding != null && detectedEncoding.CodePage != currentEncoding.CodePage)
                {
                    currentEncoding = detectedEncoding;
                    // Encoding was not the default
                    if (defaultEncoding == null || defaultEncoding.CodePage != detectedEncoding.CodePage)
                    {
                        int mostUsedCodePage = numberOfEncodedLinesByCodePage.Count > 0
                            ? numberOfEncodedLinesByCodePage.OrderByDescending(x => x.Value).First().Key
                            : detectedEncoding.CodePage;
                        if (detectedEncoding.CodePage == mostUsedCodePage
                            || (numberOfEncodedLinesByCodePage.TryGetValue(detectedEncoding.CodePage, out int numberOfLinesByDetectedEncoding)
                            && numberOfLinesByDetectedEncoding == numberOfEncodedLinesByCodePage[mostUsedCodePage]))
                        {
                            // Set the default encoding for the first time or change it to the detected one
                            // as it is now more common than the original one
                            defaultEncoding = detectedEncoding;
                        }
                    }
                    line = currentEncoding.GetString(lineBytes);
                }
                lines.Add(line);
                if (!numberOfEncodedLinesByCodePage.TryAdd(currentEncoding.CodePage, 1))
                {
                    numberOfEncodedLinesByCodePage[currentEncoding.CodePage] += 1;
                }
                lineIndex++;
            }
            codePagesChecked.Add(originalCodePage ?? 0, numberOfEncodedLinesByCodePage);
            resultsByCodePage.Add(originalCodePage ?? 0, lines);
            // Multiple code pages found and default encoding changed from original;
            // check if using any of the conflicting encodings would return the same encoding as a result
            if (tryAgainInCaseOfEncodingConflict && numberOfEncodedLinesByCodePage.Count > 1)
            {
                codePagesToCheck.AddRange(numberOfEncodedLinesByCodePage.Keys.Where(cp => !codePagesToCheck.Contains(cp)));
                int? nextCodePage = codePagesToCheck.FirstOrDefault(cp => !codePagesChecked.ContainsKey(cp));
                if (nextCodePage.HasValue)
                {
                    defaultEncoding = Encoding.GetEncoding(nextCodePage.Value);
                    continue;
                }
            }
        }
        while (tryAgainInCaseOfEncodingConflict);
        KeyValuePair<int, KeyValuePair<int, int>> encodingParameterWithBestResults =
            codePagesChecked.OrderByDescending(a => a.Value.OrderByDescending(b => b.Value).First()).
                Select(a => new KeyValuePair<int, KeyValuePair<int, int>>(a.Key,
                        a.Value.OrderByDescending(b => b.Value).First())).First();
        this.Encoding = Encoding.GetEncoding(encodingParameterWithBestResults.Value.Key);
        this.AllLines = resultsByCodePage[encodingParameterWithBestResults.Key];
        return this.AllLines;
    }

    /// <summary>
    /// Gets the specified lines from source specified in constructor or previously retrieved to <see cref="AllLines"/>.
    /// Filters the list of lines with specified options.
    /// </summary>
    /// <param name="isSuccess">(OUT) Were the lines successfully read?</param>
    /// <param name="numberOfSkippedLines">(OUT) Number of lines skipped because they did not fill line requirements.</param>
    /// <param name="encoding">(OPTIONAL) Encoding to use primarily if lines have not already been decoded (may detect others individually for lines). Leave out to auto-detect. No effect if <see cref="AllLines"/> already populated by <see cref="LineLoader(List{string})"/> constructor or a previous call to <see cref="GetAllLines()"/> or this method.</param>
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
    /// <returns>List of lines matching the conditions.</returns>
    public List<string> GetSelectedLines(out bool isSuccess,
                                         out int numberOfSkippedLines,
                                         Encoding? encoding = null,
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
        if (this.AllLines == null)
        {
            this.GetAllLines(encoding);
        }
        List<string> originalLines = this.AllLines ?? [];
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
