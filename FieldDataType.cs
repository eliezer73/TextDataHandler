// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - mailto:eliezer@programmer.net?subject=TextDataHandler
// https://github.com/eliezer73/TextDataHandler
// Licensed under the EUPL: https://joinup.ec.europa.eu/licence/european-union-public-licence-version-12-or-later-eupl

namespace TextDataHandler;

/// <summary>
/// The data type for the field
/// </summary>
public enum FieldDataType
{
	/// <summary>
	/// No type specified (error)
	/// </summary>
	None = 0,
	/// <summary>
	/// A text field
	/// </summary>
	Text = 1,
	/// <summary>
	/// An integer number
	/// </summary>
	IntegerNumber = 2,
	/// <summary>
	/// A decimal number
	/// </summary>
	DecimalNumber = 3,
	/// <summary>
	/// A date or date &amp; time
	/// </summary>
	DateTime = 4,
	/// <summary>
	/// A boolean value (true or false)
	/// </summary>
	Boolean = 5
}
