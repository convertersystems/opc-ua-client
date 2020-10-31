// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{

    [DataTypeId(DataTypeIds.LocalizedText)]
    public sealed class LocalizedText : IEquatable<LocalizedText?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedText"/> class.
        /// </summary>
        /// <param name="text">The text in the specified locale.</param>
        /// <param name="locale">The locale.</param>
        public LocalizedText(string? text, string? locale = "")
        {
            Locale = locale;
            Text = text;
        }

        public string? Text { get; }

        public string? Locale { get; }

        public static implicit operator LocalizedText(string? a)
        {
            return new LocalizedText(a);
        }

        public static implicit operator string?(LocalizedText? a)
        {
            return a?.Text;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LocalizedText);
        }

        public bool Equals(LocalizedText? other)
        {
            return other != null &&
                   Text == other.Text &&
                   Locale == other.Locale;
        }

        public override int GetHashCode()
        {
            int hashCode = 670029253;
            if (Text != null) hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Text);
            if (Locale != null) hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(Locale);
            return hashCode;
        }

        public static bool operator ==(LocalizedText? left, LocalizedText? right)
        {
            return EqualityComparer<LocalizedText?>.Default.Equals(left, right);
        }

        public static bool operator !=(LocalizedText? left, LocalizedText? right)
        {
            return !(left == right);
        }

        public override string? ToString()
        {
            return Text;
        }
    }
}