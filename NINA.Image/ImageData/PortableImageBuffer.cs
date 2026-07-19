#region "copyright"

/*
    Copyright 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.Image.ImageData {

    public enum PortablePixelFormat {
        Gray16,
        Rgb48
    }

    /// <summary>
    /// A raw pixel buffer with no WPF/System.Drawing dependency - the portable
    /// counterpart to BitmapSource for IRenderedImage.RawPixels. For Rgb48,
    /// Data is laid out as [R,G,B] triplets per pixel using the same R=2/G=1/B=0
    /// in-memory index convention as BayerFilter16bpp.DemosaicArray()'s output
    /// (see that method's comment) - not a fresh convention, kept for
    /// consistency with the rest of the portable pixel-math pipeline.
    /// </summary>
    public class PortableImageBuffer {
        public ushort[] Data { get; }
        public int Width { get; }
        public int Height { get; }
        public PortablePixelFormat Format { get; }

        public PortableImageBuffer(ushort[] data, int width, int height, PortablePixelFormat format) {
            Data = data;
            Width = width;
            Height = height;
            Format = format;
        }
    }
}
