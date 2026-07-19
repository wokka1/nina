#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System.Threading.Tasks;
#if HAS_WPF
using System.Windows.Media.Imaging;
#endif

namespace NINA.Image.ImageData {

    public class DebayeredImage : RenderedImage, IDebayeredImage {
        public LRGBArrays DebayeredData { get; private set; }

        public bool SaveColorChannels { get; private set; }
        public bool SaveLumChannel { get; private set; }

        public SensorType BayerPattern { get; private set; }

#if HAS_WPF
        protected DebayeredImage(
            BitmapSource image,
            IImageData rawImageData,
            LRGBArrays debayeredData,
            bool saveColorChannels,
            bool saveLumChannels,
            SensorType bayerPattern,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator,
            PortableImageBuffer rawPixels = null) :
            base(image, rawImageData, profileService, starDetection, starAnnotator, rawPixels) {
            this.DebayeredData = debayeredData;
            this.SaveColorChannels = saveColorChannels;
            this.SaveLumChannel = saveLumChannels;
            this.BayerPattern = bayerPattern;
        }
#else
        protected DebayeredImage(
            IImageData rawImageData,
            LRGBArrays debayeredData,
            bool saveColorChannels,
            bool saveLumChannels,
            SensorType bayerPattern,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator,
            PortableImageBuffer rawPixels = null) :
            base(rawImageData, profileService, starDetection, starAnnotator, rawPixels) {
            this.DebayeredData = debayeredData;
            this.SaveColorChannels = saveColorChannels;
            this.SaveLumChannel = saveLumChannels;
            this.BayerPattern = bayerPattern;
        }
#endif

        public static IDebayeredImage Debayer(
            IRenderedImage imageData,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator,
            bool saveColorChannels = false,
            bool saveLumChannel = false,
            SensorType bayerPattern = SensorType.RGGB) {
            // Portable computation, done unconditionally - direct from the raw sensor
            // array via BayerFilter16bpp (commit d59d86516/DebayerArray), independent
            // of the WPF path below so it doesn't depend on saveColorChannels having
            // been set for that path (LRGBArrays is often null/unpopulated otherwise).
            var (rawPixelsBuffer, portableLrgb) = ImageUtility.DebayerArray(
                imageData.RawImageData.Data.FlatArray,
                imageData.RawImageData.Properties.Width,
                imageData.RawImageData.Properties.Height,
                saveColorChannels,
                saveLumChannel,
                bayerPattern);

#if HAS_WPF
            var debayeredImage = ImageUtility.Debayer(imageData.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel, bayerPattern);
            return new DebayeredImage(
                image: debayeredImage.ImageSource,
                rawImageData: imageData.RawImageData,
                debayeredData: debayeredImage.Data,
                saveColorChannels: saveColorChannels,
                saveLumChannels: saveLumChannel,
                bayerPattern: bayerPattern,
                profileService: profileService,
                starDetection: starDetection,
                starAnnotator: starAnnotator,
                rawPixels: rawPixelsBuffer);
#else
            return new DebayeredImage(
                rawImageData: imageData.RawImageData,
                debayeredData: portableLrgb,
                saveColorChannels: saveColorChannels,
                saveLumChannels: saveLumChannel,
                bayerPattern: bayerPattern,
                profileService: profileService,
                starDetection: starDetection,
                starAnnotator: starAnnotator,
                rawPixels: rawPixelsBuffer);
#endif
        }

        public override async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
            if (this.DebayeredData == null) {
                // Unlinked stretch is only possible when the RGB Array was saved separately during debayer
                // This scenario will happen when the options are changed after the debayer has happened and the image is re-stretched again
                unlinked = false;
            }

            // Portable array-based stretch, computed unconditionally - mirrors the same
            // linked/unlinked branch the WPF path below uses (commit 841e79bb9).
            ushort[] stretchedPixels;
            if (unlinked) {
                var redStats = ImageStatistics.Create(this.RawImageData.Properties, this.DebayeredData.Red);
                var greenStats = ImageStatistics.Create(this.RawImageData.Properties, this.DebayeredData.Green);
                var blueStats = ImageStatistics.Create(this.RawImageData.Properties, this.DebayeredData.Blue);
                stretchedPixels = ImageUtility.StretchUnlinkedArray(redStats, greenStats, blueStats, this.RawPixels.Data, factor, blackClipping);
            } else {
                var statistics = await this.RawImageData.Statistics.Task;
                stretchedPixels = ImageUtility.StretchLinkedRgbArray(statistics, this.RawPixels.Data, factor, blackClipping);
            }
            var stretchedRawPixels = new PortableImageBuffer(stretchedPixels, this.RawImageData.Properties.Width, this.RawImageData.Properties.Height, PortablePixelFormat.Rgb48);

#if HAS_WPF
            var stretchedImage = unlinked ? await ImageUtility.StretchUnlinked(this, factor, blackClipping) : await ImageUtility.Stretch(this, factor, blackClipping);
            return new DebayeredImage(
                image: stretchedImage,
                rawImageData: this.RawImageData,
                debayeredData: this.DebayeredData,
                saveColorChannels: this.SaveColorChannels,
                saveLumChannels: this.SaveLumChannel,
                bayerPattern: this.BayerPattern,
                profileService: this.profileService,
                starDetection: this.starDetection,
                starAnnotator: this.starAnnotator,
                rawPixels: stretchedRawPixels);
#else
            return new DebayeredImage(
                rawImageData: this.RawImageData,
                debayeredData: this.DebayeredData,
                saveColorChannels: this.SaveColorChannels,
                saveLumChannels: this.SaveLumChannel,
                bayerPattern: this.BayerPattern,
                profileService: this.profileService,
                starDetection: this.starDetection,
                starAnnotator: this.starAnnotator,
                rawPixels: stretchedRawPixels);
#endif
        }

        public override IRenderedImage ReRender() {
            var reRenderedImage = base.ReRender();
            return reRenderedImage.Debayer(saveColorChannels: this.SaveColorChannels, saveLumChannel: this.SaveLumChannel, bayerPattern: this.BayerPattern);
        }
    }
}