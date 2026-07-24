#region "copyright"

/*
    Copyright � 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using SixLabors.ImageSharp.Drawing;
using System;
using System.Numerics;
using PointF = SixLabors.ImageSharp.PointF;

namespace NINA.WPF.Base.SkySurvey.Portable {

    /// <summary>
    /// Shared geometry helpers for the ImageSharp-based ("Portable") rendering path that mirrors the
    /// existing System.Drawing/GDI+-based Framing Assistant drawing code on a non-Windows platform.
    /// ImageSharp.Drawing has no GDI+-style "current transform" graphics-state stack - paths are immutable
    /// geometry that must be transformed before drawing - so per-shape rotation (e.g. a DSO's position angle)
    /// has to bake the rotation into the path itself around an explicit pivot, rather than push/pop a transform.
    /// </summary>
    public static class PortableDrawingUtility {

        public static float DegreesToRadians(float degrees) => (float)(degrees * Math.PI / 180.0);

        /// <summary>
        /// Rotates a path by <paramref name="degrees"/> around <paramref name="pivot"/> (not necessarily the path's own origin).
        /// Mirrors the effect of GDI+'s TranslateTransform(pivot) + RotateTransform(degrees) + TranslateTransform(-pivot) pattern
        /// used by the original System.Drawing-based Draw() methods.
        /// </summary>
        public static IPath RotateAround(IPath path, PointF pivot, float degrees) {
            if (degrees == 0) {
                return path;
            }
            var toOrigin = Matrix3x2.CreateTranslation(-pivot.X, -pivot.Y);
            var rotate = Matrix3x2.CreateRotation(DegreesToRadians(degrees));
            var fromOrigin = Matrix3x2.CreateTranslation(pivot.X, pivot.Y);
            return path.Transform(toOrigin * rotate * fromOrigin);
        }
    }
}
