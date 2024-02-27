// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - mailto:eliezer@programmer.net?subject=TextDataHandler
// https://github.com/eliezer73/TextDataHandler
// Licensed under the EUPL: https://joinup.ec.europa.eu/licence/european-union-public-licence-version-12-or-later-eupl

using System.Collections.Generic;

namespace TextDataHandler;

/// <summary>
/// Extensions to byte collections
/// </summary>
public static class ByteCollectionExtensions
{
	/// <summary>
	/// Splits the byte collection into a list of byte collections for each line
	/// </summary>
	/// <param name="bytes">The original byte collection to split.</param>
	/// <returns>The byte array split into list of byte collections, one for each line.</returns>
	public static ICollection<ICollection<byte>> SplitIntoLines(this ICollection<byte> bytes)
	{
		byte[] byteArray = bytes != null ? (byte[]) bytes : [];
		ICollection<ICollection<byte>> lines = [];
		ICollection<byte> line = [];
		for (int index = 0; index < byteArray.Length; index++)
		{
			byte byteRead = byteArray[index];
			if (byteRead == 10 || (byteRead == 13 && byteArray.Length > index + 1 && byteArray[index + 1] == 10))
			{
				// Line ending detected
				lines.Add([.. line]);
				line = [];
			}
			else
			{
				line.Add(byteRead);
				if (index == byteArray.Length - 1)
				{
					lines.Add([.. line]);
					break;
				}
			}
		}
		return lines;
	}
}
