#region "copyright"

/*
    Copyright � 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using SixLabors.Fonts;
using System.Collections.Generic;
using System.Linq;

namespace NINA.WPF.Base.SkySurvey.Portable {

    /// <summary>
    /// The original Framing Assistant drawing code creates GDI+ fonts by name ("Segoe UI") and relies on it
    /// being a real installed Windows system font. SixLabors.Fonts' SystemFonts does the equivalent lookup
    /// against whatever fonts are actually installed on the running platform, but "Segoe UI" specifically is
    /// a Microsoft font unlikely to exist on macOS/Linux - so this resolves the first available family from a
    /// fallback chain instead of a single hardcoded name.
    /// A real, documented simplification (not attempted to be pixel-identical to Windows' Segoe UI metrics) -
    /// same "additive, honestly scoped" treatment as other platform gaps found elsewhere in this project.
    /// </summary>
    public static class PortableFonts {
        private static readonly string[] PreferredFamilies = {
            "Segoe UI", "Helvetica Neue", "Helvetica", "Arial", "DejaVu Sans", "Liberation Sans", "Noto Sans"
        };

        private static readonly Dictionary<float, Dictionary<FontStyle, Font>> cache = new();
        private static FontFamily? resolvedFamily;

        private static FontFamily ResolveFamily() {
            if (resolvedFamily != null) {
                return resolvedFamily.Value;
            }

            foreach (var name in PreferredFamilies) {
                if (SystemFonts.Collection.TryGet(name, out var family)) {
                    resolvedFamily = family;
                    return family;
                }
            }

            // Last resort: whatever the platform reports first, rather than throwing when none of the
            // preferred names are installed.
            var any = SystemFonts.Collection.Families.FirstOrDefault();
            resolvedFamily = any;
            return any;
        }

        public static Font Get(float size, FontStyle style) {
            if (!cache.TryGetValue(size, out var byStyle)) {
                byStyle = new Dictionary<FontStyle, Font>();
                cache[size] = byStyle;
            }
            if (!byStyle.TryGetValue(style, out var font)) {
                font = ResolveFamily().CreateFont(size, style);
                byStyle[style] = font;
            }
            return font;
        }
    }
}
