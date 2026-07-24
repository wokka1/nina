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
using NINA.WPF.Base.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp.Processing;

namespace NINA.WPF.Base.SkySurvey {

    public abstract class MosaicSkySurvey : ISkySurvey {
        protected double MaxFoVPerImage = 60;

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            return await Task.Run(async () => {
                if (fieldOfView > MaxFoVPerImage * 3) {
                    throw new Exception(string.Format("Sky Survey only supports up to {0} degree", AstroUtil.ArcminToDegree(MaxFoVPerImage * 3)));
                } else {
                    BitmapSource image = null;

                    try {
                        if (fieldOfView <= MaxFoVPerImage) {
                            image = await GetSingleImage(coordinates, fieldOfView, fieldOfView, ct, width, height);
                        } else {
                            image = await GetMosaicImage(name, coordinates, fieldOfView, width, height, ct, progress);
                        }
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        throw new SkySurveyUnavailableException(ex.Message);
                    }

                    image.Freeze();

                    return new SkySurveyImage() {
                        Name = name,
                        Source = this.GetType().Name,
                        Image = image,
                        FoVHeight = fieldOfView,
                        FoVWidth = fieldOfView,
                        Rotation = 0,
                        Coordinates = coordinates
                    };
                }
            });
        }

        private async Task<BitmapSource> GetMosaicImage(string name, Coordinates coordinates, double fieldOfView,
            int width, int height, CancellationToken ct, IProgress<int> progress) {
            var centerTask = GetSingleImage(coordinates, MaxFoVPerImage, MaxFoVPerImage, ct, width, height);

            var borderFoV = (fieldOfView - MaxFoVPerImage) / 2.0;
            var shiftedDegree = AstroUtil.ArcminToDegree(MaxFoVPerImage) / 2.0 + AstroUtil.ArcminToDegree(borderFoV) / 2.0;

            var newCoordinates = coordinates.Shift(-shiftedDegree, -shiftedDegree, 0);
            var topLeftTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                borderFoV,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(0, -shiftedDegree, 0);
            var topTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                MaxFoVPerImage,
                borderFoV,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(shiftedDegree, -shiftedDegree, 0);
            var topRightTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                borderFoV,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(-shiftedDegree, 0, 0);
            var leftTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                MaxFoVPerImage,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(shiftedDegree, 0, 0);
            var rightTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                MaxFoVPerImage,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(-shiftedDegree, shiftedDegree, 0);
            var bottomLeftTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                borderFoV,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(0, shiftedDegree, 0);
            var bottomTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                MaxFoVPerImage,
                borderFoV,
                ct,
                width,
                height
            );

            newCoordinates = coordinates.Shift(shiftedDegree, shiftedDegree, 0);
            var bottomRightTask = GetSingleImage(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV,
                borderFoV,
                ct,
                width,
                height
            );

            /*
                * * * * * * * * * *
                * 0 |    1    | 2 *
                *___|_________|___*
                *   |         |   *
                * 3 |    4    | 5 *
                *   |         |   *
                *___|_________|___*
                * 6 |    7    | 8 *
                *   |         |   *
                * * * * * * * * * *
             */
            var tmpImages = await Task.WhenAll(topLeftTask, topTask, topRightTask, leftTask, centerTask, rightTask, bottomLeftTask, bottomTask, bottomRightTask);
            BitmapSource[] images = new BitmapSource[tmpImages.Length];
            for (var i = 0; i < tmpImages.Length; i++) {
                double factor = 1;
                if (centerTask.Result.PixelWidth > 640) {
                    factor = 640.0d / centerTask.Result.PixelWidth;
                }
                var scaled = new WriteableBitmap(new TransformedBitmap(tmpImages[i], new ScaleTransform(factor, factor)));
                images[i] = scaled;
            }

