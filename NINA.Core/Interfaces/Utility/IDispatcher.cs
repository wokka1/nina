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

namespace NINA.Core.Interfaces.Utility {

    /// <summary>
    /// Abstraction over the UI frameworks concept of a dispatcher/synchronization context, so
    /// UI-thread marshaling code in NINA.Core does not have to reference a specific UI framework
    /// (e.g. WPF's System.Windows.Threading.Dispatcher) directly.
    /// </summary>
    public interface IDispatcher {

        /// <summary>
        /// True when the calling thread is already the UI thread this dispatcher marshals to.
        /// </summary>
        bool CheckAccess();

        /// <summary>
        /// Runs the action on the UI thread and blocks the calling thread until it completes.
        /// If already on the UI thread, runs synchronously in place.
        /// </summary>
        void Invoke(Action action);

        /// <summary>
        /// Queues the action to run on the UI thread and returns immediately without waiting
        /// for it to complete.
        /// </summary>
        void BeginInvoke(Action action);
    }
}
