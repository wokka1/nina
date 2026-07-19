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
using System.Windows.Media.Imaging;

namespace NINA.Image.ImageData {

    public class DebayeredImage : RenderedImage, IDebayeredImage {
        public LRGBArrays DebayeredData { get; private set; }

        public bool SaveColorChannels { get; private set; }
        public bool SaveLumChannel { get; private set; }

        public SensorType BayerPattern { get; private set; }

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

        public static IDebayeredImage Debayer(
            IRenderedImage imageData,
            IProfileService profileService,
            IStarDetection starDetection,
            IStarAnnotator starAnnotator,
            bool saveColorChannels = false,
            bool saveLumChannel = false,
            SensorType bayerPattern = SensorType.RGGB) {
            var debayeredImage = ImageUtility.Debayer(imageData.Image, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale, saveColorChannels, saveLumChannel, bayerPattern);

            // Also compute the portable (WPF-free) equivalent directly from the raw
            // sensor array, via the same BayerFilter16bpp logic as the Bitmap path
            // above (commit d59d86516/DebayerArray) - independent computation, not
            // derived from debayeredImage, so it doesn't depend on saveColorChannels
            // having been set (LRGBArrays is often null/unpopulated otherwise).
            var (rawPixelsBuffer, _) = ImageUtility.DebayerArray(
                imageData.RawImageData.Data.FlatArray,
                imageData.RawImageData.Properties.Width,
                imageData.RawImageData.Properties.Height,
                saveColorChannels,
                saveLumChannel,
                bayerPattern);

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
        }

        public override async Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked) {
            if (this.DebayeredData == null) {
                // Unlinked stretch is only possible when the RGB Array was saved separately during debayer
                // This scenario will happen when the options are changed after the debayer has happened and the image is re-stretched again
                unlinked = false;
            }
            var stretchedImage = unlinked ? await ImageUtility.StretchUnlinked(this, factor, blackClipping) : await ImageUtility.Stretch(this, factor, blackClipping);

            // Also compute the portable equivalent, mirroring the same
            // linked/unlinked branch above via the array-based stretch methods
            // (commit 841e79bb9) instead of a Bitmap round-trip.
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
        }

        public override IRenderedImage ReRender() {
            var reRenderedImage = base.ReRender();
            return reRenderedImage.Debayer(saveColorChannels: this.SaveColorChannels, saveLumChannel: this.SaveLumChannel, bayerPattern: this.BayerPattern);
        }
    }
}