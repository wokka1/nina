#region "copyright"

/*
    Copyright � 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

#if HAS_WPF
using System.Windows;
using Point = System.Windows.Point;
#endif
using Math = System.Math;

namespace NINA.Astrometry {

    public class ViewportFoV {
        public Coordinates CenterCoordinates { get; private set; }
        public double Width { get; }
        public double Height { get; }
        public double ArcSecWidth { get; }
        public double ArcSecHeight { get; }
#if HAS_WPF
        public Point ViewPortCenterPoint { get; }
#endif

        /// <summary>
        /// Portable (WPF-free) equivalent of ViewPortCenterPoint, computed alongside it -
        /// see NINA.Astrometry.Point2d.
        /// </summary>
        public Point2d ViewPortCenterPointPortable { get; }
        public double Rotation { get; }
        public double VFoV { get; }
        public double HFoV { get; }

        public ViewportFoV(Coordinates centerCoordinates, double vFoVDegrees, double width, double height, double rotation) {
            Rotation = rotation;

            Width = width;
            Height = height;

            VFoV = vFoVDegrees;
            HFoV = (vFoVDegrees / height) * width;

            ArcSecWidth = AstroUtil.DegreeToArcsec(HFoV) / Width;
            ArcSecHeight = AstroUtil.DegreeToArcsec(VFoV) / Height;

            CenterCoordinates = centerCoordinates;

#if HAS_WPF
            ViewPortCenterPoint = new Point(width / 2, height / 2);
#endif
            ViewPortCenterPointPortable = new Point2d(width / 2, height / 2);
        }

        public bool ContainsCoordinates(Coordinates coordinates) {
            var distance = coordinates - CenterCoordinates;
            return distance.Distance.Degree < Math.Max(HFoV, VFoV);
        }

        public bool ContainsCoordinates(double ra, double dec) {
            return ContainsCoordinates(new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees));
        }

#if HAS_WPF
        public void Shift(Vector delta) {
            ShiftPortable(delta.X, delta.Y);
        }
#endif

        /// <summary>
        /// Portable (WPF-free) equivalent of Shift(Vector) - Vector was only ever used here as a
        /// convenient (X,Y) pair, Coordinates.Shift itself already takes plain doubles, so this
        /// isn't really a "port" so much as skipping the WPF-only wrapper type entirely.
        /// </summary>
        public void ShiftPortable(double deltaX, double deltaY) {
            if (deltaX == 0 && deltaY == 0) {
                return;
            }

            CenterCoordinates = CenterCoordinates.Shift(deltaX, deltaY, Rotation, ArcSecWidth, ArcSecHeight);
        }
    }
}