            var mosaicWidth = images[0].PixelWidth + images[1].PixelWidth + images[2].PixelWidth;
            var mosaicHeight = images[0].PixelHeight + images[3].PixelHeight + images[6].PixelHeight;

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                drawingContext.DrawImage(images[0], new System.Windows.Rect(0, 0, images[0].PixelWidth, images[0].PixelHeight));
                drawingContext.DrawImage(images[1], new System.Windows.Rect(images[0].PixelWidth, 0, images[1].PixelWidth, images[1].PixelHeight));
                drawingContext.DrawImage(images[2], new System.Windows.Rect(images[0].PixelWidth + images[1].PixelWidth, 0, images[2].PixelWidth, images[2].PixelHeight));

                drawingContext.DrawImage(images[3], new System.Windows.Rect(0, images[0].PixelHeight, images[3].PixelWidth, images[3].PixelHeight));
                drawingContext.DrawImage(images[4], new System.Windows.Rect(images[0].PixelWidth, images[0].PixelHeight, images[4].PixelWidth, images[4].PixelHeight));
                drawingContext.DrawImage(images[5], new System.Windows.Rect(images[0].PixelWidth + images[1].PixelWidth, images[0].PixelHeight, images[5].PixelWidth, images[5].PixelHeight));

                drawingContext.DrawImage(images[6], new System.Windows.Rect(0, images[0].PixelHeight + images[3].PixelHeight, images[6].PixelWidth, images[6].PixelHeight));
                drawingContext.DrawImage(images[7], new System.Windows.Rect(images[0].PixelWidth, images[0].PixelHeight + images[3].PixelHeight, images[7].PixelWidth, images[7].PixelHeight));
                drawingContext.DrawImage(images[8], new System.Windows.Rect(images[0].PixelWidth + images[1].PixelWidth, images[0].PixelHeight + images[3].PixelHeight, images[8].PixelWidth, images[8].PixelHeight));
            }

            // Converts the Visual (DrawingVisual) into a BitmapSource
            RenderTargetBitmap bmp = new RenderTargetBitmap(mosaicWidth, mosaicHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();
            return bmp;
        }

        protected abstract Task<BitmapSource> GetSingleImage(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height);

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetImage - same single-vs-mosaic branching and field-of-view
        /// math, decoded/composited via ImageSharp instead of WPF's TransformedBitmap/DrawingVisual/RenderTargetBitmap
        /// so it also runs on macOS/Linux.
        /// </summary>
        public async Task<SkySurveyImagePortable> GetImagePortable(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            return await Task.Run(async () => {
                if (fieldOfView > MaxFoVPerImage * 3) {
                    throw new Exception(string.Format("Sky Survey only supports up to {0} degree", AstroUtil.ArcminToDegree(MaxFoVPerImage * 3)));
                } else {
                    SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = null;

                    try {
                        if (fieldOfView <= MaxFoVPerImage) {
                            image = await GetSingleImagePortable(coordinates, fieldOfView, fieldOfView, ct, width, height);
                        } else {
                            image = await GetMosaicImagePortable(name, coordinates, fieldOfView, width, height, ct, progress);
                        }
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        throw new SkySurveyUnavailableException(ex.Message);
                    }

                    return new SkySurveyImagePortable() {
                        Name = name,
                        Source = this.GetType().Name,
                        Image = image,
                        FoVHeight = fieldOfView,
                        FoVWidth = fieldOfView,
                        Rotation = 0,
                        Coordinates = coordinates
                    };
                }
            });
        }

        private async Task<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>> GetMosaicImagePortable(string name, Coordinates coordinates, double fieldOfView,
            int width, int height, CancellationToken ct, IProgress<int> progress) {
            var centerTask = GetSingleImagePortable(coordinates, MaxFoVPerImage, MaxFoVPerImage, ct, width, height);

            var borderFoV = (fieldOfView - MaxFoVPerImage) / 2.0;
            var shiftedDegree = AstroUtil.ArcminToDegree(MaxFoVPerImage) / 2.0 + AstroUtil.ArcminToDegree(borderFoV) / 2.0;

            var newCoordinates = coordinates.Shift(-shiftedDegree, -shiftedDegree, 0);
            var topLeftTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, borderFoV, ct, width, height);

            newCoordinates = coordinates.Shift(0, -shiftedDegree, 0);
            var topTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                MaxFoVPerImage, borderFoV, ct, width, height);

            newCoordinates = coordinates.Shift(shiftedDegree, -shiftedDegree, 0);
            var topRightTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, borderFoV, ct, width, height);

            newCoordinates = coordinates.Shift(-shiftedDegree, 0, 0);
            var leftTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, MaxFoVPerImage, ct, width, height);

            newCoordinates = coordinates.Shift(shiftedDegree, 0, 0);
            var rightTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, MaxFoVPerImage, ct, width, height);

            newCoordinates = coordinates.Shift(-shiftedDegree, shiftedDegree, 0);
            var bottomLeftTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, borderFoV, ct, width, height);

            newCoordinates = coordinates.Shift(0, shiftedDegree, 0);
            var bottomTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                MaxFoVPerImage, borderFoV, ct, width, height);

            newCoordinates = coordinates.Shift(shiftedDegree, shiftedDegree, 0);
            var bottomRightTask = GetSingleImagePortable(
                new Coordinates(newCoordinates.RADegrees, newCoordinates.Dec, Epoch.J2000, Coordinates.RAType.Degrees),
                borderFoV, borderFoV, ct, width, height);

            /*
                * * * * * * * * * *
                * 0 |    1    | 2 *
                *___|_________|___*
                *   |         |   *
                * 3 |    4    | 5 *
                *   |         |   *
                *___|_________|___*
                * 6 |    7    | 8 *
                *   |         |   *
                * * * * * * * * * *
             */
            var tmpImages = await Task.WhenAll(topLeftTask, topTask, topRightTask, leftTask, centerTask, rightTask, bottomLeftTask, bottomTask, bottomRightTask);

            double factor = 1;
            if (centerTask.Result.Width > 640) {
                factor = 640.0d / centerTask.Result.Width;
            }

            var images = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>[tmpImages.Length];
            for (var i = 0; i < tmpImages.Length; i++) {
                var w = Math.Max(1, (int)(tmpImages[i].Width * factor));
                var h = Math.Max(1, (int)(tmpImages[i].Height * factor));
                images[i] = tmpImages[i].Clone(x => x.Resize(w, h));
            }
            foreach (var tmp in tmpImages) {
                tmp.Dispose();
            }

            var mosaicWidth = images[0].Width + images[1].Width + images[2].Width;
            var mosaicHeight = images[0].Height + images[3].Height + images[6].Height;

            var mosaic = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(mosaicWidth, mosaicHeight);
            mosaic.Mutate(ctx => {
                ctx.DrawImage(images[0], new SixLabors.ImageSharp.Point(0, 0), 1f);
                ctx.DrawImage(images[1], new SixLabors.ImageSharp.Point(images[0].Width, 0), 1f);
                ctx.DrawImage(images[2], new SixLabors.ImageSharp.Point(images[0].Width + images[1].Width, 0), 1f);

                ctx.DrawImage(images[3], new SixLabors.ImageSharp.Point(0, images[0].Height), 1f);
                ctx.DrawImage(images[4], new SixLabors.ImageSharp.Point(images[0].Width, images[0].Height), 1f);
                ctx.DrawImage(images[5], new SixLabors.ImageSharp.Point(images[0].Width + images[1].Width, images[0].Height), 1f);

                ctx.DrawImage(images[6], new SixLabors.ImageSharp.Point(0, images[0].Height + images[3].Height), 1f);
                ctx.DrawImage(images[7], new SixLabors.ImageSharp.Point(images[0].Width, images[0].Height + images[3].Height), 1f);
                ctx.DrawImage(images[8], new SixLabors.ImageSharp.Point(images[0].Width + images[1].Width, images[0].Height + images[3].Height), 1f);
            });

            foreach (var img in images) {
                img.Dispose();
            }

            return mosaic;
        }

        protected abstract Task<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>> GetSingleImagePortable(Coordinates coordinates, double fovW, double fovH, CancellationToken ct, int width, int height);
    }
}