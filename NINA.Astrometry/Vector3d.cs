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
    /// Portable, WPF-free 3D vector (double precision) - the Avalonia/backend-safe equivalent
    /// of System.Windows.Media.Media3D.Vector3D. Double precision is deliberate (not
    /// System.Numerics.Vector3's float) to match Vector3D's precision for astrometry math.
    /// </summary>
    public readonly struct Vector3d {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector3d(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
