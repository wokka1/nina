#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Interfaces.Utility;

namespace NINA.Core.Utility {

    /// <summary>
    /// Ambient holder for the active IDispatcher, set once by the application's composition root
    /// at startup. Exists so UI-thread-marshaling types that are constructed all over the codebase
    /// via "new" (e.g. AsyncObservableCollection&lt;T&gt;) rather than through DI can still reach a
    /// dispatcher without NINA.Core referencing a specific UI framework.
    /// </summary>
    public static class DispatcherProvider {

        /// <summary>
        /// The active dispatcher for the running application. Null until the composition root sets
        /// it (e.g. WPF's App.OnStartup), which mirrors how WPF's own Application.Current is null
        /// until the framework sets it up.
        /// </summary>
        public static IDispatcher Current { get; set; }
    }
}
