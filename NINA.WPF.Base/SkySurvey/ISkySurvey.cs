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
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NINA.WPF.Base.SkySurvey {

    public interface ISkySurvey {

        Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress);

        Task<SkySurveyImage> GetImage(string name, string hipsSkyMapPath, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress) {

            return GetImage(name, coordinates: coordinates, fieldOfView: fieldOfView, width: width, height: height, ct: ct, progress: progress);
        }

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of GetImage - returns a SkySurveyImagePortable instead of a
        /// BitmapSource-backed SkySurveyImage, so it also runs on macOS/Linux. Default implementation throws -
        /// only providers that have actually been ported override it, same additive spirit as everything else
        /// in this file (existing GetImage is untouched).
        /// </summary>
        Task<SkySurveyImagePortable> GetImagePortable(string name, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress) {
            throw new NotSupportedException(GetType().Name + " does not yet implement the portable image path.");
        }

        /// <summary>
        /// Portable counterpart to the hipsSkyMapPath-taking GetImage overload (used by Hips2FitsSurvey).
        /// </summary>
        Task<SkySurveyImagePortable> GetImagePortable(string name, string hipsSkyMapPath, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress) {
            return GetImagePortable(name, coordinates: coordinates, fieldOfView: fieldOfView, width: width, height: height, ct: ct, progress: progress);
        }

    }

    public class SkySurveyImage {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Source { get; set; }
        public BitmapSource Image { get; set; }
        public double FoVWidth { get; set; }
        public double FoVHeight { get; set; }
        public double Rotation { get; set; }
        public Coordinates Coordinates { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Portable (ImageSharp-based) counterpart to SkySurveyImage - same shape, Image is a SixLabors.ImageSharp
    /// buffer instead of a WPF BitmapSource.
    /// </summary>
    public class SkySurveyImagePortable {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Source { get; set; }
        public Image<Rgba32> Image { get; set; }
        public double FoVWidth { get; set; }
        public double FoVHeight { get; set; }
        public double Rotation { get; set; }
        public Coordinates Coordinates { get; set; }
        public string Name { get; set; }
    }
}