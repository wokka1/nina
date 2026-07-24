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
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    public class ESOSkySurvey : MosaicSkySurvey, ISkySurvey {

        public ESOSkySurvey() {
            MaxFoVPerImage = 120;
        }

        private const string Url = "http://archive.eso.org/dss/dss/image?ra={0}&dec={1}&x={2}&y={3}&mime-type=download-gif&Sky-Survey=DSS2&equinox=J2000&statsmode=VO";

        protected override async Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height) {
            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    fovW,
                    fovH
                );

                var bytes = await request.Request(ct);
                return NINA.Image.ImageAnalysis.ImageUtility.FromEncodedBytes(bytes);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }
        }

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetSingleImage - decoded via ImageSharp instead of WPF.
        /// </summary>
        protected override async Task<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>> GetSingleImagePortable(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height) {
            try {
                var request = new HttpDownloadImageRequest(
                    Url,
                    coordinates.RADegrees,
                    coordinates.Dec,
                    fovW,
                    fovH
                );

                var bytes = await request.Request(ct);
                return SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new SkySurveyUnavailableException(ex.Message);
            }
        }
    }
}