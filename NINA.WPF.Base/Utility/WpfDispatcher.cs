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
using System.Windows;
using System.Windows.Threading;
using NINA.Core.Interfaces.Utility;

namespace NINA.WPF.Base.Utility {

    /// <summary>
    /// IDispatcher implementation backed by WPF's Application.Current.Dispatcher. Set as
    /// NINA.Core.Utility.DispatcherProvider.Current during app startup.
    /// </summary>
    public class WpfDispatcher : IDispatcher {

        private Dispatcher Dispatcher => Application.Current?.Dispatcher;

        public bool CheckAccess() {
            var dispatcher = Dispatcher;
            return dispatcher == null || dispatcher.CheckAccess();
        }

        public void Invoke(Action action) {
            var dispatcher = Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) {
                action();
            } else {
                dispatcher.Invoke(action);
            }
        }

        public void BeginInvoke(Action action) {
            var dispatcher = Dispatcher;
            if (dispatcher == null) {
                action();
            } else {
                dispatcher.BeginInvoke(action);
            }
        }
    }
}
