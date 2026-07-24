#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Drawing;

namespace NINA.WPF.Base.Model.FramingAssistant {

    public class FramingConstellationBoundary {
        public List<PointF> Points = new List<PointF>();

        // Real runtime finding (2026-07-24): boundaryPen's field initializer runs in this class's implicit
        // static constructor the instant any member is touched, including DrawPortable - needs libgdiplus,
        // not present on macOS/Linux - so it's gated together with its only consumer, Draw(Graphics).
#if HAS_WPF
        private static Pen boundaryPen = new Pen(Color.FromArgb(128, Color.Khaki), 0.1f);

        public void Draw(Graphics g) {
            if (this.Points.Count > 1) {
                g.DrawPolygon(boundaryPen, this.Points.ToArray());
            }
        }
#endif

        private static readonly SixLabors.ImageSharp.Drawing.Processing.SolidPen boundaryPenPortable =
            new SixLabors.ImageSharp.Drawing.Processing.SolidPen(SixLabors.ImageSharp.Color.Khaki.WithAlpha(128f / 255f), 0.1f);

        public void DrawPortable(SixLabors.ImageSharp.Processing.IImageProcessingContext ctx) {
            if (this.Points.Count > 1) {
                var points = this.Points.ConvertAll(p => new SixLabors.ImageSharp.PointF(p.X, p.Y)).ToArray();
                SixLabors.ImageSharp.Drawing.Processing.DrawPolygonExtensions.DrawPolygon(ctx, boundaryPenPortable, points);
            }
        }
    }
}