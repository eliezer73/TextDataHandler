// SPDX-License-Identifier: EUPL-1.2+
//
// Copyleft © 2024 Eliezer - https://github.com/eliezer73
// Licensed under the EUPL version 1.2 or later: https://data.europa.eu/eli/dec_impl/2017/863/oj

namespace TextDataHandler;

/// <summary>
/// The data type for the field
/// </summary>
public enum FieldDataType
{
    /// <summary>
    /// A text field
    /// </summary>
    Text = 1,
    /// <summary>
    /// An integer number
    /// </summary>
    Integer = 2,
    /// <summary>
    /// A decimal number
    /// </summary>
    Decimal = 3,
    /// <summary>
    /// A date or date & time
    /// </summary>
    DateTime = 4,
    /// <summary>
    /// A boolean value (true or false)
    /// </summary>
    Boolean = 5
}
