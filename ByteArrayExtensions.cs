// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

namespace TextDataHandler;

/// <summary>
/// Extensions to <see cref="byte[]"/>.
/// </summary>
public static class ByteArrayExtensions
{
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
}
