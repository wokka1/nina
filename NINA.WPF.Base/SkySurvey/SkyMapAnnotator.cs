#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model.FramingAssistant;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if HAS_WPF
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#endif
using System.Windows.Input;
using ISPointF = SixLabors.ImageSharp.PointF;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace NINA.WPF.Base.SkySurvey {

    public partial class SkyMapAnnotator : BaseINPC, ITelescopeConsumer
#if HAS_WPF
        , ISkyMapAnnotator
#endif
    {
        private readonly DatabaseInteraction dbInstance;
        public ViewportFoV ViewportFoV { get; private set; }
        private List<Constellation> dbConstellations;
        private Dictionary<string, DeepSkyObject> dbDSOs;
        private List<CacheImage> cacheImages;
        private Bitmap img;
        private Bitmap dsoImageBuffer;
        private Graphics g;
        private Graphics dsoImageGraphics;
        private ITelescopeMediator telescopeMediator;
        private readonly IProfileService profileService;
        private CacheSkySurvey cache;

        public SkyMapAnnotator() {
            dbInstance = new DatabaseInteraction();
            DSOInViewport = new List<FramingDSO>();
            ConstellationsInViewport = new List<FramingConstellation>();
            FrameLineMatrix = new FrameLineMatrix2();
            ConstellationBoundaries = new Dictionary<string, ConstellationBoundary>();
            cacheImages = new List<CacheImage>();
            annotateDSO = true;
            annotateGrid = true;
        }

        public SkyMapAnnotator(ITelescopeMediator mediator, IProfileService profileService) : this() {
            this.telescopeMediator = mediator;
            this.profileService = profileService;
            LoadSettings();
        }

        public async Task Initialize(Coordinates centerCoordinates, double vFoVDegrees, double imageWidth, double imageHeight, double imageRotation, CacheSkySurvey cache, CancellationToken ct) {
            telescopeMediator?.RemoveConsumer(this);

            dsoImageGraphics?.Dispose();
            g?.Dispose();
            img?.Dispose();
            dsoImageBuffer?.Dispose();

            this.cache = cache;

            ViewportFoV = new ViewportFoV(centerCoordinates, vFoVDegrees, imageWidth, imageHeight, imageRotation);

            if (dbConstellations == null) {
                dbConstellations = await dbInstance.GetConstellationsWithStars(ct);
            }

            if (dbDSOs == null) {
                dbDSOs = (await dbInstance.GetDeepSkyObjects(string.Empty, null, new DatabaseInteraction.DeepSkyObjectSearchParams(), ct)).ToDictionary(x => x.Id, y => y);
            }

            if (ActiveCatalogues == null) {
                var catalogues = await dbInstance.GetCatalogues(50, ct);
                var settings = profileService.ActiveProfile.FramingAssistantSettings;

                ActiveCatalogues = catalogues?.Select(x => {

                    bool isActive = !settings.DisabledCatalogues.Contains(x);
                    var activeCat = new ActiveCatalogue(x, isActive);

                    activeCat.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(ActiveCatalogue.Active)) {
                            SaveDisabledCatalogues();
                            #if HAS_WPF
                            UpdateSkyMap();
                            #endif
                        }
                    };

                    return activeCat;
                }).ToList() ?? new List<ActiveCatalogue>();

                UpdateShowAllCataloguesState();
            }

            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();

            img = new Bitmap((int)ViewportFoV.Width, (int)ViewportFoV.Height, PixelFormat.Format32bppArgb);
            dsoImageBuffer = new Bitmap((int)ViewportFoV.Width, (int)ViewportFoV.Height, PixelFormat.Format32bppArgb);

            dsoImageGraphics = Graphics.FromImage(dsoImageBuffer);
            dsoImageGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            dsoImageGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            g = Graphics.FromImage(img);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            InitializePortableBuffers();

            FrameLineMatrix.CalculatePoints(ViewportFoV);
            if (ConstellationBoundaries.Count == 0) {
                ConstellationBoundaries = await GetConstellationBoundaries();
            }

            telescopeMediator?.RegisterConsumer(this);
            Initialized = true;

            firstDraw = true;
            #if HAS_WPF
            UpdateSkyMap();
            #endif
            firstDraw = false;
        }

        public ViewportFoV ChangeFoV(double vFoVDegrees) {
            ConstellationsInViewport.Clear();
            ClearFrameLineMatrix();
            ViewportFoV = new ViewportFoV(ViewportFoV.CenterCoordinates, vFoVDegrees, ViewportFoV.Width, ViewportFoV.Height, ViewportFoV.Rotation);

            FrameLineMatrix.CalculatePoints(ViewportFoV);

            #if HAS_WPF
            UpdateSkyMap();
            #endif

            return ViewportFoV;
        }

        public ICommand DragCommand { get; private set; }

        public FrameLineMatrix2 FrameLineMatrix { get; private set; }

        public List<FramingDSO> DSOInViewport { get; private set; }

        [ObservableProperty]
        private bool initialized;

        public List<FramingConstellation> ConstellationsInViewport { get; private set; }

        [ObservableProperty]
        private IList<ActiveCatalogue> activeCatalogues;

        [ObservableProperty]
        private bool annotateConstellationBoundaries;

        [ObservableProperty]
        private bool dynamicFoV;

        [ObservableProperty]
        private bool annotateConstellations;

        [ObservableProperty]
        private bool annotateGrid;

        [ObservableProperty]
        private bool annotateDSO;

        [ObservableProperty]
        private bool useCachedImages;

