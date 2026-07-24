using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NINA.Image.ImageData;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// Converts NINA.Image.ImageData.PortableImageBuffer (the WPF/System.Drawing-free
    /// counterpart to BitmapSource built earlier this project, see IRenderedImage.RawPixels)
    /// into a real, displayable Avalonia bitmap. This is the missing piece the roadmap called
    /// out for Phase 2 - all the portable pixel math already existed, nothing could actually
    /// render it on screen until this.
    ///
    /// Downshifts each 16-bit channel to 8 bits (ushort >> 8) - PortableImageBuffer's data is
    /// expected to already be stretch-processed (via IRenderedImage.Stretch(), which is what
    /// actually makes faint linear sensor data visible at all) before reaching here, same as
    /// the real WPF app only ever displays the stretched image, never raw linear data.
    /// </summary>
    public static class PortableImageBufferConverter {

        public static WriteableBitmap ToWriteableBitmap(PortableImageBuffer buffer) {
            var bitmap = new WriteableBitmap(
                new PixelSize(buffer.Width, buffer.Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);

            using var lockedBuffer = bitmap.Lock();
            var pixelCount = buffer.Width * buffer.Height;
            var bgra = new byte[pixelCount * 4];

            if (buffer.Format == PortablePixelFormat.Rgb48) {
                // [R,G,B] ushort triplets per pixel (R=index 2, G=index 1, B=index 0 - the
                // in-memory convention BayerFilter16bpp.DemosaicArray() established, kept
                // consistent throughout the portable pipeline, see PortableImageBuffer's own
                // doc comment).
                for (int i = 0; i < pixelCount; i++) {
                    byte b = (byte)(buffer.Data[i * 3 + 0] >> 8);
                    byte g = (byte)(buffer.Data[i * 3 + 1] >> 8);
                    byte r = (byte)(buffer.Data[i * 3 + 2] >> 8);
                    int o = i * 4;
                    bgra[o + 0] = b;
                    bgra[o + 1] = g;
                    bgra[o + 2] = r;
                    bgra[o + 3] = 255;
                }
            } else {
                // Gray16 - one ushort per pixel, replicated across B/G/R.
                for (int i = 0; i < pixelCount; i++) {
                    byte v = (byte)(buffer.Data[i] >> 8);
                    int o = i * 4;
                    bgra[o + 0] = v;
                    bgra[o + 1] = v;
                    bgra[o + 2] = v;
                    bgra[o + 3] = 255;
                }
            }

            Marshal.Copy(bgra, 0, lockedBuffer.Address, bgra.Length);
            return bitmap;
        }

        /// <summary>
        /// Converts raw RGBA32 bytes (R,G,B,A per pixel - SixLabors.ImageSharp.PixelFormats.Rgba32's own
        /// in-memory layout, as produced by SkyMapAnnotator.RenderPortable()/CacheSkySurveyImageFactory.
        /// RenderPortable() via Image&lt;Rgba32&gt;.CopyPixelDataTo) into a displayable Avalonia bitmap.
        /// Unlike ToWriteableBitmap(PortableImageBuffer) above, there's no 16-bit downshift here - this data
        /// is already 8-bit-per-channel - just a channel-order swap (RGBA -> Avalonia's BGRA8888).
        /// </summary>
        public static WriteableBitmap ToWriteableBitmapFromRgba32(byte[] rgba, int width, int height) {
            var bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var lockedBuffer = bitmap.Lock();
            var pixelCount = width * height;
            var bgra = new byte[pixelCount * 4];

            for (int i = 0; i < pixelCount; i++) {
                int o = i * 4;
                byte r = rgba[o + 0];
                byte g = rgba[o + 1];
                byte b = rgba[o + 2];
                byte a = rgba[o + 3];
                bgra[o + 0] = b;
                bgra[o + 1] = g;
                bgra[o + 2] = r;
                bgra[o + 3] = a;
            }

            Marshal.Copy(bgra, 0, lockedBuffer.Address, bgra.Length);
            return bitmap;
        }
    }
}
