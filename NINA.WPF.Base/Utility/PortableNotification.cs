#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

#if !HAS_WPF

namespace NINA.Core.Utility.Notification {

    /// <summary>
    /// NINA.Core.Utility.Notification.Notification is a WPF toast-popup overlay and is excluded
    /// from NINA.Core's portable (net10.0) build - see NINA.Core.csproj. The equipment view models
    /// in this project call it pervasively purely for fire-and-forget user feedback, so rather than
    /// touching every call site individually, this is a minimal no-op stand-in with the same static
    /// API surface actually used here. It intentionally does not attempt to reproduce the popup UI -
    /// that will need a real Avalonia-native notification surface in a later phase.
    /// </summary>
    public static class Notification {

        public static void ShowInformation(string message) {
            Logger.Debug($"Notification.ShowInformation (no-op outside WPF): {message}");
        }

        public static void ShowSuccess(string message) {
            Logger.Debug($"Notification.ShowSuccess (no-op outside WPF): {message}");
        }

        public static void ShowWarning(string message) {
            Logger.Debug($"Notification.ShowWarning (no-op outside WPF): {message}");
        }

        public static void ShowError(string message) {
            Logger.Debug($"Notification.ShowError (no-op outside WPF): {message}");
        }
    }
}

#endif