#if HAS_WPF
        [ObservableProperty]
        private BitmapSource skyMapOverlay;
#endif

        [ObservableProperty]
        private bool showAllCatalogues = true;
        private List<string> DisabledCatalogues =>
               profileService.ActiveProfile.FramingAssistantSettings?.DisabledCatalogues ?? new List<string>();

        partial void OnAnnotateConstellationBoundariesChanged(bool oldValue, bool newValue) {
            if (profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellationBoundaries != newValue)
                profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellationBoundaries = newValue;
        }

        partial void OnAnnotateConstellationsChanged(bool oldValue, bool newValue) {
            if (profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellations != newValue)
                profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellations = newValue;
        }

        partial void OnAnnotateDSOChanged(bool oldValue, bool newValue) {
            if (profileService.ActiveProfile.FramingAssistantSettings.AnnotateDSO != newValue)
                profileService.ActiveProfile.FramingAssistantSettings.AnnotateDSO = newValue;
        }

        partial void OnAnnotateGridChanged(bool oldValue, bool newValue) {
            if (profileService.ActiveProfile.FramingAssistantSettings.AnnotateGrid != newValue)
                profileService.ActiveProfile.FramingAssistantSettings.AnnotateGrid = newValue;
        }

        partial void OnShowAllCataloguesChanged(bool oldValue, bool newValue) {
            if (ActiveCatalogues == null) return;

            foreach (var catalogue in ActiveCatalogues) {
                catalogue.Active = newValue;
            }

            SaveDisabledCatalogues();
            #if HAS_WPF
            UpdateSkyMap();
            #endif
        }
        private void SaveDisabledCatalogues() {

            if (ActiveCatalogues == null) return;

            var settings = profileService.ActiveProfile.FramingAssistantSettings;
            settings.DisabledCatalogues = ActiveCatalogues
                .Where(x => !x.Active)
                .Select(x => x.Name)
                .ToList();
        }
        private void UpdateShowAllCataloguesState() {

            if (ActiveCatalogues == null || !ActiveCatalogues.Any()) return;

            bool newState;
            if (ActiveCatalogues.All(c => c.Active)) {
                newState = true;
            } else if (ActiveCatalogues.All(c => !c.Active)) {
                newState = false;
            } else {
                return;
            }

            if (ShowAllCatalogues != newState) {
                ShowAllCatalogues = newState;
            }
        }

        private void LoadSettings() {
            AnnotateConstellationBoundaries =
                profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellationBoundaries;
            AnnotateConstellations = profileService.ActiveProfile.FramingAssistantSettings.AnnotateConstellations;
            AnnotateDSO = profileService.ActiveProfile.FramingAssistantSettings.AnnotateDSO;
            AnnotateGrid = profileService.ActiveProfile.FramingAssistantSettings.AnnotateGrid;
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DeepSkyObject> GetDeepSkyObjectsForViewport() {
            var dsoList = new Dictionary<string, DeepSkyObject>();

            double minSize = 0;
            if (!(Math.Min(ViewportFoV.HFoV, ViewportFoV.VFoV) < 10)) {
                // Stuff has to be at least 3 pixel wide
                minSize = 3 * Math.Min(ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);
            }
            var maxSize = AstroUtil.DegreeToArcsec(2 * Math.Max(ViewportFoV.HFoV, ViewportFoV.VFoV));

            var filteredCatalogues = ActiveCatalogues.Where(x => !x.Active).Select(x => x.Name).ToList();

            return dbDSOs
                .Where(d => (d.Value.Size != null && d.Value.Size > minSize && d.Value.Size < maxSize) || ViewportFoV.VFoV <= 10)
                .Where(dso => !filteredCatalogues.Any(dso.Value.Name.StartsWith))
                .Where(dso => ViewportFoV.ContainsCoordinates(dso.Value.Coordinates))
                .ToDictionary(x => x.Key, y => y.Value);
        }

        public void ClearImagesForViewport() {
            if (cacheImages != null) {
                foreach (var image in cacheImages) {
                    image.Dispose();
                }
                cacheImages.Clear();
            }
        }

        /// <summary>
        /// Query for skyobjects for a reference coordinate that overlap the current viewport
        /// </summary>
        /// <returns></returns>
        private List<CacheImage> GetCacheImagesForViewport() {
            using (MyStopWatch.Measure()) {
                double minSize = 6;
                double maxSize = 600;

                var l = new List<CacheImage>();
                foreach (var entry in cache.Cache.Elements("Image")) {
                    double fovW = double.Parse(entry.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
                    double fovH = double.Parse(entry.Attribute("FoVH").Value, CultureInfo.InvariantCulture);

                    if (fovW < minSize || fovW > maxSize) {
                        continue;
                    }

                    double ra = double.Parse(entry.Attribute("RA").Value, CultureInfo.InvariantCulture);
                    double dec = double.Parse(entry.Attribute("Dec").Value, CultureInfo.InvariantCulture);
                    double rotation = double.Parse(entry.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
                    string path = Path.Combine(cache.framingAssistantCachePath, entry.Attribute("FileName").Value);

                    if (AstroUtil.ArcminToArcsec(fovW) > minSize && AstroUtil.ArcminToArcsec(fovH) > minSize) {
                        var existing = cacheImages.FirstOrDefault(x => x.Coordinates.RA == ra && x.Coordinates.Dec == dec);
                        if (existing == null) {
                            existing = new CacheImage(ra, dec, fovW, fovH, rotation, path);
                            cacheImages.Add(existing);
                        }
                        l.Add(existing);
                    }
                }

                l = l.Where(x => {
                    var distance = x.Coordinates - ViewportFoV.CenterCoordinates;
                    return distance.Distance.Degree < Math.Max(ViewportFoV.HFoV, ViewportFoV.VFoV) + AstroUtil.ArcminToDegree(Math.Max(x.FoVH, x.FoVW));
                })
                    //Order in descending order so that smallest field of view is drawn on top, as it most likely contains most details
                    .OrderByDescending(x => x.FoVW)
                    .ToList();

                return l;
            }
        }

#if HAS_WPF
        public Coordinates ShiftViewport(Vector delta) {
            ViewportFoV.Shift(delta);

            return ViewportFoV.CenterCoordinates;
        }
#endif

        /// <summary>
        /// Portable (WPF-free) equivalent of ShiftViewport(Vector) - ViewportFoV.ShiftPortable
        /// already does the real work with plain doubles, Vector was only ever a convenience
        /// (X,Y) container here, not real vector math.
        /// </summary>
        public Coordinates ShiftViewportPortable(double deltaX, double deltaY) {
            ViewportFoV.ShiftPortable(deltaX, deltaY);

            return ViewportFoV.CenterCoordinates;
        }

        public void ClearFrameLineMatrix() {
            FrameLineMatrix.RAPoints.Clear();
            FrameLineMatrix.DecPoints.Clear();
        }

        public void CalculateFrameLineMatrix() {
            FrameLineMatrix.CalculatePoints(ViewportFoV);
        }

        private Dictionary<string, ConstellationBoundary> ConstellationBoundaries;

        private async Task<Dictionary<string, ConstellationBoundary>> GetConstellationBoundaries() {
            var dic = new Dictionary<string, ConstellationBoundary>();
            var list = await dbInstance.GetConstellationBoundaries(new CancellationToken());
            foreach (var item in list) {
                dic.Add(item.Name, item);
            }
            return dic;
        }

        public List<FramingConstellationBoundary> ConstellationBoundariesInViewPort { get; private set; } = new List<FramingConstellationBoundary>();

        public void CalculateConstellationBoundaries() {
            ConstellationBoundariesInViewPort.Clear();
            foreach (var boundary in ConstellationBoundaries) {
                var frameLine = new FramingConstellationBoundary();
                if (boundary.Value.Boundaries.Any((x) => ViewportFoV.ContainsCoordinates(x))) {
                    foreach (var coordinates in boundary.Value.Boundaries) {
                        var point = coordinates.XYProjectionPortable(ViewportFoV);
                        frameLine.Points.Add(new PointF((float)point.X, (float)point.Y));
                    }

                    ConstellationBoundariesInViewPort.Add(frameLine);
                }
            }
        }

        // These 5 helpers (through UpdateAndDrawGrid below) call the original .Draw(g)/.DrawStars(g)/
        // .DrawAnnotations(g) methods on FramingDSO/FramingConstellation/FramingConstellationBoundary/
        // FrameLineMatrix2 - all now #if HAS_WPF-gated in their own files, so these callers need the same gate.
#if HAS_WPF
        private void UpdateAndAnnotateDSOs() {
            var allGatheredDSO = GetDeepSkyObjectsForViewport();

            var existingDSOs = new List<string>();
            for (int i = DSOInViewport.Count - 1; i >= 0; i--) {
                var dso = DSOInViewport[i];
                if (allGatheredDSO.ContainsKey(dso.Id)) {
                    dso.RecalculateTopLeft(ViewportFoV);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInViewport.RemoveAt(i);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, ViewportFoV));
            }

            foreach (var dso in DSOInViewport) {
                dso.Draw(g);
            }
        }

        private void DrawStars() {
            foreach (var constellation in ConstellationsInViewport) {
                constellation.DrawStars(g);
            }
        }

        private void UpdateAndAnnotateConstellations(bool drawAnnotations) {
            foreach (var constellation in dbConstellations) {
                var viewPortConstellation = ConstellationsInViewport.FirstOrDefault(x => x.Id == constellation.Id);

                var isInViewport = false;
                foreach (var star in constellation.Stars) {
                    if (!ViewportFoV.ContainsCoordinates(star.Coords)) {
                        continue;
                    }

                    isInViewport = true;
                    break;
                }

                if (isInViewport) {
                    if (viewPortConstellation == null) {
                        var framingConstellation = new FramingConstellation(constellation, ViewportFoV);
                        framingConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                        ConstellationsInViewport.Add(framingConstellation);
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            if (drawAnnotations) {
                foreach (var constellation in ConstellationsInViewport) {
                    constellation.DrawAnnotations(g);
                }
            }
        }

        private void UpdateAndDrawConstellationBoundaries() {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                constellationBoundary.Draw(g);
            }
        }

        private void UpdateAndDrawGrid() {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            FrameLineMatrix.Draw(g);
        }
#endif

#if HAS_WPF
        private Task DrawBufferedDSOImages(CancellationToken ct) {
            return Task.Run(async () => {
                try {
                    var relevantImages = GetCacheImagesForViewport();
                    foreach (var cacheImage in relevantImages) {
                        ct.ThrowIfCancellationRequested();
                        if (File.Exists(cacheImage.ImagePath)) {
                            var image = cacheImage.GetImageForScale(ViewportFoV.HFoV, ViewportFoV.Width);
                            var sourceR = new RectangleF(0, 0, image.Width, image.Height);

                            var imageResW = AstroUtil.ArcminToArcsec(cacheImage.FoVW) / image.Width;
                            var imageResH = AstroUtil.ArcminToArcsec(cacheImage.FoVH) / image.Height;
                            var conversionW = imageResW / ViewportFoV.ArcSecWidth;
                            var conversionH = imageResH / ViewportFoV.ArcSecHeight;
                            var dest = new RectangleF(-(float)(image.Width * conversionW / 2f), -(float)(image.Height * conversionH / 2f), (float)(image.Width * conversionW), (float)(image.Height * conversionH));

                            var center = cacheImage.Coordinates.XYProjectionPortable(ViewportFoV);

                            var panelDeltaX = center.X - ViewportFoV.ViewPortCenterPointPortable.X;
                            var panelDeltaY = center.Y - ViewportFoV.ViewPortCenterPointPortable.Y;
                            var referenceCenter = ViewportFoV.CenterCoordinates.Shift(panelDeltaX < 1E-10 ? 1 : 0, panelDeltaY, ViewportFoV.Rotation, ViewportFoV.ArcSecWidth, ViewportFoV.ArcSecHeight);

                            var rotation = -(90 - ((float)AstroUtil.CalculatePositionAngle(referenceCenter.RADegrees, cacheImage.Coordinates.RADegrees, referenceCenter.Dec, cacheImage.Coordinates.Dec)));
                            if (panelDeltaX < 0) {
                                rotation += 180;
                            }
                            if (cacheImage.Coordinates.Dec < 0 || (referenceCenter.Dec < 0 && cacheImage.Coordinates.Dec >= 0)) {
                                rotation += 180;
                            }

                            rotation += (float)cacheImage.Rotation;

                            dsoImageGraphics.TranslateTransform((float)center.X, (float)center.Y);
                            dsoImageGraphics.RotateTransform(rotation);
                            dsoImageGraphics.DrawImage(image, dest, sourceR, GraphicsUnit.Pixel);
                            dsoImageGraphics.ResetTransform();
                        }
                    }
                    ct.ThrowIfCancellationRequested();

                    Render();
                } catch (Exception) {
                } finally {
                    renderCts?.Cancel();
                }
            });
        }

        private void Render() {
            try {
                g.Clear(Color.Transparent);

                if (!DllLoader.IsX86() && UseCachedImages) {
                    g.DrawImage((Bitmap)dsoImageBuffer.Clone(), 0, 0);
                }

                if (!AnnotateConstellations && AnnotateDSO || AnnotateConstellations) {
                    UpdateAndAnnotateConstellations(AnnotateConstellations);
                    if (!AnnotateDSO) {
                        DrawStars();
                    }
                }

                if (AnnotateDSO) {
                    UpdateAndAnnotateDSOs();
                    DrawStars();
                }

                if (AnnotateConstellationBoundaries) {
                    UpdateAndDrawConstellationBoundaries();
                }

                if (AnnotateGrid) {
                    UpdateAndDrawGrid();
                }

                if (telescopeConnected) {
                    DrawTelescope();
                }
                var source = ImageUtility.ConvertBitmap(img, PixelFormats.Bgra32);
                source.Freeze();
                SkyMapOverlay = source;
            } catch (Exception) {
            }
        }

        private PeriodicTimer renderTimer;
        private CancellationTokenSource renderCts;
        private Task renderTask;
        private Coordinates oldCenter;
        private double oldFoV;
        private bool oldUseCachedImages;
        public void UpdateSkyMap() {
            if (Initialized) {
                var center = ViewportFoV.CenterCoordinates;
                var fov = ViewportFoV.HFoV;
                var needFullRedraw = firstDraw || center != oldCenter || fov != oldFoV || UseCachedImages != oldUseCachedImages || renderTask == null || renderTask.Status < TaskStatus.RanToCompletion;
                try {
                    try { renderCts?.Cancel(); } catch { }
                    while (renderTask != null && (renderTask.Status < TaskStatus.RanToCompletion)) {
                    }
                } catch (Exception) {
                }

                oldCenter = ViewportFoV.CenterCoordinates;
                oldFoV = ViewportFoV.HFoV;
                oldUseCachedImages = UseCachedImages;

                if (needFullRedraw) {
                    dsoImageGraphics.Clear(Color.Transparent);
                }
                Render();

                if (UseCachedImages && needFullRedraw) {
                    renderCts = new CancellationTokenSource();
                    renderTask = DrawBufferedDSOImages(renderCts.Token);

                    renderTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                    var token = renderCts.Token;
                    _ = Task.Run(async () => {
                        try {
                            while (await renderTimer.WaitForNextTickAsync(token)) {
                                Render();
                            }
                        } catch { }

                    });
                }
            }
        }
#endif

        // Real runtime finding (2026-07-24): DrawTelescope + ScopePen gated together - ScopePen's field
        // initializer runs in this class's implicit static constructor the instant any member is touched
        // (including RenderPortable/DrawTelescopePortable), needing libgdiplus, not present on macOS/Linux.
#if HAS_WPF
        private void DrawTelescope() {
            if (ViewportFoV.ContainsCoordinates(telescopeCoordinates)) {
                var scopePosition = telescopeCoordinates.XYProjectionPortable(ViewportFoV);
                g.DrawEllipse(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y - 15), 30, 30);
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y - 15),
                    (float)(scopePosition.X), (float)(scopePosition.Y - 5));
                g.DrawLine(ScopePen, (float)(scopePosition.X), (float)(scopePosition.Y + 5),
                    (float)(scopePosition.X), (float)(scopePosition.Y + 15));
                g.DrawLine(ScopePen, (float)(scopePosition.X - 15), (float)(scopePosition.Y),
                    (float)(scopePosition.X - 5), (float)(scopePosition.Y));
                g.DrawLine(ScopePen, (float)(scopePosition.X + 5), (float)(scopePosition.Y),
                    (float)(scopePosition.X + 15), (float)(scopePosition.Y));
            }
        }

        private static readonly Pen ScopePen = new Pen(Color.FromArgb(128, Color.Yellow), 2.0f);
#endif

        private bool telescopeConnected;
        private Coordinates telescopeCoordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Degrees);
        private bool firstDraw;

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (deviceInfo.Connected && deviceInfo.Coordinates != null) {
                telescopeConnected = true;
                var coordinates = deviceInfo.Coordinates.Transform(Epoch.J2000);
                if (Math.Abs(telescopeCoordinates.RADegrees - coordinates.RADegrees) > 0.01 || Math.Abs(telescopeCoordinates.Dec - coordinates.Dec) > 0.01) {
                    telescopeCoordinates = coordinates;
                    if (ViewportFoV.ContainsCoordinates(coordinates)) {
                        #if HAS_WPF
                        UpdateSkyMap();
                        #endif
                    }
                }
            } else {
                telescopeConnected = false;
            }
        }

        public void Dispose() {
            telescopeMediator?.RemoveConsumer(this);
            dsoImageGraphics?.Dispose();
            g?.Dispose();
            img?.Dispose();
            dsoImageBuffer?.Dispose();
            FrameLineMatrix?.Dispose();
            imgPortable?.Dispose();
        }

        private SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> imgPortable;

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen ScopePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.Yellow.WithAlpha(128f / 255f), 2.0f);

        [ObservableProperty]
        private byte[] skyMapOverlayRawPixels;

        /// <summary>
        /// Portable (ImageSharp-based) counterpart to Initialize()'s Bitmap/Graphics setup - allocates the
        /// equivalent overlay buffer so RenderPortable() has somewhere to draw. Called from the same place
        /// Initialize() creates img/g, right after the ViewportFoV for this session is known.
        /// NOTE: this does not yet allocate a portable dsoImageBuffer equivalent - the cached-background-image
        /// compositing (DrawBufferedDSOImages) hasn't been ported yet, see CacheSkySurveyImageFactory.Render().
        /// </summary>
        private void InitializePortableBuffers() {
            imgPortable?.Dispose();
            imgPortable = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>((int)ViewportFoV.Width, (int)ViewportFoV.Height);
        }

        /// <summary>
        /// Portable (ImageSharp-based) counterpart to Render(). Same drawing order as the original, minus the
        /// cached-background-image layer (dsoImageBuffer - not yet ported) and running through each *Portable
        /// draw method built alongside the System.Drawing-based ones instead of a shared Graphics instance.
        /// Note: DSOInViewport/ConstellationsInViewport are the same fields Render() maintains - safe because in
        /// practice only one of Render()/RenderPortable() runs against a given SkyMapAnnotator instance (WPF app
        /// vs. Avalonia app), never both on the same instance.
        /// </summary>
        public void RenderPortable() {
            try {
                if (imgPortable == null) {
                    InitializePortableBuffers();
                }

                imgPortable.Mutate(ctx => {
                    SixLabors.ImageSharp.Drawing.Processing.ClearExtensions.Clear(ctx, SixLabors.ImageSharp.Color.Transparent);

                    if (!AnnotateConstellations && AnnotateDSO || AnnotateConstellations) {
                        UpdateAndAnnotateConstellationsPortable(ctx, AnnotateConstellations);
                        if (!AnnotateDSO) {
                            DrawStarsPortable(ctx);
                        }
                    }

                    if (AnnotateDSO) {
                        UpdateAndAnnotateDSOsPortable(ctx);
                        DrawStarsPortable(ctx);
                    }

                    if (AnnotateConstellationBoundaries) {
                        UpdateAndDrawConstellationBoundariesPortable(ctx);
                    }

                    if (AnnotateGrid) {
                        UpdateAndDrawGridPortable(ctx);
                    }

                    if (telescopeConnected) {
                        DrawTelescopePortable(ctx);
                    }
                });

                var buffer = new byte[imgPortable.Width * imgPortable.Height * 4];
                imgPortable.CopyPixelDataTo(buffer);
                SkyMapOverlayRawPixels = buffer;
            } catch (Exception) {
            }
        }

        private void UpdateAndAnnotateDSOsPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            var allGatheredDSO = GetDeepSkyObjectsForViewport();

            var existingDSOs = new List<string>();
            for (int i = DSOInViewport.Count - 1; i >= 0; i--) {
                var dso = DSOInViewport[i];
                if (allGatheredDSO.ContainsKey(dso.Id)) {
                    dso.RecalculateTopLeft(ViewportFoV);
                    existingDSOs.Add(dso.Id);
                } else {
                    DSOInViewport.RemoveAt(i);
                }
            }

            var dsosToAdd = allGatheredDSO.Where(x => !existingDSOs.Any(y => y == x.Value.Id));
            foreach (var dso in dsosToAdd) {
                DSOInViewport.Add(new FramingDSO(dso.Value, ViewportFoV));
            }

            foreach (var dso in DSOInViewport) {
                dso.DrawPortable(ctx);
            }
        }

        private void DrawStarsPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            foreach (var constellation in ConstellationsInViewport) {
                constellation.DrawStarsPortable(ctx);
            }
        }

        private void UpdateAndAnnotateConstellationsPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx, bool drawAnnotations) {
            foreach (var constellation in dbConstellations) {
                var viewPortConstellation = ConstellationsInViewport.FirstOrDefault(x => x.Id == constellation.Id);

                var isInViewport = false;
                foreach (var star in constellation.Stars) {
                    if (!ViewportFoV.ContainsCoordinates(star.Coords)) {
                        continue;
                    }

                    isInViewport = true;
                    break;
                }

                if (isInViewport) {
                    if (viewPortConstellation == null) {
                        var framingConstellation = new FramingConstellation(constellation, ViewportFoV);
                        framingConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                        ConstellationsInViewport.Add(framingConstellation);
                    } else {
                        viewPortConstellation.RecalculateConstellationPoints(ViewportFoV, drawAnnotations);
                    }
                } else if (viewPortConstellation != null) {
                    ConstellationsInViewport.Remove(viewPortConstellation);
                }
            }

            if (drawAnnotations) {
                foreach (var constellation in ConstellationsInViewport) {
                    constellation.DrawAnnotationsPortable(ctx);
                }
            }
        }

        private void UpdateAndDrawConstellationBoundariesPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            CalculateConstellationBoundaries();
            foreach (var constellationBoundary in ConstellationBoundariesInViewPort) {
                constellationBoundary.DrawPortable(ctx);
            }
        }

        private void UpdateAndDrawGridPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            ClearFrameLineMatrix();
            CalculateFrameLineMatrix();

            FrameLineMatrix.DrawPortable(ctx);
        }

        private void DrawTelescopePortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            if (ViewportFoV.ContainsCoordinates(telescopeCoordinates)) {
                var scopePosition = telescopeCoordinates.XYProjectionPortable(ViewportFoV);
                var center = new ISPointF((float)scopePosition.X, (float)scopePosition.Y);

                var ellipse = new SixLabors.ImageSharp.Drawing.EllipsePolygon(center, 15f);
                SixLabors.ImageSharp.Drawing.Processing.DrawPathExtensions.Draw(ctx, ScopePenPortable, ellipse);

                SixLabors.ImageSharp.Drawing.Processing.DrawLineExtensions.DrawLine(ctx, ScopePenPortable,
                    new ISPointF(center.X, center.Y - 15), new ISPointF(center.X, center.Y - 5));
                SixLabors.ImageSharp.Drawing.Processing.DrawLineExtensions.DrawLine(ctx, ScopePenPortable,
                    new ISPointF(center.X, center.Y + 5), new ISPointF(center.X, center.Y + 15));
                SixLabors.ImageSharp.Drawing.Processing.DrawLineExtensions.DrawLine(ctx, ScopePenPortable,
                    new ISPointF(center.X - 15, center.Y), new ISPointF(center.X - 5, center.Y));
                SixLabors.ImageSharp.Drawing.Processing.DrawLineExtensions.DrawLine(ctx, ScopePenPortable,
                    new ISPointF(center.X + 5, center.Y), new ISPointF(center.X + 15, center.Y));
            }
        }
    }
}