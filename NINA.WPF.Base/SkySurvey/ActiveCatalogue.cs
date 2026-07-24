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
using NINA.Core.Utility;

namespace NINA.WPF.Base.SkySurvey {

    /// <summary>
    /// Moved out of ISkyMapAnnotator.cs (still WPF-only, excluded from net10.0) since this class itself has no
    /// WPF dependency at all and SkyMapAnnotator's ActiveCatalogues (populated in the shared, portable
    /// Initialize()/GetDeepSkyObjectsForViewport() path) needs it to exist on both TFMs.
    /// </summary>
    public partial class ActiveCatalogue : BaseINPC {
        [ObservableProperty]
        private string name;
        [ObservableProperty]
        private bool active;

        public ActiveCatalogue(string name, bool active) {
            Name = name;
            Active = active;
        }
    }
}
