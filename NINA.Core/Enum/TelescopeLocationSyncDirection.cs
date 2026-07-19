using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Enum {
#if HAS_WPF
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
#endif
    public enum TelescopeLocationSyncDirection {
        [Description("Lbl_TelescopeLocationSyncDirection_Prompt")]
        PROMPT,
        [Description("Lbl_TelescopeLocationSyncDirection_ToApplication")]
        TOAPPLICATION,
        [Description("Lbl_TelescopeLocationSyncDirection_ToTelescope")]
        TOTELESCOPE,
        [Description("Lbl_TelescopeLocationSyncDirection_NoSync")]
        NOSYNC
    }
}
