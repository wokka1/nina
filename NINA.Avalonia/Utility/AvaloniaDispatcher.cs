#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"

using System;
using Avalonia.Threading;
using IDispatcher = NINA.Core.Interfaces.Utility.IDispatcher;

namespace NINA.Avalonia.Utility {

    /// <summary>
    /// IDispatcher implementation backed by Avalonia's Dispatcher.UIThread. Set as
    /// NINA.Core.Utility.DispatcherProvider.Current during app startup, mirroring
    /// NINA.WPF.Base.Utility.WpfDispatcher's role in the WPF app.
    ///
    /// Aliases NINA.Core.Interfaces.Utility.IDispatcher because Avalonia.Threading also defines
    /// its own IDispatcher type - a real, unavoidable name collision between the two frameworks,
    /// not a typo.
    /// </summary>
    public class AvaloniaDispatcher : IDispatcher {

        public bool CheckAccess() {
            return Dispatcher.UIThread.CheckAccess();
        }

        public void Invoke(Action action) {
            if (Dispatcher.UIThread.CheckAccess()) {
                action();
            } else {
                Dispatcher.UIThread.Invoke(action);
            }
        }

        public void BeginInvoke(Action action) {
            if (Dispatcher.UIThread.CheckAccess()) {
                action();
            } else {
                Dispatcher.UIThread.Post(action);
            }
        }
    }
}
