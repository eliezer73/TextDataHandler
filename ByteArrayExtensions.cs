﻿// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

using System.Text;

namespace LineLoader
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Get a list of string lines from a byte array.
        /// </summary>
        /// <param name="bytes">Byte array to read from.</param>
        /// <param name="defaultEncoding">The default encoding.</param>
        /// <param name="tryAgainInCaseOfEncodingConflict">If there are multiple encodings for different lines,
        /// check which one of the codings does not change its default.</param>
        /// <returns>List of lines read.</returns>
        public static List<string> GetLines(this byte[] bytes, ref Encoding? defaultEncoding, bool tryAgainInCaseOfEncodingConflict = true)
        {
            int? originalCodePage = defaultEncoding?.CodePage;
            List<int> codePagesToCheck = [originalCodePage ?? 0];
            Dictionary<int, Dictionary<int, int>> codePagesChecked = [];
            Dictionary<int, List<string>> resultsByCodePage = [];
            do
            {
                originalCodePage = defaultEncoding?.CodePage;
                List<byte[]> byteLines = bytes.SplitIntoLines();
                if (defaultEncoding == null && bytes.Length > 10)
                {
                    // Test encoding by possible byte order mark
                    using MemoryStream testStream = new(bytes[..10]);
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
                    else
                    {
                        break;
                    }
                }
                else
                {
                    return lines;
                }
            }
            while (tryAgainInCaseOfEncodingConflict);
            return resultsByCodePage[codePagesChecked.OrderByDescending(n => n.Value.OrderByDescending(l => l.Value).First()).First().Key];
        }

        /// <summary>
        /// Splits the byte array into a list of byte arrays for each line
        /// </summary>
        /// <param name="bytes">The original byte array to split.</param>
        /// <returns>The byte array split into list of byte arrays, one for each line.</returns>
        public static List<byte[]> SplitIntoLines(this byte[] bytes)
        {
            List<byte[]> lines = [];
            List<byte> line = [];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte byteRead = bytes[index];
                if (byteRead == 10 || (byteRead == 13 && bytes.Length > index + 1 && bytes[index + 1] == 10))
                {
                    // Line ending detected
                    lines.Add([.. line]);
                    line = [];
                }
                else
                {
                    line.Add(byteRead);
                    if (index == bytes.Length - 1)
                    {
                        lines.Add([.. line]);
                        break;
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// Checks character encoding of the bytes - against suspected encoding or otherwise detected.
        /// </summary>
        /// <param name="bytes">The bytes to check characters for.</param>
        /// <param name="suspectedEncoding">The suspected encoding expected by caller.</param>
        /// <param name="detectedEncoding">(OUT) The actually detected encoding (may match suspected if given).</param>
        /// <returns><c>true</c> if a encoding match was made and it was either the same as suspected or no suspected encoding was given,
        /// <c>false</c> if an encoding could not be found and either detected encoding does not match the characters or
        /// the encoding is not supported by this method, or
        /// <c>null</c> if an encoding was found but it did not match suspected encoding - or if an encoding was not found but
        /// there was no evidence to support that it wasn't suspected encoding either - or there was no suspected encoding.
        /// </returns>
        public static bool? CheckCharacterEncoding(this byte[] bytes, Encoding? suspectedEncoding, out Encoding? detectedEncoding)
        {
            // Register encoding provider providing access to code pages only supported in .NET Framework
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            detectedEncoding = null;
            if (bytes.Length % 4 == 0)
            {
                int matchingWesternUTF32_LE = 0;
                int matchingWesternUTF32_BE = 0;
                int notMatchingWesternUTF32 = 0;
                for (int index = 0; index < bytes.Length; index += 4)
                {
                    byte firstByte = bytes[index];
                    byte secondByte = bytes[index + 1];
                    byte thirdByte = bytes[index + 2];
                    byte fourthByte = bytes[index + 3];

                    if (firstByte > 0 && secondByte == 0 && thirdByte == 0 && fourthByte == 0)
                    {
                        matchingWesternUTF32_LE++;
                    }
                    else if (firstByte == 0 && secondByte == 0 && thirdByte == 0 && fourthByte > 0)
                    {
                        matchingWesternUTF32_BE++;
                    }
                    else
                    {
                        notMatchingWesternUTF32++;
                    }
                }
                if (matchingWesternUTF32_LE > notMatchingWesternUTF32 && matchingWesternUTF32_BE == 0)
                {
                    detectedEncoding = Encoding.UTF32;
                }
                else if (matchingWesternUTF32_BE > notMatchingWesternUTF32 && matchingWesternUTF32_LE == 0)
                {
                    detectedEncoding = Encoding.GetEncoding(12001); // Unicode (UTF-32 Big-Endian)
                }
            }
            if (bytes.Length % 2 == 0
                && (detectedEncoding == null
                    || (suspectedEncoding != null
                        && (suspectedEncoding.CodePage == Encoding.Unicode.CodePage
                            || suspectedEncoding.CodePage == Encoding.BigEndianUnicode.CodePage))))
            {
                int matchingWesternUTF16_LE = 0;
                int matchingWesternUTF16_BE = 0;
                int notMatchingWesternUTF16 = 0;
                for (int index = 0; index < bytes.Length; index += 2)
                {
                    byte firstByte = bytes[index];
                    byte secondByte = bytes[index + 1];

                    if (firstByte > 0 && secondByte == 0)
                    {
                        matchingWesternUTF16_LE++;
                    }
                    else if (firstByte == 0 && secondByte > 0)
                    {
                        matchingWesternUTF16_BE++;
                    }
                    else
                    {
                        notMatchingWesternUTF16++;
                    }
                }
                if (matchingWesternUTF16_LE > notMatchingWesternUTF16 && matchingWesternUTF16_BE == 0)
                {
                    detectedEncoding = Encoding.Unicode;
                }
                else if (matchingWesternUTF16_BE > notMatchingWesternUTF16 && matchingWesternUTF16_LE == 0)
                {
                    detectedEncoding = Encoding.BigEndianUnicode;
                }
            }
            if (detectedEncoding != null && (suspectedEncoding == null || suspectedEncoding.CodePage == detectedEncoding.CodePage))
            {
                return true;
            }
            if (detectedEncoding == null ||
                (suspectedEncoding != null &&
                    (suspectedEncoding.IsSingleByte || suspectedEncoding.CodePage == Encoding.UTF8.CodePage)))
            {
                int asciiControlChars = 0;
                int asciiOtherChars = 0;
                int ia5GermanPotentialLetters = 0;
                int ia5SwedishPotentialLetters = 0;
                int ia5NorwegianPotentialLetters = 0;
                int squareBrackets = 0;
                int braces = 0;
                bool? is7Bit = null;
                bool? isUtf8 = null;
                int checkUtf8ExtraBytes = 0;
                int ibm437Letters = 0;
                int asmo708Letters = 0;
                int iso8859_1Chars = 0;
                int iso8859_15Chars = 0;
                int windows1252Chars = 0;
                for (int index = 0; index < bytes.Length; index++)
                {
                    byte checkByte = bytes[index];
                    if (checkUtf8ExtraBytes > 0 && (checkByte < 128 || checkByte >= 192))
                    {
                        // A byte should be within range 128 - 191 to contribute to a valid UTF-8 character
                        // after valid start byte(s)
                        isUtf8 = false;
                        checkUtf8ExtraBytes = 0;
                    }
                    // Check all other characteristics of the byte being checked

                    // Check 7-bit (mostly) ASCII-based character sets
                    if (checkByte < 128)
                    {
                        if (!is7Bit.HasValue)
                        {
                            is7Bit = true;
                        }
                        if ((checkByte >= 0 && checkByte < 9) || (checkByte >= 10 && checkByte < 32))
                        {
                            asciiControlChars++;
                        }
                        else if (checkByte == 64)
                        {
                            asciiOtherChars++;
                            ia5SwedishPotentialLetters++;
                        }
                        else if (checkByte == 91)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                            squareBrackets++;
                        }
                        else if (checkByte == 92)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                        }
                        else if (checkByte == 93)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                            squareBrackets--;
                        }
                        else if (checkByte == 94 || checkByte == 96)
                        {
                            asciiOtherChars++;
                            ia5SwedishPotentialLetters++;
                        }
                        else if (checkByte == 123)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                            braces++;
                        }
                        else if (checkByte == 124)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                        }
                        else if (checkByte == 125)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                            ia5NorwegianPotentialLetters++;
                            braces--;
                        }
                        else if (checkByte == 126)
                        {
                            asciiOtherChars++;
                            ia5GermanPotentialLetters++;
                            ia5SwedishPotentialLetters++;
                        }
                        else if (checkByte == 127)
                        {
                            asciiControlChars++;
                        }
                        else
                        {
                            asciiOtherChars++;
                        }
                    }
                    else
                    {
                        is7Bit = false;
                        // 437 OEM United States
                        if ((checkByte >= 128 && checkByte < 155) || (checkByte >= 160 && checkByte < 166)
                            || checkByte == 168 || checkByte == 173 || (checkByte >= 224 && checkByte < 239))
                        {
                            ibm437Letters++;
                        }
                        // 708 Arabic (ASMO 708)
                        if (checkByte == 130 || checkByte == 131 || checkByte == 133 || (checkByte >= 135 && checkByte < 141)
                            || checkByte == 147 || checkByte == 150 || checkByte == 151 || checkByte == 154 || checkByte == 156
                            || checkByte == 158 || checkByte == 159 || (checkByte >= 191 && checkByte < 219)
                            || (checkByte >= 224 && checkByte < 235))
                        {
                            asmo708Letters++;
                        }
                        // 1252 Western European (Windows)
                        if (checkByte == 128 || checkByte == 138 || checkByte == 140 || checkByte == 142
                            || checkByte == 154 || checkByte == 156 || checkByte == 158 || checkByte == 159)
                        {
                            windows1252Chars++;
                        }
                        // 28605 Latin 9 (ISO 8859-15)
                        if (checkByte == 166 || checkByte == 168 || checkByte == 180 || checkByte == 184
                            || (checkByte >= 180 && checkByte < 191))
                        {
                            iso8859_15Chars++;
                        }
                        // 28591 Western European (ISO 8859-1)
                        if (checkByte == 161 || checkByte >= 191 && checkByte != 215 && checkByte != 247)
                        {
                            iso8859_1Chars++;
                        }
                        // 65001 Unicode (UTF-8)
                        if (!isUtf8.HasValue || isUtf8.Value == true)
                        {
                            if (checkByte >= 128 && checkByte < 192 && checkUtf8ExtraBytes > 0)
                            {
                                checkUtf8ExtraBytes--;
                                if (checkUtf8ExtraBytes == 0 && !isUtf8.HasValue)
                                {
                                    isUtf8 = true;
                                }
                            }
                            else if (checkByte >= 192 && checkByte < 248 && checkUtf8ExtraBytes == 0)
                            {
                                if (checkByte < 224)
                                {
                                    // 110xxxxx & 10xxxxxx
                                    checkUtf8ExtraBytes = 1;
                                }
                                else if (checkByte < 240)
                                {
                                    // 1110xxxx & 10xxxxxx & 10xxxxxx
                                    checkUtf8ExtraBytes = 2;
                                }
                                else
                                {
                                    // 11110xxx & 10xxxxxx & 10xxxxxx & 10xxxxxx
                                    checkUtf8ExtraBytes = 3;
                                }
                            }
                            else
                            {
                                // A byte within this range is unexpected at this position for a valid UTF-8 character
                                isUtf8 = false;
                            }
                        }
                    }
                }
                if (isUtf8 ?? false)
                {
                    if (suspectedEncoding != null && suspectedEncoding.CodePage == Encoding.UTF8.CodePage)
                    {
                        detectedEncoding = suspectedEncoding;
                        return true;
                    }
                    else
                    {
                        detectedEncoding = Encoding.UTF8;
                        return suspectedEncoding != null ? null : true;
                    }
                }
                else
                {
                    bool isAsciiDetected = false;
                    if (asciiControlChars < 2 && asciiOtherChars > asciiControlChars * 4)
                    {
                        isAsciiDetected = true;
                    }
                    if (is7Bit ?? false)
                    {
                        if (suspectedEncoding != null && ((suspectedEncoding.CodePage == 20106 && ia5GermanPotentialLetters > 0) // German (IA5)
                        || (suspectedEncoding.CodePage == 20107 && ia5SwedishPotentialLetters > 0) // Swedish (IA5)
                        || (suspectedEncoding.CodePage == 20108 && ia5NorwegianPotentialLetters > 0))) // Norwegian (IA5)
                        {
                            detectedEncoding = suspectedEncoding;
                            return isAsciiDetected ? true : null;
                        }
                        // If there are non-equal number of opening and closing square brackets or braces, check separately:
                        // 20106 German (IA5)
                        // 20107 Swedish (IA5)
                        // 20108 Norwegian (IA5)
                        else if (suspectedEncoding == null && detectedEncoding == null
                            && (squareBrackets > 2 || squareBrackets < -2 || braces > 2 || braces < -2))
                        {
                            if (ia5NorwegianPotentialLetters >= ia5GermanPotentialLetters
                                && ia5NorwegianPotentialLetters > ia5SwedishPotentialLetters)
                            {
                                detectedEncoding = Encoding.GetEncoding(20108); // Norwegian (IA5)
                            }
                            else if (ia5GermanPotentialLetters > ia5NorwegianPotentialLetters
                                && ia5GermanPotentialLetters > ia5SwedishPotentialLetters)
                            {
                                detectedEncoding = Encoding.GetEncoding(20106); // German (IA5)
                            }
                            else if (ia5SwedishPotentialLetters > 0)
                            {
                                detectedEncoding = Encoding.GetEncoding(20107); // Swedish (IA5)
                            }
                            if (detectedEncoding != null)
                            {
                                return isAsciiDetected ? true : null;
                            }
                        }

                        if (suspectedEncoding != null
                            && new List<int> {
                                437, // OEM United States
                                708, // Arabic (ASMO 708)
                                720, // Arabic (DOS)
                                737, // Greek (DOS)
                                775, // Baltic (DOS)
                                850, // Western European (DOS)
                                852, // Central European (DOS)
                                855, // OEM Cyrillic
                                857, // Turkish (DOS)
                                858, // OEM Multilingual Latin I
                                860, // Portuguese (DOS)
                                861, // Icelandic (DOS)
                                862, // Hebrew (DOS)
                                863, // French Canadian (DOS)
                                864, // Arabic (864)
                                865, // Nordic (DOS)
                                866, // Cyrillic (DOS)
                                869, // Greek, Modern (DOS)
                                874, // Thai (Windows)
                                1250, // Central European (Windows)
                                1251, // Cyrillic (Windows)
                                1252, // Western European (Windows)
                                1253, // Greek (Windows)
                                1254, // Turkish (Windows)
                                1255, // Hebrew (Windows)
                                1256, // Arabic (Windows)
                                1257, // Baltic (Windows)
                                1258, // Vietnamese (Windows)
                                10000, // Western European (Mac)
                                10004, // Arabic (Mac)
                                10005, // Hebrew (Mac)
                                10006, // Greek (Mac)
                                10007, // Cyrillic (Mac)
                                10010, // Romanian (Mac)
                                10017, // Ukrainian (Mac)
                                10021, // Thai (Mac)
                                10029, // Central European (Mac)
                                10079, // Icelandic (Mac)
                                10081, // Turkish (Mac)
                                10082, // Croatian (Mac)
                                20105, // Western European (IA5)
                                20106, // German (IA5)
                                20107, // Swedish (IA5)
                                20108, // Norwegian (IA5)
                                20127, // US-ASCII
                                20269, // ISO-6937
                                20866, // Cyrillic (KOI8-R)
                                21866, // Cyrillic (KOI8-U)
                                28591, // Western European (ISO 8859-1)
                                28592, // Central European (ISO)
                                28593, // Latin 3 (ISO)
                                28594, // Baltic (ISO)
                                28595, // Cyrillic (ISO)
                                28596, // Arabic (ISO)
                                28597, // Greek (ISO)
                                28598, // Hebrew (ISO-Visual)
                                28599, // Turkish (ISO)
                                28603, // Estonian (ISO)
                                28605, // Latin 9 (ISO 8859-15)
                                29001, // Europa
                                38598, // Hebrew (ISO-Logical)
                                65001 // Unicode (UTF-8)
                            }.Contains(suspectedEncoding.CodePage))
                        {
                            // Any ASCII-compatible encoding is a match
                            detectedEncoding = suspectedEncoding;
                            return isAsciiDetected ? true : null;
                        }
                        else
                        {
                            detectedEncoding = Encoding.ASCII; // US-ASCII
                            return isAsciiDetected
                                ? (suspectedEncoding == null) // Not supporting non-ASCII based 7 or 8 bit encodings
                                : null;
                        }
                    }
                    else if (iso8859_1Chars > 0 && isAsciiDetected)
                    {
                        if (windows1252Chars == 0 && iso8859_15Chars == 0)
                        {
                            if (suspectedEncoding != null
                                && new List<int> {
                                    1252, // Western European (Windows)
                                    28591, // Western European (ISO 8859-1)
                                    28605 // Latin 9 (ISO 8859-15)
                                }.Contains(suspectedEncoding.CodePage))
                            {
                                detectedEncoding = suspectedEncoding;
                                return true;
                            }
                            else
                            {
                                detectedEncoding = Encoding.GetEncoding(28591); // Western European (ISO 8859-1)
                                return (suspectedEncoding == null) ? true : null;
                            }
                        }
                        else if (iso8859_15Chars > 0 && windows1252Chars == 0)
                        {
                            if (suspectedEncoding != null
                                && new List<int> {
                                    1252, // Western European (Windows)
                                    28605 // Latin 9 (ISO 8859-15)
                                }.Contains(suspectedEncoding.CodePage))
                            {
                                detectedEncoding = suspectedEncoding;
                                return true;
                            }
                            else
                            {
                                detectedEncoding = Encoding.GetEncoding(28605); // Western European (ISO 8859-15)
                                return (suspectedEncoding == null) ? true : null;
                            }
                        }
                        else if (windows1252Chars > 0)
                        {
                            if (suspectedEncoding != null && suspectedEncoding.CodePage == 1252) // Western European (Windows)
                            {
                                detectedEncoding = suspectedEncoding;
                                return true;
                            }
                            else
                            {
                                detectedEncoding = Encoding.GetEncoding(1252); // Western European (Windows)
                                return (suspectedEncoding == null) ? true : null;
                            }
                        }
                    }
                    else
                    {
                        detectedEncoding = null;
                        return false;
                    }
                }
            }
            if (detectedEncoding != null)
            {
                if (suspectedEncoding == null)
                {
                    return true;
                }
                else if (suspectedEncoding.CodePage == detectedEncoding.CodePage)
                {
                    detectedEncoding = suspectedEncoding;
                    return true;
                }
                else
                {
                    return false; // Not supporting other multi-byte encodings than already detected ones
                }
            }
            else
            {
                return false; // Nothing detected and not supporting undetected ones
            }
        }
    }
}
