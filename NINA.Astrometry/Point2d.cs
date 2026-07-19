#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Astrometry {

    /// <summary>
    /// Portable, WPF-free 2D point (double precision) - the Avalonia/backend-safe equivalent
    /// of System.Windows.Point for pixel-coordinate math (WCS pixel references, sky-projection
    /// results) that has no actual UI dependency of its own.
    /// </summary>
    public readonly struct Point2d {
        public double X { get; }
        public double Y { get; }

        public Point2d(double x, double y) {
            X = x;
            Y = y;
        }
    }
}
