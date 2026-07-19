#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using Accord.Imaging.Filters;
using System.Drawing;
using System.Drawing.Imaging;

namespace NINA.Image.ImageAnalysis {

    /// <summary>
    /// Portable, Bitmap/GDI+-free entry point for Accord.Imaging's Median filter - see
    /// CannyEdgeDetectorPortable for the rationale behind the double-clone (source-copy
    /// plus pre-filled destination) pattern used to reproduce ApplyInPlace's semantics.
    /// </summary>
    public class MedianPortable : Median {

        public unsafe byte[] ApplyInPlaceToGray8Array(byte[] source, int width, int height) {
            var sourceCopy = (byte[])source.Clone();
            var destination = (byte[])source.Clone();
            var rect = new Rectangle(0, 0, width, height);

            fixed (byte* srcPtr = sourceCopy)
            fixed (byte* dstPtr = destination) {
                var src = new UnmanagedImage((System.IntPtr)srcPtr, width, height, width, PixelFormat.Format8bppIndexed);
                var dst = new UnmanagedImage((System.IntPtr)dstPtr, width, height, width, PixelFormat.Format8bppIndexed);
                ProcessFilter(src, dst, rect);
            }

            return destination;
        }
    }
}
