#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using NINA.Image.ImageData;
using NINA.Astrometry;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if HAS_WPF
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NINA.WPF.Base.SkySurvey {

    public class FileSkySurvey : ISkySurvey {
        private readonly IImageDataFactory imageDataFactory;

        public FileSkySurvey(IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
        }

#if HAS_WPF
        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width,
            int height, CancellationToken ct, IProgress<int> progress) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Loc.Instance["LblLoadImage"];
            dialog.FileName = "";
            dialog.DefaultExt = ".tif";
            dialog.Multiselect = false;
            dialog.Filter = "Image files|*.tif;*.tiff;*.jpeg;*.jpg;*.png;*.cr2;*.cr3;*.nef;*.raw;*.raf;*.pef;*.dng;*.arw;*.orf;*.rw2;*.fit;*.fts;*.fits;*.fit.fz;*.fits.fz;*.xisf|TIFF files|*.tif;*.tiff;|JPEG files|*.jpeg;*.jpg|PNG Files|*.png|RAW Files|*.cr2;*.cr3;*.nef;*.raw;*.raf;*.pef;*.dng;*.arw;*.orf;*.rw2|XISF Files|*.xisf|FITS Files|*.fit;*.fits;*.fit.fz;*.fits.fz";

            if (dialog.ShowDialog() == true) {
                var arr = await imageDataFactory.CreateFromFile(dialog.FileName, 16, false, ct);
                var renderedImage = arr.RenderImage();
                renderedImage = await renderedImage.Stretch(factor: 0.2, blackClipping: -2.8, unlinked: false);

                var targetName = string.IsNullOrWhiteSpace(arr.MetaData.Target?.Name) ? Path.GetFileNameWithoutExtension(dialog.FileName) : arr.MetaData.Target.Name;

                if (arr.MetaData.WorldCoordinateSystem != null) {
                    var img = renderedImage.Image;
                    if (arr.MetaData.WorldCoordinateSystem.Flipped) {
                        var tb = new TransformedBitmap();
                        tb.BeginInit();
                        tb.Source = renderedImage.Image;
                        var transform = new ScaleTransform(-1, 1, 0, 0);
                        tb.Transform = transform;
                        tb.EndInit();
                        img = tb;
                    }

                    return new FileSkySurveyImage() {
                        Name = targetName,
                        Coordinates = arr.MetaData.WorldCoordinateSystem.GetCoordinates(renderedImage.Image.PixelWidth / 2, renderedImage.Image.PixelHeight / 2),
                        FoVHeight = AstroUtil.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleY * renderedImage.Image.PixelHeight),
                        FoVWidth = AstroUtil.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleX * renderedImage.Image.PixelWidth),
                        Image = img,
                        Rotation = arr.MetaData.WorldCoordinateSystem.Rotation,
                        Source = nameof(FileSkySurvey),
                        Data = arr
                    };
                } else {
                    var pixelSize = arr.MetaData.Camera.PixelSize;
                    var focalLength = arr.MetaData.Telescope.FocalLength;
                    var arcSecPerPixel = AstroUtil.ArcsecPerPixel(pixelSize, focalLength);

                    var referenceCoordinates = arr.MetaData.Telescope.Coordinates;
                    if (referenceCoordinates == null) {
                        referenceCoordinates = arr.MetaData.Target.Coordinates;
                    }

                    return new FileSkySurveyImage() {
                        Name = targetName,
                        Coordinates = referenceCoordinates,
                        FoVHeight = arcSecPerPixel * arr.Properties.Height,
                        FoVWidth = arcSecPerPixel * arr.Properties.Width,
                        Image = renderedImage.Image,
                        Rotation = double.NaN,
                        Source = nameof(FileSkySurvey),
                        Data = arr
                    };
                }
            } else {
                return null;
            }
        }
