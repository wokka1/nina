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
using NINA.Core.Model;
using NINA.Image.ImageAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
#if HAS_WPF
using System.Windows.Media.Imaging;
#endif

namespace NINA.Image.Interfaces {

    public interface IRenderedImage {
        IImageData RawImageData { get; }

#if HAS_WPF
        BitmapSource OriginalImage { get; }

        BitmapSource Image { get; }
#endif

        /// <summary>
        /// Portable (no WPF/System.Drawing dependency) counterpart to Image -
        /// same rendered pixel data, computed via NINA.Image.ImageAnalysis's
        /// array-based debayer/stretch methods rather than a BitmapSource
        /// round-trip. See NINA.Image.ImageData.PortableImageBuffer.
        /// </summary>
        NINA.Image.ImageData.PortableImageBuffer RawPixels { get; }

        IDebayeredImage Debayer(bool saveColorChannels = false, bool saveLumChannel = false, SensorType bayerPattern = SensorType.RGGB);

        IRenderedImage ReRender();

        Task<IRenderedImage> Stretch(double factor, double blackClipping, bool unlinked);

        Task<IRenderedImage> DetectStars(
            bool annotateImage,
            StarSensitivityEnum sensitivity,
            NoiseReductionEnum noiseReduction,
            CancellationToken cancelToken = default,
            IProgress<ApplicationStatus> progress = default(Progress<ApplicationStatus>));

#if HAS_WPF
        Task<BitmapSource> GetThumbnail();
#endif
        void UpdateAnalysis(StarDetectionParams p, StarDetectionResult result);
    }
}