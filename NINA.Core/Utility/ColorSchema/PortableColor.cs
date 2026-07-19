#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Core.Utility.ColorSchema {

    /// <summary>
    /// Portable, WPF-free ARGB color (byte components) - the Avalonia/backend-safe equivalent
    /// of System.Windows.Media.Color for reading theme colors outside a WPF binding context.
    /// </summary>
    public readonly struct PortableColor {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public PortableColor(byte a, byte r, byte g, byte b) {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public static bool operator ==(PortableColor left, PortableColor right) =>
            left.A == right.A && left.R == right.R && left.G == right.G && left.B == right.B;

        public static bool operator !=(PortableColor left, PortableColor right) => !(left == right);

        public override bool Equals(object obj) => obj is PortableColor other && this == other;

        public override int GetHashCode() => (A, R, G, B).GetHashCode();

        /// <summary>
        /// Parses an 8-digit AARRGGBB hex string (e.g. "#FF550C18") - the same format
        /// used throughout this codebase's theme presets. Portable equivalent of
        /// (Color)ColorConverter.ConvertFromString(hex) for this specific format.
        /// </summary>
        public static PortableColor FromHex(string hex) {
            var s = hex.StartsWith("#") ? hex.Substring(1) : hex;
            byte a = byte.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte r = byte.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(s.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return new PortableColor(a, r, g, b);
        }
    }
}
