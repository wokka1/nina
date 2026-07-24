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
using System.Drawing;
using System.IO;
using System.Linq;
using ISPointF = SixLabors.ImageSharp.PointF;
using NINA.WPF.Base.SkySurvey.Portable;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace NINA.WPF.Base.Model.FramingAssistant {

    public class FramingDSO {
        private const int DSO_DEFAULT_SIZE = 30;

        private double arcSecWidth;
        private double arcSecHeight;
        private readonly double sizeWidth;
        private readonly double sizeHeight;
        private readonly float positionAngle;
        private Coordinates coordinates;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a ViewportFoV and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="viewport">The viewport of the offending DSO</param>
        public FramingDSO(DeepSkyObject dso, ViewportFoV viewport) {
            dsoType = dso.DSOType;
            arcSecWidth = viewport.ArcSecWidth;
            arcSecHeight = viewport.ArcSecHeight;

            if (dso.Size != null && dso.Size >= arcSecWidth) {
                sizeWidth = dso.Size.Value;
            } else {
                sizeWidth = DSO_DEFAULT_SIZE;
            }

            if(dso.PositionAngle != null) {
                positionAngle = (90 - (float)dso.PositionAngle.Degree);

                if (dso.SizeMin != null && dso.SizeMin >= arcSecHeight) {
                    sizeHeight = dso.SizeMin.Value;
                } else {
                    sizeHeight = DSO_DEFAULT_SIZE;
                }
            } else {
                positionAngle = 0f;
                sizeHeight = sizeWidth;
            }

            Id = dso.Id;
            Name1 = dso.Name;
            Name2 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("M "));
            Name3 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("NGC "));

            if (Name3 != null && Name1 == Name3.Replace(" ", "")) {
                Name1 = null;
            }

            if (Name1 == null && Name2 == null) {
                Name1 = Name3;
                Name3 = null;
            }

            if (Name1 == null && Name2 != null) {
                Name1 = Name2;
                Name2 = Name3;
                Name3 = null;
            }

            coordinates = dso.Coordinates;

            RecalculateTopLeft(viewport);
        }

        public PointF TextPosition { get; private set; }

        public void RecalculateTopLeft(ViewportFoV reference) {
            ViewPortCenter = reference.CenterCoordinates;
            ViewPortCenterPoint = reference.ViewPortCenterPointPortable;
            ViewPortRotation = reference.Rotation;
            CenterPoint = coordinates.XYProjectionPortable(reference);
            arcSecWidth = reference.ArcSecWidth;
            arcSecHeight = reference.ArcSecHeight;
            TextPosition = new PointF((float)CenterPoint.X, (float)(CenterPoint.Y + RadiusHeight + 5));
        }

        public double RadiusWidth => (sizeWidth / arcSecWidth) / 2;

        public double RadiusHeight => (sizeHeight / arcSecHeight) / 2;

        /// <summary>
        /// Point2d (WPF-free) rather than System.Windows.Point - used identically by both the original
        /// System.Drawing-based Draw() (just reads .X/.Y, never needed WPF's Point type specifically) and
        /// the portable DrawPortable(), so no HAS_WPF gating/duplication needed for these two fields.
        /// </summary>
        public Point2d CenterPoint { get; private set; }
        public Coordinates ViewPortCenter { get; private set; }
        public Point2d ViewPortCenterPoint { get; private set; }
        public double ViewPortRotation { get; private set; }

        public string Id { get; }
        public string Name1 { get; }
        public string Name2 { get; }
        public string Name3 { get; }

        private string dsoType;

        // Real runtime finding (2026-07-24): plain field initializers run in this class's implicit static
        // constructor the instant ANY member is touched (including DrawPortable), so these (and their only
        // consumer, the original Draw(Graphics) below) must be gated - not just for compile-time type
        // availability, but because System.Drawing.SolidBrush/Pen/Font construction needs libgdiplus at runtime,
        // which isn't present on macOS/Linux.
