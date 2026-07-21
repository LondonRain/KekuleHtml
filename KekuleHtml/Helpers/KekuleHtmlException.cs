// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
namespace KekuleHtml.Helpers;

/// <summary>
/// Thrown when something goes wrong and needed to be enriched with <see cref="KekuleHtml"/> specific information.
/// </summary>
#pragma warning disable RCS1194 // Implement exception constructors
public sealed class KekuleHtmlException : Exception
#pragma warning restore RCS1194 // Implement exception constructors
{
    /// <summary>
    /// Creates an exception with a custom <paramref name="message"/>.
    /// </summary>
    public KekuleHtmlException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates an exception with a custom <paramref name="message"/> and an <paramref name="innerException"/>.
    /// </summary>
    public KekuleHtmlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
