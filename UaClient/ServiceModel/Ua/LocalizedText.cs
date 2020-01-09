// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Workstation.ServiceModel.Ua
{
    public sealed class LocalizedText
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedText"/> class.
        /// </summary>
        /// <param name="text">The text in the specified locale.</param>
        /// <param name="locale">The locale.</param>
        public LocalizedText(string? text, string? locale = "")
        {
            this.Locale = locale;
            this.Text = text;
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

        public static bool operator ==(LocalizedText? a, LocalizedText? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
            {
                return false;
            }

            return (a.Text == b.Text) && (a.Locale == b.Locale);
        }

        public static bool operator !=(LocalizedText? a, LocalizedText? b)
        {
            return !(a == b);
        }

        public override bool Equals(object? o)
        {
            if (o is LocalizedText)
            {
                return this == (LocalizedText)o;
            }

            return false;
        }

        public bool Equals(LocalizedText? that)
        {
            return this == that;
        }

        public override int GetHashCode()
        {
            int result = this.Locale != null ? this.Locale.GetHashCode() : 0;
            result = (397 * result) ^ (this.Text != null ? this.Text.GetHashCode() : 0);
            return result;
        }

        public override string? ToString()
        {
            return this.Text;
        }
    }
}