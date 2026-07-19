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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Core.Utility;
#if HAS_WPF
using NINA.Core.Utility.WindowService;
#endif
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
#if HAS_WPF
using System.Windows.Media.Imaging;
#endif
using NINA.Image.ImageAnalysis;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using System;

namespace NINA.Equipment.Interfaces.ViewModel {

    public interface IImageControlVM : IDockableVM {
        bool AutoStretch { get; set; }
#if HAS_WPF
        BahtinovImage BahtinovImage { get; }
#endif
        ObservableRectangle BahtinovRectangle { get; set; }
        ICommand CancelPlateSolveImageCommand { get; }
        bool DetectStars { get; set; }
        ICommand DragMoveCommand { get; }
        double DragResizeBoundary { get; }
        ICommand DragStartCommand { get; }
        ICommand DragStopCommand { get; }
#if HAS_WPF
        BitmapSource Image { get; set; }
#endif
        IAsyncCommand InspectAberrationCommand { get; }
        bool IsLiveViewEnabled { get; }
        IAsyncCommand PlateSolveImageCommand { get; }
        AsyncCommand<bool> PrepareImageCommand { get; }
        IRenderedImage RenderedImage { get; set; }
        bool ShowBahtinovAnalyzer { get; set; }
        bool ShowCrossHair { get; set; }
        ApplicationStatus Status { get; set; }
#if HAS_WPF
        IWindowServiceFactory WindowServiceFactory { get; set; }
#endif
        int ImageRotation { get; set; }

        void Dispose();

        Task<IRenderedImage> PrepareImage(IImageData data, PrepareImageParameters parameters, CancellationToken cancelToken);

        void UpdateDeviceInfo(CameraInfo cameraInfo);

        event EventHandler<ImagePreparedEventArgs> ImagePrepared;
    }
}