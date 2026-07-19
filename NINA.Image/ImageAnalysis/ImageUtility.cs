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
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageAnalysis {

    public class ImageUtility {

        public static ColorRemappingGeneral GetColorRemappingFilter(
            IImageStatistics statistics,
            double targetHistogramMeanPct,
            double shadowsClipping,
            System.Windows.Media.PixelFormat pf) {
            ushort[] map = GetStretchMap(statistics, targetHistogramMeanPct, shadowsClipping);

            if (pf == PixelFormats.Gray16) {
                var filter = new ColorRemappingGeneral(map);
                return filter;
            } else if (pf == PixelFormats.Rgb48) {
                var filter = new ColorRemappingGeneral(map, map, map);
                return filter;
            } else {
                throw new NotSupportedException();
            }
        }

        public static ColorRemappingGeneral GetColorRemappingFilterUnlinked(
            IImageStatistics redStatistics,
            IImageStatistics greenStatistics,
            IImageStatistics blueStatistics,
            double targetHistogramMeanPct,
            double shadowsClipping,
            System.Windows.Media.PixelFormat pf) {
            ushort[] mapRed = GetStretchMap(redStatistics, targetHistogramMeanPct, shadowsClipping);
            ushort[] mapGreen = GetStretchMap(greenStatistics, targetHistogramMeanPct, shadowsClipping);
            ushort[] mapBlue = GetStretchMap(blueStatistics, targetHistogramMeanPct, shadowsClipping);
            if (pf == PixelFormats.Rgb48) {
                var filter = new ColorRemappingGeneral(mapRed, mapGreen, mapBlue);
                return filter;
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Adjusts x for a given midToneBalance
        /// </summary>
        /// <param name="midToneBalance"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static double MidtonesTransferFunction(double midToneBalance, double x) {
            if (x > 0) {
                if (x < 1) {
                    return (midToneBalance - 1) * x / ((2 * midToneBalance - 1) * x - midToneBalance);
                }
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Converts a value from range [0;65535] to [0;1]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static double NormalizeUShort(double val, int bitDepth) {
            return val / (double)((1 << bitDepth) - 1);
        }

        /// <summary>
        /// Converts a value from range [0;1] to [0;65535]
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ushort DenormalizeUShort(double val) {
            return (ushort)(val * ushort.MaxValue + (val < 0.5 ? 0.5 : 0.0));
        }

        private static ushort[] GetStretchMap(IImageStatistics statistics, double targetHistogramMedianPercent, double shadowsClipping) {
            ushort[] map = new ushort[ushort.MaxValue + 1];

            var normalizedMedian = NormalizeUShort(statistics.Median, statistics.BitDepth);
            var normalizedMAD = NormalizeUShort(statistics.MedianAbsoluteDeviation, statistics.BitDepth);

            var scaleFactor = 1.4826; // see https://en.wikipedia.org/wiki/Median_absolute_deviation

            double shadows = 0d;
            double midtones = 0.5d;
            double highlights = 1d;

            //Assume the image is inverted or overexposed when median is higher than half of the possible value
            if (normalizedMedian > 0.5) {
                shadows = 0.0d;
                highlights = normalizedMedian - shadowsClipping * normalizedMAD * scaleFactor;
                midtones = MidtonesTransferFunction(targetHistogramMedianPercent, 1.0 - (highlights - normalizedMedian));
            } else {
                shadows = normalizedMedian + shadowsClipping * normalizedMAD * scaleFactor;
                midtones = MidtonesTransferFunction(targetHistogramMedianPercent, normalizedMedian - shadows);
                highlights = 1;
            }

            for (int i = 0; i < map.Length; i++) {
                double value = NormalizeUShort(i, statistics.BitDepth);

                map[i] = DenormalizeUShort(MidtonesTransferFunction(midtones, 1 - highlights + value - shadows));
            }

            return map;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap) {
            System.Windows.Media.PixelFormat pf;

            switch (bitmap.PixelFormat) {
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    pf = System.Windows.Media.PixelFormats.Bgr565;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    pf = System.Windows.Media.PixelFormats.Bgra32;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    pf = System.Windows.Media.PixelFormats.Bgra32;
                    break;

                default:
                    pf = System.Windows.Media.PixelFormats.Gray16;
                    break;
            }
            return ConvertBitmap(bitmap, pf);
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap, System.Windows.Media.PixelFormat pf) {
            BitmapData bitmapData = null;
            try {
                bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

                return BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height, 96, 96, pf, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            } finally {
                if (bitmapData != null) {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        /// Decodes an in-memory encoded image (e.g. a downloaded JPEG/PNG/GIF) into a frozen,
        /// UI-thread-independent BitmapSource. Used to keep image-format decoding out of
        /// NINA.Core, which does not reference WPF.
        /// </summary>
        public static BitmapSource FromEncodedBytes(byte[] data) {
            using var ms = new System.IO.MemoryStream(data);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static Bitmap BitmapFromSource(BitmapSource source) {
            return BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
        }

        public static Bitmap BitmapFromSource(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            Bitmap bmp = new Bitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    pf);
            BitmapData data = null;
            try {
                data = bmp.LockBits(
                        new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                        ImageLockMode.WriteOnly,
                        pf);
                source.CopyPixels(
                        Int32Rect.Empty,
                        data.Scan0,
                        data.Height * data.Stride,
                        data.Stride);
            } finally {
                if (data != null) {
                    bmp.UnlockBits(data);
                }
            }
            return bmp;
        }

        public static Bitmap Convert16BppTo8Bpp(BitmapSource source) {
            using(MyStopWatch.Measure()) { 
                using (var bmp = BitmapFromSource(source)) {
                    return Accord.Imaging.Image.Convert16bppTo8bpp(bmp);
                }
            }
        }

        /// <summary>
        /// Portable, Bitmap/GDI+-free equivalent of Convert16BppTo8Bpp - same per-pixel
        /// ">> 8" bit-depth reduction as Accord.Imaging.Image.Convert16bppTo8bpp, applied
        /// directly to a raw Gray16 array instead of a Bitmap.
        /// </summary>
        public static byte[] Convert16BppTo8BppArray(ushort[] source) {
            using (MyStopWatch.Measure()) {
                var destination = new byte[source.Length];
                for (int i = 0; i < source.Length; i++) {
                    destination[i] = (byte)(source[i] >> 8);
                }
                return destination;
            }
        }

        public static BitmapSource Convert16BppTo8BppSource(BitmapSource source) {
            FormatConvertedBitmap s = new FormatConvertedBitmap();
            s.BeginInit();
            s.Source = source;
            s.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            s.EndInit();
            s.Freeze();
            return s;
        }

        public static BitmapSource CreateSourceFromArray(IImageArray arr, ImageProperties props, System.Windows.Media.PixelFormat pf) {
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (props.Width * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(props.Width, props.Height, dpi, dpi, pf, null, arr.FlatArray, stride);
            source.Freeze();
            return source;
        }

        public static DebayeredImageData Debayer(BitmapSource source, System.Drawing.Imaging.PixelFormat pf, bool saveColorChannels = false, bool saveLumChannel = false, SensorType bayerPattern = SensorType.RGGB) {
            using (MyStopWatch.Measure()) {
                if (pf != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                    throw new NotSupportedException();
                }
                using (var bmp = BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)) {
                    return Debayer(bmp, saveColorChannels, saveLumChannel, bayerPattern);
                }
            }
        }

        /// <summary>
        /// Shared by both Debayer(Bitmap...) below and the portable DebayerArray() -
        /// extracted so the pattern-selection logic isn't duplicated between the
        /// two entry points (this used to be inline in Debayer(Bitmap...) only).
        /// </summary>
        private static int[,] GetBayerPatternArray(SensorType bayerPattern) {
            switch (bayerPattern) {
                case SensorType.RGGB:
                    return new int[,] { { RGB.B, RGB.G }, { RGB.G, RGB.R } };

                case SensorType.RGBG:
                    return new int[,] { { RGB.G, RGB.B }, { RGB.G, RGB.R } };

                case SensorType.GRGB:
                    return new int[,] { { RGB.B, RGB.G }, { RGB.R, RGB.G } };

                case SensorType.GRBG:
                    return new int[,] { { RGB.G, RGB.B }, { RGB.R, RGB.G } };

                case SensorType.GBGR:
                    return new int[,] { { RGB.R, RGB.G }, { RGB.B, RGB.G } };

                case SensorType.GBRG:
                    return new int[,] { { RGB.G, RGB.R }, { RGB.B, RGB.G } };

                case SensorType.BGRG:
                    return new int[,] { { RGB.G, RGB.R }, { RGB.G, RGB.B } };

                case SensorType.BGGR:
                    return new int[,] { { RGB.R, RGB.G }, { RGB.G, RGB.B } };

                default:
                    throw new InvalidImagePropertiesException(string.Format(Loc.Instance["LblUnsupportedCfaPattern"], bayerPattern));
            }
        }

        /// <summary>
        /// Portable, Bitmap/GDI+-free debayer for a raw single-channel pixel array -
        /// same BayerFilter16bpp.DemosaicArray() logic (commit d59d86516) as the
        /// Bitmap-based Debayer() overloads below, sharing the same pattern
        /// selection via GetBayerPatternArray(). Returns the interleaved Rgb48
        /// buffer plus the same optional LRGBArrays side-output as the Bitmap path.
        /// </summary>
        public static (PortableImageBuffer buffer, LRGBArrays lrgb) DebayerArray(
            ushort[] source,
            int width,
            int height,
            bool saveColorChannels = false,
            bool saveLumChannel = false,
            SensorType bayerPattern = SensorType.RGGB) {
            using (MyStopWatch.Measure()) {
                var filter = new BayerFilter16bpp();
                filter.SaveColorChannels = saveColorChannels;
                filter.SaveLumChannel = saveLumChannel;
                filter.BayerPattern = GetBayerPatternArray(bayerPattern);

                var demosaiced = filter.DemosaicArray(source, width, height);
                var buffer = new PortableImageBuffer(demosaiced, width, height, PortablePixelFormat.Rgb48);
                return (buffer, filter.LRGBArrays);
            }
        }

        public static DebayeredImageData Debayer(Bitmap bmp, bool saveColorChannels = false, bool saveLumChannel = false, SensorType bayerPattern = SensorType.RGGB) {
            using (MyStopWatch.Measure()) {
                var filter = new BayerFilter16bpp();
                filter.SaveColorChannels = saveColorChannels;
                filter.SaveLumChannel = saveLumChannel;

                Logger.Debug($"Debayering pattern {bayerPattern}");

                filter.BayerPattern = GetBayerPatternArray(bayerPattern);

                DebayeredImageData debayered = new DebayeredImageData();
                using (var debayeredBitmap = filter.Apply(bmp)) {
                    debayered.ImageSource = ConvertBitmap(debayeredBitmap, PixelFormats.Rgb48);
                    debayered.ImageSource.Freeze();
                }
                debayered.Data = filter.LRGBArrays;
                return debayered;
            }
        }

        public static ColorPalette GetGrayScalePalette() {
            using (var bmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                ColorPalette monoPalette = bmp.Palette;

                System.Drawing.Color[] entries = monoPalette.Entries;

                for (int i = 0; i < 256; i++) {
                    entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }

                return monoPalette;
            }
        }

        public static Task<BitmapSource> Stretch(IRenderedImage image, double factor, double blackClipping) {
            return Task.Run(async () => {
                var imageStatistics = await image.RawImageData.Statistics.Task;
                if (image.OriginalImage.Format == PixelFormats.Gray16) {
                    using (var bmp = ImageUtility.BitmapFromSource(image.OriginalImage, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)) {
                        return Stretch(imageStatistics, bmp, image.OriginalImage.Format, factor, blackClipping);
                    }
                } else if (image.OriginalImage.Format == PixelFormats.Rgb48) {
                    using (var bmp = ImageUtility.BitmapFromSource(image.OriginalImage, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                        return Stretch(imageStatistics, bmp, image.OriginalImage.Format, factor, blackClipping);
                    }
                } else {
                    throw new NotSupportedException();
                }
            });
        }

        public static Task<BitmapSource> StretchUnlinked(IDebayeredImage data, double factor, double blackClipping) {
            return Task.Run(async () => {
                if (data.OriginalImage.Format != PixelFormats.Rgb48) {
                    throw new NotSupportedException();
                } else {
                    var asyncR = Task.Run(() => ImageData.ImageStatistics.Create(data.RawImageData.Properties, data.DebayeredData.Red));
                    var asyncG = Task.Run(() => ImageData.ImageStatistics.Create(data.RawImageData.Properties, data.DebayeredData.Green));
                    var asyncB = Task.Run(() => ImageData.ImageStatistics.Create(data.RawImageData.Properties, data.DebayeredData.Blue));
                    await Task.WhenAll(asyncR, asyncG, asyncB);
                    using (var img = ImageUtility.BitmapFromSource(data.OriginalImage, System.Drawing.Imaging.PixelFormat.Format48bppRgb)) {
                        return StretchUnlinked(asyncR.Result, asyncG.Result, asyncB.Result, img, data.OriginalImage.Format, factor, blackClipping);
                    }
                }
            });
        }

        public static BitmapSource StretchUnlinked(
            IImageStatistics redStatistics,
            IImageStatistics greenStatistics,
            IImageStatistics blueStatistics,
            Bitmap img,
            System.Windows.Media.PixelFormat pf,
            double factor,
            double blackClipping) {
            using (MyStopWatch.Measure()) {
                // Swap Red & Blue statistics due to differences in 48-bit Bitmap (RGB) & BitmapSource (BGR).
                var filter = ImageUtility.GetColorRemappingFilterUnlinked(blueStatistics, greenStatistics, redStatistics, factor, blackClipping, pf);
                filter.ApplyInPlace(img);

                var source = ImageUtility.ConvertBitmap(img, pf);
                source.Freeze();
                return source;
            }
        }

        public static BitmapSource Stretch(IImageStatistics statistics, Bitmap img, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var filter = ImageUtility.GetColorRemappingFilter(statistics, factor, blackClipping, pf);
                filter.ApplyInPlace(img);

                var source = ImageUtility.ConvertBitmap(img, pf);
                source.Freeze();
                return source;
            }
        }

        /// <summary>
        /// Portable, Bitmap/GDI+-free stretch for a single-channel (grayscale)
        /// raw pixel array - same GetStretchMap() lookup-table math as the
        /// Bitmap-based Stretch() overloads above, applied directly via
        /// ColorRemappingGeneral.ApplyToArray() instead of going through a
        /// System.Drawing.Bitmap. Returns a new array; the input is untouched.
        /// </summary>
        public static ushort[] StretchArray(IImageStatistics statistics, ushort[] data, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var map = GetStretchMap(statistics, factor, blackClipping);
                var result = (ushort[])data.Clone();
                var filter = new ColorRemappingGeneral(map);
                filter.ApplyToArray(result, isGrayscale: true);
                return result;
            }
        }

        /// <summary>
        /// Portable, Bitmap/GDI+-free unlinked (per-channel) stretch for an
        /// RGB triplet array. Mirrors StretchUnlinked(Bitmap...)'s red/blue
        /// statistics swap - not a fresh design choice, but kept for
        /// consistency with the existing R=2/G=1/B=0 (RGB.R/G/B) in-memory
        /// channel convention that BayerFilter16bpp.DemosaicArray()'s output
        /// already uses (a legacy 48bpp-Bitmap-derived layout, not something
        /// introduced here - see the original StretchUnlinked(Bitmap) comment).
        /// Returns a new array; the input is untouched.
        /// </summary>
        public static ushort[] StretchUnlinkedArray(
            IImageStatistics redStatistics,
            IImageStatistics greenStatistics,
            IImageStatistics blueStatistics,
            ushort[] rgbData,
            double factor,
            double blackClipping) {
            using (MyStopWatch.Measure()) {
                var mapRed = GetStretchMap(redStatistics, factor, blackClipping);
                var mapGreen = GetStretchMap(greenStatistics, factor, blackClipping);
                var mapBlue = GetStretchMap(blueStatistics, factor, blackClipping);
                var result = (ushort[])rgbData.Clone();
                var filter = new ColorRemappingGeneral(mapBlue, mapGreen, mapRed);
                filter.ApplyToArray(result, isGrayscale: false);
                return result;
            }
        }

        /// <summary>
        /// Portable, Bitmap/GDI+-free "linked" stretch for an interleaved Rgb48
        /// array - the same map applied to all 3 channels, matching what
        /// GetColorRemappingFilter(..., PixelFormats.Rgb48) does in the
        /// Bitmap-based Stretch(IImageStatistics, Bitmap, ...) path when a
        /// single shared statistic (not per-channel) drives the stretch.
        /// Returns a new array; the input is untouched.
        /// </summary>
        public static ushort[] StretchLinkedRgbArray(IImageStatistics statistics, ushort[] rgbData, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var map = GetStretchMap(statistics, factor, blackClipping);
                var result = (ushort[])rgbData.Clone();
                var filter = new ColorRemappingGeneral(map, map, map);
                filter.ApplyToArray(result, isGrayscale: false);
                return result;
            }
        }

        public static void BitShiftLeftInPlace(ushort[] data, int shift) {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (shift <= 0)
                return;

            // For SIMD we multiply by 2^shift, since Vector<T> has no shift ops
            ushort factor = (ushort)(1 << shift);
            var factorVec = new System.Numerics.Vector<ushort>(factor);

            int vectorSize = System.Numerics.Vector<ushort>.Count;
            int i = 0;
            int length = data.Length;

            // SIMD loop
            for (; i <= length - vectorSize; i += vectorSize) {
                var v = new System.Numerics.Vector<ushort>(data, i);
                v = System.Numerics.Vector.Multiply(v, factorVec);
                v.CopyTo(data, i);
            }

            // Tail
            for (; i < length; i++) {
                data[i] = (ushort)(data[i] << shift);
            }
        }
    }
}