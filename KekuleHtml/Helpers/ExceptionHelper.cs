// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.Text;

namespace KekuleHtml.Helpers
{
    /// <summary>
    /// Provides helper methods for <see cref="Exception"/>s.
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Builds a message text from an <paramref name="ex"/>, including the messages of all inner exceptions and all the stack traces.
        /// </summary>
        public static string GetMessageText(Exception ex)
        {
            var sb = new StringBuilder();

            Exception? current = ex;
            while (current != null)
            {
                sb.AppendLine(current.Message);
                sb.AppendLine(current.StackTrace);
                sb.AppendLine();
                current = current.InnerException;
            }

            return sb.ToString();
        }
    }
}
