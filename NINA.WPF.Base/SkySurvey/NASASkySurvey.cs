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
using Accord.Statistics.Visualizations;
using NINA.Image.ImageData;
using NINA.Core.Utility.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.WPF.Base.Exceptions;
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    internal class NASASkySurvey : ISkySurvey {
        private const string Url = "https://skyview.gsfc.nasa.gov/current/cgi/runquery.pl?Survey=dss2r&Position={0},{1}&Size={2}&Pixels={3}&Return=JPG";

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 2;
            fieldOfView = Math.Round(fieldOfView, 2);
            var pixels = Math.Ceiling(Math.Min(AstroUtil.ArcminToArcsec(fieldOfView) / arcSecPerPixel, 5000));

            BitmapSource image;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    AstroUtil.ArcminToDegree(fieldOfView),
                    pixels
                );

                var bytes = await request.Request(ct, progress);
                image = NINA.Image.ImageAnalysis.ImageUtility.FromEncodedBytes(bytes);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            image.Freeze();

            using (var bmp = Image.ImageAnalysis.ImageUtility.BitmapFromSource(image, System.Drawing.Imaging.PixelFormat.Format8bppIndexed)) {
                bmp.Palette = Image.ImageAnalysis.ImageUtility.GetGrayScalePalette();
                Accord.Imaging.ImageStatistics stats = new Accord.Imaging.ImageStatistics(bmp);
                Histogram gray = stats.GrayWithoutBlack;
                new Accord.Imaging.Filters.BrightnessCorrection(Math.Min(115 - gray.Median, 0)).ApplyInPlace(bmp);
                new Accord.Imaging.Filters.ContrastCorrection((int)Math.Round(115 - gray.StdDev * 2)).ApplyInPlace(bmp);
                image = Image.ImageAnalysis.ImageUtility.ConvertBitmap(bmp, System.Windows.Media.PixelFormats.Gray8);
                image.Freeze();
            }

            return new SkySurveyImage() {
                Image = image,
                Name = name,
                Source = nameof(NASASkySurvey),
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetImage - downloads and decodes the same way, then applies
        /// an approximate equivalent of the original's Accord.Imaging-based brightness/contrast correction
        /// (median-centered brightness shift + stddev-based contrast scaling around the midpoint gray value) -
        /// a documented approximation, not a byte-for-byte port of Accord.Imaging's own internal formulas.
        /// </summary>
        public async Task<SkySurveyImagePortable> GetImagePortable(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 2;
            fieldOfView = Math.Round(fieldOfView, 2);
            var pixels = Math.Ceiling(Math.Min(AstroUtil.ArcminToArcsec(fieldOfView) / arcSecPerPixel, 5000));

            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L8> gray;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    AstroUtil.ArcminToDegree(fieldOfView),
                    pixels
                );

                var bytes = await request.Request(ct, progress);
                using var decoded = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.L8>(bytes);
                gray = decoded.Clone();
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            ApplyBrightnessContrastPortable(gray);

            var rgba = gray.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgba32>();
            gray.Dispose();

            return new SkySurveyImagePortable() {
                Image = rgba,
                Name = name,
                Source = nameof(NASASkySurvey),
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }

        private static void ApplyBrightnessContrastPortable(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L8> gray) {
            long sum = 0;
            long sumSq = 0;
            long count = 0;
            var histogram = new int[256];

            gray.ProcessPixelRows(accessor => {
                for (int y = 0; y < accessor.Height; y++) {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++) {
                        var v = row[x].PackedValue;
                        if (v == 0) {
                            continue;
                        }
                        histogram[v]++;
                        sum += v;
                        sumSq += (long)v * v;
                        count++;
                    }
                }
            });

            if (count == 0) {
                return;
            }

            double mean = (double)sum / count;
            double variance = ((double)sumSq / count) - (mean * mean);
            double stdDev = variance > 0 ? Math.Sqrt(variance) : 0;

            long half = count / 2;
            long running = 0;
            int median = 0;
            for (int i = 1; i < 256; i++) {
                running += histogram[i];
                if (running >= half) {
                    median = i;
                    break;
                }
            }

            var brightnessAdjustment = Math.Min(115 - median, 0);
            var contrastFactor = Math.Max(0.1, (115 - (stdDev * 2)) / 115.0);

            gray.ProcessPixelRows(accessor => {
                for (int y = 0; y < accessor.Height; y++) {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++) {
                        double v = row[x].PackedValue;
                        v = ((v - 127.5) * contrastFactor) + 127.5 + brightnessAdjustment;
                        row[x] = new SixLabors.ImageSharp.PixelFormats.L8((byte)Math.Clamp(v, 0, 255));
                    }
                }
            });
        }
    }
}