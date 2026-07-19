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
    }
}
