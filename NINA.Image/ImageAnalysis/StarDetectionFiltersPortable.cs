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
using System.Drawing;
using System.Drawing.Imaging;

namespace NINA.Image.ImageAnalysis {

    /// <summary>
    /// Portable, Bitmap/GDI+-free helpers for the star-detection pipeline's
    /// Accord.Imaging filter chain. Unlike GrayscalePortable/BayerFilter16bpp,
    /// SISThreshold needs no subclass at all - SISThreshold.CalculateThreshold()
    /// is already public static, and Threshold.ApplyInPlace(UnmanagedImage,
    /// Rectangle) is already public on the base filter class - both callable
    /// directly against a plain UnmanagedImage wrapping a pinned managed array,
    /// with no Bitmap/GDI+ involved.
    /// </summary>
    public static class StarDetectionFiltersPortable {

        /// <summary>
        /// Adaptive (Simple Image Statistics) binary threshold for an 8bpp
        /// grayscale array - same two-call sequence (CalculateThreshold then
        /// Threshold.ApplyInPlace) as SISThreshold.ProcessFilter() itself.
        /// Returns a new array; the input is untouched.
        /// </summary>
        public static unsafe byte[] SISThresholdArray(byte[] source, int width, int height) {
            var result = (byte[])source.Clone();
            var rect = new Rectangle(0, 0, width, height);

            fixed (byte* ptr = result) {
                var unmanagedImage = new UnmanagedImage((System.IntPtr)ptr, width, height, width, PixelFormat.Format8bppIndexed);
                int threshold = SISThreshold.CalculateThreshold(unmanagedImage, rect);
                new Threshold(threshold).ApplyInPlace(unmanagedImage, rect);
            }

            return result;
        }
    }
}