#endif

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetImage - takes an already-chosen file path
        /// instead of opening its own file dialog, since Avalonia's file picker
        /// (IStorageProvider) is fundamentally different from WPF's synchronous
        /// Microsoft.Win32.OpenFileDialog (async, needs a real TopLevel/window reference) and
        /// can't be called from this portable library - NINA.WPF.Base has no reference to
        /// Avalonia, by design, same reasoning as every other *Portable method here. The caller
        /// (FramingAssistantViewModel, via Avalonia's own IStorageProvider) picks the file and
        /// passes the path in; everything after that - decode, stretch, WCS/telescope-derived
        /// framing - is real, portable logic, reusing the same IImageData/IRenderedImage
        /// pipeline the Phase 2 Imaging tab already proved out (CreateFromFile -> RenderImage
        /// -> Stretch -> RawPixels).
        /// </summary>
        public async Task<SkySurveyImagePortable> GetImagePortableFromPath(string filePath, CancellationToken ct) {
            var arr = await imageDataFactory.CreateFromFile(filePath, 16, false, ct);
            var renderedImage = arr.RenderImage();
            renderedImage = await renderedImage.Stretch(factor: 0.2, blackClipping: -2.8, unlinked: false);

            var targetName = string.IsNullOrWhiteSpace(arr.MetaData.Target?.Name) ? Path.GetFileNameWithoutExtension(filePath) : arr.MetaData.Target.Name;
            var img = ToImageSharp(renderedImage.RawPixels);

            if (arr.MetaData.WorldCoordinateSystem != null) {
                if (arr.MetaData.WorldCoordinateSystem.Flipped) {
                    // Portable equivalent of the WPF path's TransformedBitmap+ScaleTransform(-1,1)
                    // horizontal flip.
                    img.Mutate(x => x.Flip(FlipMode.Horizontal));
                }

                return new SkySurveyImagePortable {
                    Name = targetName,
                    Coordinates = arr.MetaData.WorldCoordinateSystem.GetCoordinates(renderedImage.RawPixels.Width / 2, renderedImage.RawPixels.Height / 2),
                    FoVHeight = AstroUtil.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleY * renderedImage.RawPixels.Height),
                    FoVWidth = AstroUtil.ArcsecToArcmin(arr.MetaData.WorldCoordinateSystem.PixelScaleX * renderedImage.RawPixels.Width),
                    Image = img,
                    Rotation = arr.MetaData.WorldCoordinateSystem.Rotation,
                    Source = nameof(FileSkySurvey)
                };
            } else {
                var pixelSize = arr.MetaData.Camera.PixelSize;
                var focalLength = arr.MetaData.Telescope.FocalLength;
                var arcSecPerPixel = AstroUtil.ArcsecPerPixel(pixelSize, focalLength);

                var referenceCoordinates = arr.MetaData.Telescope.Coordinates ?? arr.MetaData.Target.Coordinates;

                return new SkySurveyImagePortable {
                    Name = targetName,
                    Coordinates = referenceCoordinates,
                    FoVHeight = arcSecPerPixel * arr.Properties.Height,
                    FoVWidth = arcSecPerPixel * arr.Properties.Width,
                    Image = img,
                    Rotation = double.NaN,
                    Source = nameof(FileSkySurvey)
                };
            }
        }

        /// <summary>
        /// Downshifts PortableImageBuffer's 16-bit-per-channel data to 8-bit and builds a real
        /// ImageSharp image - same conversion convention (ushort >> 8, [R,G,B] triplet layout
        /// with R at index 2/G at 1/B at 0 for Rgb48) as
        /// NINA.Avalonia.Utility.PortableImageBufferConverter.ToWriteableBitmap, kept consistent
        /// since both read the exact same PortableImageBuffer shape.
        /// </summary>
        private static Image<Rgba32> ToImageSharp(PortableImageBuffer buffer) {
            var img = new Image<Rgba32>(buffer.Width, buffer.Height);

            img.ProcessPixelRows(accessor => {
                for (int y = 0; y < accessor.Height; y++) {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++) {
                        int i = y * accessor.Width + x;
                        if (buffer.Format == PortablePixelFormat.Rgb48) {
                            byte b = (byte)(buffer.Data[i * 3 + 0] >> 8);
                            byte g = (byte)(buffer.Data[i * 3 + 1] >> 8);
                            byte r = (byte)(buffer.Data[i * 3 + 2] >> 8);
                            row[x] = new Rgba32(r, g, b, (byte)255);
                        } else {
                            byte v = (byte)(buffer.Data[i] >> 8);
                            row[x] = new Rgba32(v, v, v, (byte)255);
                        }
                    }
                }
            });

            return img;
        }
    }

    public class FileSkySurveyImage : SkySurveyImage {
        public IImageData Data { get; set; }
    }
}
