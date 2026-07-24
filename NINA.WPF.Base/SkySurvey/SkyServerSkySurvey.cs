#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility.Http;
using NINA.WPF.Base.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
#if HAS_WPF
using System.Windows.Media.Imaging;
#endif

namespace NINA.WPF.Base.SkySurvey {

    internal class SkyServerSkySurvey : ISkySurvey {
        private const string Url = "http://skyserver.sdss.org/dr14/SkyserverWS/ImgCutout/getjpeg?ra={0}&dec={1}&width={2}&height={3}&scale={4}";

#if HAS_WPF
        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 0.4;
            var targetFoVInArcSec = AstroUtil.ArcminToArcsec(fieldOfView);
            var pixels = Math.Min(targetFoVInArcSec / arcSecPerPixel, 2048);
            if (pixels == 2048) {
                arcSecPerPixel = targetFoVInArcSec / 2048;
            }

            BitmapSource image;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    pixels,
                    pixels,
                    arcSecPerPixel);

                var bytes = await request.Request(ct, progress);
                image = NINA.Image.ImageAnalysis.ImageUtility.FromEncodedBytes(bytes);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            image.Freeze();
            return new SkySurveyImage() {
                Name = name,
                Source = nameof(SkyServerSkySurvey),
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
#endif

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetImage - same query math, decoded via ImageSharp
        /// instead of WPF so it also runs on macOS/Linux.
        /// </summary>
        public async Task<SkySurveyImagePortable> GetImagePortable(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            var arcSecPerPixel = 0.4;
            var targetFoVInArcSec = AstroUtil.ArcminToArcsec(fieldOfView);
            var pixels = Math.Min(targetFoVInArcSec / arcSecPerPixel, 2048);
            if (pixels == 2048) {
                arcSecPerPixel = targetFoVInArcSec / 2048;
            }

            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image;

            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    pixels,
                    pixels,
                    arcSecPerPixel);

                var bytes = await request.Request(ct, progress);
                image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }

            return new SkySurveyImagePortable() {
                Name = name,
                Source = nameof(SkyServerSkySurvey),
                Image = image,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}