#if HAS_WPF
        private static SolidBrush dsoFillColorBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255));

        private static Pen galxyStrokePen = new Pen(Color.FromArgb(128, Color.BurlyWood));
        private static SolidBrush galxyFontColorBrush = new SolidBrush(Color.BurlyWood);

        private static Pen nebulaStrokePen = new Pen(Color.FromArgb(128, Color.Violet));
        private static SolidBrush nebulaFontColorBrush = new SolidBrush(Color.Violet);

        private static Pen plNebulaStrokePen = new Pen(Color.FromArgb(128, Color.Cyan));
        private static SolidBrush plNebulaFontColorBrush = new SolidBrush(Color.Cyan);

        private static Pen gloclStrokePen = new Pen(Color.FromArgb(128, Color.Yellow));
        private static SolidBrush gloclFontColorBrush = new SolidBrush(Color.Yellow);

        private static Pen dsoDefaultStrokePen = new Pen(Color.FromArgb(127, 255, 255, 255));
        private static SolidBrush dsoDefaultFontColorBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        private static Font dsoFont = new Font("Segoe UI", 10, System.Drawing.FontStyle.Regular);

        public void Draw(System.Drawing.Graphics g) {
            Pen dsoPen;
            SolidBrush dsoSolidBrush;
            switch (dsoType) {
                case "GALXY":
                case "GALCL":
                    dsoPen = galxyStrokePen;
                    dsoSolidBrush = galxyFontColorBrush;
                    break;

                case "PLNNB":
                    dsoPen = plNebulaStrokePen;
                    dsoSolidBrush = plNebulaFontColorBrush;
                    break;

                case "BRTNB":
                case "CL+NB":
                    dsoPen = nebulaStrokePen;
                    dsoSolidBrush = nebulaFontColorBrush;
                    break;

                case "GLOCL":
                    dsoPen = gloclStrokePen;
                    dsoSolidBrush = gloclFontColorBrush;
                    break;

                default:
                    dsoPen = dsoDefaultStrokePen;
                    dsoSolidBrush = dsoDefaultFontColorBrush;
                    break;
            }

            var panelDeltaX = CenterPoint.X - ViewPortCenterPoint.X;
            var panelDeltaY = CenterPoint.Y - ViewPortCenterPoint.Y;
            var referenceCenter = ViewPortCenter.Shift(panelDeltaX < 1E-10 ? 1 : 0, panelDeltaY, ViewPortRotation, arcSecWidth, arcSecHeight);

            
            
            
            float adjustedAngle = positionAngle;            
            if (Math.Abs(ViewPortCenter.RA - coordinates.RA) > 1E-13 || Math.Abs(ViewPortCenter.Dec - coordinates.Dec) > 1E-13) {                
                adjustedAngle = positionAngle - ( 90 - ((float)AstroUtil.CalculatePositionAngle(referenceCenter.RADegrees, coordinates.RADegrees, referenceCenter.Dec, coordinates.Dec) /*+ (float)ViewPortRotation*/)) ;
            }           

            g.TranslateTransform((float)CenterPoint.X, (float)CenterPoint.Y);
            g.RotateTransform(adjustedAngle);
            g.TranslateTransform(-(float)CenterPoint.X, -(float)CenterPoint.Y);
            g.FillEllipse(dsoFillColorBrush, (float)(this.CenterPoint.X - this.RadiusWidth), (float)(this.CenterPoint.Y - this.RadiusHeight),
                    (float)(this.RadiusWidth * 2), (float)(this.RadiusHeight * 2));
            g.DrawEllipse(dsoPen, (float)(this.CenterPoint.X - this.RadiusWidth), (float)(this.CenterPoint.Y - this.RadiusHeight),
                (float)(this.RadiusWidth * 2), (float)(this.RadiusHeight * 2));
            g.ResetTransform();

            var size1 = g.MeasureString(this.Name1, dsoFont);
            g.DrawString(this.Name1, dsoFont, dsoSolidBrush, this.TextPosition.X - size1.Width / 2, (float)(this.TextPosition.Y));
            if (this.Name2 != null) {
                var size2 = g.MeasureString(this.Name2, dsoFont);
                g.DrawString(this.Name2, dsoFont, dsoSolidBrush, this.TextPosition.X - size2.Width / 2, (float)(this.TextPosition.Y + size1.Height + 2));
                if (this.Name3 != null) {
                    var size3 = g.MeasureString(this.Name3, dsoFont);
                    g.DrawString(this.Name3, dsoFont, dsoSolidBrush, this.TextPosition.X - size3.Width / 2, (float)(this.TextPosition.Y + size1.Height + 2 + size2.Height + 2));
                }
            }
        }
