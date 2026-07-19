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
using System.Drawing.Imaging;

namespace NINA.Image.ImageAnalysis {

    /// <summary>
    /// Portable, Bitmap/GDI+-free entry point for Accord.Imaging's ResizeBicubic filter,
    /// used to downscale the 8bpp grayscale detection buffer before structure detection.
    /// Unlike the in-place filters, resizing produces a new, differently-sized buffer -
    /// this mirrors Apply(BitmapData)'s exact semantics (a freshly allocated destination,
    /// fully populated by ProcessFilter), just skipping the Bitmap/BitmapData layer.
    /// </summary>
    public class ResizeBicubicPortable : ResizeBicubic {

        public ResizeBicubicPortable(int newWidth, int newHeight) : base(newWidth, newHeight) {
        }

        public unsafe byte[] ApplyToGray8Array(byte[] source, int width, int height, int newWidth, int newHeight) {
            var destination = new byte[newWidth * newHeight];

            fixed (byte* srcPtr = source)
            fixed (byte* dstPtr = destination) {
                var src = new UnmanagedImage((System.IntPtr)srcPtr, width, height, width, PixelFormat.Format8bppIndexed);
                var dst = new UnmanagedImage((System.IntPtr)dstPtr, newWidth, newHeight, newWidth, PixelFormat.Format8bppIndexed);
                ProcessFilter(src, dst);
            }

            return destination;
        }
    }
}
