#region "copyright"

/*
    Copyright 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using System.Drawing.Imaging;

namespace NINA.Image.ImageAnalysis {

    /// <summary>
    /// Portable, Bitmap/GDI+-free entry point for Accord.Imaging's Grayscale filter -
    /// same core ProcessFilter() logic (inherited, untouched, third-party vendored
    /// code), invoked directly (protected members are callable from a subclass, no
    /// reflection needed) via a plain UnmanagedImage wrapper around pinned managed
    /// arrays instead of a real System.Drawing.Bitmap. UnmanagedImage itself has no
    /// GDI+ P/Invoke dependency (confirmed by inspection - it's just an IntPtr +
    /// width/height/stride/PixelFormat holder), only Bitmap/Graphics actually touch
    /// native GDI+, so this stays safe on platforms where System.Drawing.Common's
    /// Bitmap path doesn't work (see project_multiagent_bigproject memory).
    /// </summary>
    public class GrayscalePortable : Grayscale {

        public GrayscalePortable(double cr, double cg, double cb) : base(cr, cg, cb) {
        }

        /// <summary>
        /// Converts a raw interleaved Rgb48 pixel array to Gray16, using the exact
        /// same weighted-sum logic as the inherited ProcessFilter().
        /// </summary>
        public unsafe ushort[] ApplyToRgb48Array(ushort[] source, int width, int height) {
            var destination = new ushort[width * height];
            int srcStride = width * 3 * sizeof(ushort);
            int dstStride = width * sizeof(ushort);

            fixed (ushort* srcPtr = source)
            fixed (ushort* dstPtr = destination) {
                var src = new UnmanagedImage((System.IntPtr)srcPtr, width, height, srcStride, PixelFormat.Format48bppRgb);
                var dst = new UnmanagedImage((System.IntPtr)dstPtr, width, height, dstStride, PixelFormat.Format16bppGrayScale);
                ProcessFilter(src, dst);
            }

            return destination;
        }
    }
}