#endif

        private static readonly SixLabors.ImageSharp.Color dsoFillColorPortable = SixLabors.ImageSharp.Color.FromRgba(255, 255, 255, 10);

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen galxyStrokePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.BurlyWood.WithAlpha(128f / 255f));
        private static readonly SixLabors.ImageSharp.Color galxyFontColorPortable = SixLabors.ImageSharp.Color.BurlyWood;

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen nebulaStrokePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.Violet.WithAlpha(128f / 255f));
        private static readonly SixLabors.ImageSharp.Color nebulaFontColorPortable = SixLabors.ImageSharp.Color.Violet;

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen plNebulaStrokePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.Cyan.WithAlpha(128f / 255f));
        private static readonly SixLabors.ImageSharp.Color plNebulaFontColorPortable = SixLabors.ImageSharp.Color.Cyan;

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen gloclStrokePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.Yellow.WithAlpha(128f / 255f));
        private static readonly SixLabors.ImageSharp.Color gloclFontColorPortable = SixLabors.ImageSharp.Color.Yellow;

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen dsoDefaultStrokePenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.FromRgba(255, 255, 255, 127));
        private static readonly SixLabors.ImageSharp.Color dsoDefaultFontColorPortable = SixLabors.ImageSharp.Color.FromRgba(255, 255, 255, 255);

        private static readonly SixLabors.Fonts.Font dsoFontPortable = NINA.WPF.Base.SkySurvey.Portable.PortableFonts.Get(10, SixLabors.Fonts.FontStyle.Regular);

        /// <summary>
        /// Portable (ImageSharp-based) equivalent of Draw(Graphics). Only the ellipse marker is rotated by the DSO's
        /// position angle - the original also draws its name labels after a ResetTransform(), i.e. unrotated - so this
        /// bakes the rotation into the ellipse's own path via PortableDrawingUtility.RotateAround rather than pushing/
        /// popping a graphics-wide transform, and draws the text separately afterward exactly like the original does.
        /// </summary>
        public void DrawPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            SixLabors.ImageSharp.Drawing.Processing.SolidPen dsoPen;
            SixLabors.ImageSharp.Color dsoColor;
            switch (dsoType) {
                case "GALXY":
                case "GALCL":
                    dsoPen = galxyStrokePenPortable;
                    dsoColor = galxyFontColorPortable;
                    break;

                case "PLNNB":
                    dsoPen = plNebulaStrokePenPortable;
                    dsoColor = plNebulaFontColorPortable;
                    break;

                case "BRTNB":
                case "CL+NB":
                    dsoPen = nebulaStrokePenPortable;
                    dsoColor = nebulaFontColorPortable;
                    break;

                case "GLOCL":
                    dsoPen = gloclStrokePenPortable;
                    dsoColor = gloclFontColorPortable;
                    break;

                default:
                    dsoPen = dsoDefaultStrokePenPortable;
                    dsoColor = dsoDefaultFontColorPortable;
                    break;
            }

            var panelDeltaX = CenterPoint.X - ViewPortCenterPoint.X;
            var panelDeltaY = CenterPoint.Y - ViewPortCenterPoint.Y;
            var referenceCenter = ViewPortCenter.Shift(panelDeltaX < 1E-10 ? 1 : 0, panelDeltaY, ViewPortRotation, arcSecWidth, arcSecHeight);

            float adjustedAngle = positionAngle;
            if (Math.Abs(ViewPortCenter.RA - coordinates.RA) > 1E-13 || Math.Abs(ViewPortCenter.Dec - coordinates.Dec) > 1E-13) {
                adjustedAngle = positionAngle - (90 - ((float)AstroUtil.CalculatePositionAngle(referenceCenter.RADegrees, coordinates.RADegrees, referenceCenter.Dec, coordinates.Dec)));
            }

            var pivot = new ISPointF((float)this.CenterPoint.X, (float)this.CenterPoint.Y);
            var ellipse = new SixLabors.ImageSharp.Drawing.EllipsePolygon(pivot, new SixLabors.ImageSharp.SizeF((float)(this.RadiusWidth * 2), (float)(this.RadiusHeight * 2)));
            var rotated = PortableDrawingUtility.RotateAround(ellipse, pivot, adjustedAngle);

            SixLabors.ImageSharp.Drawing.Processing.FillPathExtensions.Fill(ctx, dsoFillColorPortable, rotated);
            SixLabors.ImageSharp.Drawing.Processing.DrawPathExtensions.Draw(ctx, dsoPen, rotated);

            var textPos = new ISPointF((float)this.TextPosition.X, (float)this.TextPosition.Y);
            var size1 = SixLabors.Fonts.TextMeasurer.MeasureSize(this.Name1, new SixLabors.Fonts.TextOptions(dsoFontPortable));
            SixLabors.ImageSharp.Drawing.Processing.DrawTextExtensions.DrawText(ctx, this.Name1, dsoFontPortable, dsoColor, new ISPointF(textPos.X - size1.Width / 2, textPos.Y));
            if (this.Name2 != null) {
                var size2 = SixLabors.Fonts.TextMeasurer.MeasureSize(this.Name2, new SixLabors.Fonts.TextOptions(dsoFontPortable));
                SixLabors.ImageSharp.Drawing.Processing.DrawTextExtensions.DrawText(ctx, this.Name2, dsoFontPortable, dsoColor, new ISPointF(textPos.X - size2.Width / 2, textPos.Y + size1.Height + 2));
                if (this.Name3 != null) {
                    var size3 = SixLabors.Fonts.TextMeasurer.MeasureSize(this.Name3, new SixLabors.Fonts.TextOptions(dsoFontPortable));
                    SixLabors.ImageSharp.Drawing.Processing.DrawTextExtensions.DrawText(ctx, this.Name3, dsoFontPortable, dsoColor, new ISPointF(textPos.X - size3.Width / 2, textPos.Y + size1.Height + 2 + size2.Height + 2));
                }
            }
        }
    }
}