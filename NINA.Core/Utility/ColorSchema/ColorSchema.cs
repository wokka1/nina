#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
#if HAS_WPF
using System.Windows.Media;
#endif
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NINA.Core.Utility.ColorSchema {

    [XmlRoot("ColorSchemas")]
    public class ColorSchemas {

        public ColorSchemas() {
            Items = new List<ColorSchema>();
        }

        [XmlElement("ColorSchema")]
        public List<ColorSchema> Items { get; set; }

        public static ColorSchemas ReadColorSchemas() {
            ColorSchemas schemas = new ColorSchemas();

            schemas.Items.Add(new ColorSchema {
                Name = "Light",
                PrimaryColorPortable = PortableColor.FromHex("#FF000000"),
                SecondaryColorPortable = PortableColor.FromHex("#FF54748c"),
                BorderColorPortable = PortableColor.FromHex("#AABCBCBC"),
                BackgroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF0d3956"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF114f77"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF0B3C5D"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF2190DB"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FFc5d2db"),
                CrosshairColorPortable = PortableColor.FromHex("#FFc5d2db"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF000000")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Classic",
                PrimaryColorPortable = PortableColor.FromHex("#FF000000"),
                SecondaryColorPortable = PortableColor.FromHex("#FF54748c"),
                BorderColorPortable = PortableColor.FromHex("#FFADB2B5"),
                BackgroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF4f4f4f"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF707070"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FFDDDDDD"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FFb8e0f3"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FF000000"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FFF4F4F4"),
                CrosshairColorPortable = PortableColor.FromHex("#FFF4F4F4"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FF000000"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FF000000"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF000000")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Dark",
                PrimaryColorPortable = PortableColor.FromHex("#FF550C18"),
                SecondaryColorPortable = PortableColor.FromHex("#FF1B2A41"),
                BorderColorPortable = PortableColor.FromHex("#FF550C18"),
                BackgroundColorPortable = PortableColor.FromHex("#FF02010A"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF230409"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF2d060d"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF550C18"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF96031A"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FF02010A"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF443730"),
                CrosshairColorPortable = PortableColor.FromHex("#FF443730"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FF02010A"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FF02010A"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF550C18")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Seance",
                PrimaryColorPortable = PortableColor.FromHex("#FF000000"),
                SecondaryColorPortable = PortableColor.FromHex("#FFBE90D4"),
                BorderColorPortable = PortableColor.FromHex("#AAAEA8D3"),
                BackgroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF450d4f"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF5d116b"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF663399"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF9A12B3"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FFaa69bc"),
                CrosshairColorPortable = PortableColor.FromHex("#FFaa69bc"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF000000")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Persian",
                PrimaryColorPortable = PortableColor.FromHex("#FFECF0F1"),
                SecondaryColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                BorderColorPortable = PortableColor.FromHex("#AABCBCBC"),
                BackgroundColorPortable = PortableColor.FromHex("#FF263238"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF2a2c31"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF2c3438"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF00796B"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF00A592"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                CrosshairColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFECF0F1"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFECF0F1"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFECF0F1")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Persian Faint",
                PrimaryColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                SecondaryColorPortable = PortableColor.FromHex("#FF1D2731"),
                BorderColorPortable = PortableColor.FromHex("#AA3F4141"),
                BackgroundColorPortable = PortableColor.FromHex("#FF263238"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF2a2c31"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF2c3438"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF007063"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF00BCA6"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                CrosshairColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFBDC3C7")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "High Contrast",
                PrimaryColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SecondaryColorPortable = PortableColor.FromHex("#FF00b7b1"),
                BorderColorPortable = PortableColor.FromHex("#FFFF9900"),
                BackgroundColorPortable = PortableColor.FromHex("#FF000000"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF2F090D"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF191919"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FFFF0000"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF00b7b1"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF7f7f7f"),
                CrosshairColorPortable = PortableColor.FromHex("#FF7f7f7f"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFFFFFFF")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Black Coral",
                PrimaryColorPortable = PortableColor.FromHex("#FFDEDEE8"),
                SecondaryColorPortable = PortableColor.FromHex("#FF592941"),
                BorderColorPortable = PortableColor.FromHex("#FF656F87"),
                BackgroundColorPortable = PortableColor.FromHex("#FF545E75"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF393f4c"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF4a5368"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF99261a"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FFe5a859"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFF7F7FF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                CrosshairColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFF7F7FF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFF7F7FF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFDEDEE8")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Arsenic",
                PrimaryColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                SecondaryColorPortable = PortableColor.FromHex("#FF82A3A1"),
                BorderColorPortable = PortableColor.FromHex("#AA495963"),
                BackgroundColorPortable = PortableColor.FromHex("#FF394648"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF2a2c31"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF30393a"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF406A79"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF64A6BD"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFF8E9E9"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF696D7D"),
                CrosshairColorPortable = PortableColor.FromHex("#FF696D7D"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFF8E9E9"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFF8E9E9"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFFFFFFF")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Vivid Malachite",
                PrimaryColorPortable = PortableColor.FromHex("#FFECF0F1"),
                SecondaryColorPortable = PortableColor.FromHex("#FF1b3325"),
                BorderColorPortable = PortableColor.FromHex("#FF285238"),
                BackgroundColorPortable = PortableColor.FromHex("#FF34403A"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF2a352f"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF415148"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF138A36"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF04E824"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                CrosshairColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFECF0F1")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Shark",
                PrimaryColorPortable = PortableColor.FromHex("#FFEDEDED"),
                SecondaryColorPortable = PortableColor.FromHex("#FFA9AAAC"),
                BorderColorPortable = PortableColor.FromHex("#AA3E4146"),
                BackgroundColorPortable = PortableColor.FromHex("#FF36393E"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF202225"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF404144"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF2a2c31"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF24252A"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF848484"),
                CrosshairColorPortable = PortableColor.FromHex("#FF848484"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFA9AAAC"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFA9AAAC"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFEDEDED")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Slate",
                PrimaryColorPortable = PortableColor.FromHex("#FFB0B3B9"),
                SecondaryColorPortable = PortableColor.FromHex("#FF32555E"),
                BorderColorPortable = PortableColor.FromHex("#FF3F4141"),
                BackgroundColorPortable = PortableColor.FromHex("#FF1E2129"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF14151A"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF2C2F38"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF163647"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF32555E"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFFFFFFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                CrosshairColorPortable = PortableColor.FromHex("#FF9E9E9E"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFBDC3C7"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFB0B3B9")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Wisteria",
                PrimaryColorPortable = PortableColor.FromHex("#FFECF0F1"),
                SecondaryColorPortable = PortableColor.FromHex("#FF6644AD"),
                BorderColorPortable = PortableColor.FromHex("#AA3F4141"),
                BackgroundColorPortable = PortableColor.FromHex("#FF2D0D25"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF230d1e"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF3d1433"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF8E44AD"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF9B59B6"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFECF0F1"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FFa866c4"),
                CrosshairColorPortable = PortableColor.FromHex("#FFa866c4"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FF5E330B"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFECF0F1"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFECF0F1"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFECF0F1")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Navy",
                PrimaryColorPortable = PortableColor.FromHex("#FF6FC3DF"),
                SecondaryColorPortable = PortableColor.FromHex("#FFE93B19"),
                BorderColorPortable = PortableColor.FromHex("#AA1F3B53"),
                BackgroundColorPortable = PortableColor.FromHex("#FF0C141F"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF10233d"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF0f2138"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF1C314F"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF488093"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFBFEEFF"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF3a5168"),
                CrosshairColorPortable = PortableColor.FromHex("#FF3a5168"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FFE93B19"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FFDB0606"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FF6FC3DF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FF6FC3DF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF6FC3DF")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Dark Nebula",
                PrimaryColorPortable = PortableColor.FromHex("#FFF5F4FA"),
                SecondaryColorPortable = PortableColor.FromHex("#68808080"),
                BorderColorPortable = PortableColor.FromHex("#AA3E4146"),
                BackgroundColorPortable = PortableColor.FromHex("#E1191A1C"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF1E2024"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF404144"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF34373D"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF696C70"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FF6495ED"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF848484"),
                CrosshairColorPortable = PortableColor.FromHex("#FF848484"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FFBA5E07"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFF0F8FF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFF0F8FF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFF5F4FA")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Dichromacy",
                PrimaryColorPortable = PortableColor.FromHex("#FFEAF430"),
                SecondaryColorPortable = PortableColor.FromHex("#FF808080"),
                BorderColorPortable = PortableColor.FromHex("#FF3E4146"),
                BackgroundColorPortable = PortableColor.FromHex("#FF191A1C"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF000000"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF404144"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF34373D"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF106CE6"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FFEAF430"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF848484"),
                CrosshairColorPortable = PortableColor.FromHex("#FF848484"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FFBA5E07"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFEAF430"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFEAF430"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFEAF430")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Custom",
                PrimaryColorPortable = PortableColor.FromHex("#FFF5F4FA"),
                SecondaryColorPortable = PortableColor.FromHex("#68808080"),
                BorderColorPortable = PortableColor.FromHex("#AA3E4146"),
                BackgroundColorPortable = PortableColor.FromHex("#E1191A1C"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF1E2024"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF404144"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF34373D"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF696C70"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FF6495ED"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF848484"),
                CrosshairColorPortable = PortableColor.FromHex("#FF848484"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FFBA5E07"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FF700000"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FFF0F8FF"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FFF0F8FF"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FFF5F4FA")
            });
            schemas.Items.Add(new ColorSchema {
                Name = "Alternative Custom",
                PrimaryColorPortable = PortableColor.FromHex("#FF550C18"),
                SecondaryColorPortable = PortableColor.FromHex("#FF1B2A41"),
                BorderColorPortable = PortableColor.FromHex("#FF550C18"),
                BackgroundColorPortable = PortableColor.FromHex("#FF02010A"),
                SecondaryBackgroundColorPortable = PortableColor.FromHex("#FF230409"),
                TertiaryBackgroundColorPortable = PortableColor.FromHex("#FF2d060d"),
                ButtonBackgroundColorPortable = PortableColor.FromHex("#FF550C18"),
                ButtonBackgroundSelectedColorPortable = PortableColor.FromHex("#FF96031A"),
                ButtonForegroundColorPortable = PortableColor.FromHex("#FF02010A"),
                ButtonForegroundDisabledColorPortable = PortableColor.FromHex("#FF443730"),
                CrosshairColorPortable = PortableColor.FromHex("#FF443730"),
                NotificationWarningColorPortable = PortableColor.FromHex("#FFF5A300"),
                NotificationErrorColorPortable = PortableColor.FromHex("#FFDB0606"),
                NotificationWarningTextColorPortable = PortableColor.FromHex("#FF02010A"),
                NotificationErrorTextColorPortable = PortableColor.FromHex("#FF02010A"),
                SequencerExpressionTextColorPortable = PortableColor.FromHex("#FF550C18")
            });

            return schemas;
        }
    }

    [Serializable()]
    [DataContract]
    public class ColorSchema : SerializableINPC {
        private PortableColor primaryColor;
        private PortableColor secondaryColor;
        private PortableColor borderColor;
        private PortableColor backgroundColor;
        private PortableColor secondaryBackgroundColor;
        private PortableColor tertiaryBackgroundColor;
        private PortableColor buttonBackgroundColor;
        private PortableColor buttonBackgroundSelectedColor;
        private PortableColor buttonForegroundColor;
        private PortableColor buttonForegroundDisabledColor;
        private PortableColor crosshairColor;
        private PortableColor notificationWarningColor;
        private PortableColor notificationErrorColor;
        private PortableColor notificationWarningTextColor;
        private PortableColor notificationErrorTextColor;
        private PortableColor sequencerExpressionTextColor;

        [DataMember]
        public String Name { get; set; }

#if HAS_WPF
        [DataMember]
        public Color PrimaryColor {
            get => Color.FromArgb(primaryColor.A, primaryColor.R, primaryColor.G, primaryColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (primaryColor != portable) {
                    primaryColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor PrimaryColorPortable {
            get => primaryColor;
            set {
                if (primaryColor != value) {
                    primaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color SecondaryColor {
            get => Color.FromArgb(secondaryColor.A, secondaryColor.R, secondaryColor.G, secondaryColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (secondaryColor != portable) {
                    secondaryColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor SecondaryColorPortable {
            get => secondaryColor;
            set {
                if (secondaryColor != value) {
                    secondaryColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color BorderColor {
            get => Color.FromArgb(borderColor.A, borderColor.R, borderColor.G, borderColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (borderColor != portable) {
                    borderColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor BorderColorPortable {
            get => borderColor;
            set {
                if (borderColor != value) {
                    borderColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color BackgroundColor {
            get => Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (backgroundColor != portable) {
                    backgroundColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor BackgroundColorPortable {
            get => backgroundColor;
            set {
                if (backgroundColor != value) {
                    backgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color SecondaryBackgroundColor {
            get => Color.FromArgb(secondaryBackgroundColor.A, secondaryBackgroundColor.R, secondaryBackgroundColor.G, secondaryBackgroundColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (secondaryBackgroundColor != portable) {
                    secondaryBackgroundColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor SecondaryBackgroundColorPortable {
            get => secondaryBackgroundColor;
            set {
                if (secondaryBackgroundColor != value) {
                    secondaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color TertiaryBackgroundColor {
            get => Color.FromArgb(tertiaryBackgroundColor.A, tertiaryBackgroundColor.R, tertiaryBackgroundColor.G, tertiaryBackgroundColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (tertiaryBackgroundColor != portable) {
                    tertiaryBackgroundColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor TertiaryBackgroundColorPortable {
            get => tertiaryBackgroundColor;
            set {
                if (tertiaryBackgroundColor != value) {
                    tertiaryBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color ButtonBackgroundColor {
            get => Color.FromArgb(buttonBackgroundColor.A, buttonBackgroundColor.R, buttonBackgroundColor.G, buttonBackgroundColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (buttonBackgroundColor != portable) {
                    buttonBackgroundColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor ButtonBackgroundColorPortable {
            get => buttonBackgroundColor;
            set {
                if (buttonBackgroundColor != value) {
                    buttonBackgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color ButtonBackgroundSelectedColor {
            get => Color.FromArgb(buttonBackgroundSelectedColor.A, buttonBackgroundSelectedColor.R, buttonBackgroundSelectedColor.G, buttonBackgroundSelectedColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (buttonBackgroundSelectedColor != portable) {
                    buttonBackgroundSelectedColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor ButtonBackgroundSelectedColorPortable {
            get => buttonBackgroundSelectedColor;
            set {
                if (buttonBackgroundSelectedColor != value) {
                    buttonBackgroundSelectedColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color ButtonForegroundColor {
            get => Color.FromArgb(buttonForegroundColor.A, buttonForegroundColor.R, buttonForegroundColor.G, buttonForegroundColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (buttonForegroundColor != portable) {
                    buttonForegroundColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor ButtonForegroundColorPortable {
            get => buttonForegroundColor;
            set {
                if (buttonForegroundColor != value) {
                    buttonForegroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color ButtonForegroundDisabledColor {
            get => Color.FromArgb(buttonForegroundDisabledColor.A, buttonForegroundDisabledColor.R, buttonForegroundDisabledColor.G, buttonForegroundDisabledColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (buttonForegroundDisabledColor != portable) {
                    buttonForegroundDisabledColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor ButtonForegroundDisabledColorPortable {
            get => buttonForegroundDisabledColor;
            set {
                if (buttonForegroundDisabledColor != value) {
                    buttonForegroundDisabledColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color CrosshairColor {
            get {
                if (crosshairColor == default) {
                    crosshairColor = new PortableColor(0xFF, 0x84, 0x84, 0x84);
                }
                return Color.FromArgb(crosshairColor.A, crosshairColor.R, crosshairColor.G, crosshairColor.B);
            }
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (portable == default) {
                    portable = new PortableColor(0xFF, 0x84, 0x84, 0x84);
                }
                if (crosshairColor != portable) {
                    crosshairColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor CrosshairColorPortable {
            get {
                if (crosshairColor == default) {
                    crosshairColor = new PortableColor(0xFF, 0x84, 0x84, 0x84);
                }
                return crosshairColor;
            }
            set {
                var portable = value;
                if (portable == default) {
                    portable = new PortableColor(0xFF, 0x84, 0x84, 0x84);
                }
                if (crosshairColor != portable) {
                    crosshairColor = portable;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color NotificationWarningColor {
            get => Color.FromArgb(notificationWarningColor.A, notificationWarningColor.R, notificationWarningColor.G, notificationWarningColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (notificationWarningColor != portable) {
                    notificationWarningColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor NotificationWarningColorPortable {
            get => notificationWarningColor;
            set {
                if (notificationWarningColor != value) {
                    notificationWarningColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color NotificationErrorColor {
            get => Color.FromArgb(notificationErrorColor.A, notificationErrorColor.R, notificationErrorColor.G, notificationErrorColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (notificationErrorColor != portable) {
                    notificationErrorColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor NotificationErrorColorPortable {
            get => notificationErrorColor;
            set {
                if (notificationErrorColor != value) {
                    notificationErrorColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color NotificationWarningTextColor {
            get => Color.FromArgb(notificationWarningTextColor.A, notificationWarningTextColor.R, notificationWarningTextColor.G, notificationWarningTextColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (notificationWarningTextColor != portable) {
                    notificationWarningTextColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor NotificationWarningTextColorPortable {
            get => notificationWarningTextColor;
            set {
                if (notificationWarningTextColor != value) {
                    notificationWarningTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color NotificationErrorTextColor {
            get => Color.FromArgb(notificationErrorTextColor.A, notificationErrorTextColor.R, notificationErrorTextColor.G, notificationErrorTextColor.B);
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (notificationErrorTextColor != portable) {
                    notificationErrorTextColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor NotificationErrorTextColorPortable {
            get => notificationErrorTextColor;
            set {
                if (notificationErrorTextColor != value) {
                    notificationErrorTextColor = value;
                    RaisePropertyChanged();
                }
            }
        }

#if HAS_WPF
        [DataMember]
        public Color SequencerExpressionTextColor {
            get {
                if (sequencerExpressionTextColor == default) {
                    sequencerExpressionTextColor = new PortableColor(0xFF, 0xF5, 0xF4, 0xFA);
                }
                return Color.FromArgb(sequencerExpressionTextColor.A, sequencerExpressionTextColor.R, sequencerExpressionTextColor.G, sequencerExpressionTextColor.B);
            }
            set {
                var portable = new PortableColor(value.A, value.R, value.G, value.B);
                if (portable == default) {
                    portable = new PortableColor(0xFF, 0xF5, 0xF4, 0xFA);
                }
                if (sequencerExpressionTextColor != portable) {
                    sequencerExpressionTextColor = portable;
                    RaisePropertyChanged();
                }
            }
        }
#endif

        [XmlIgnore]
        [IgnoreDataMember]
        public PortableColor SequencerExpressionTextColorPortable {
            get {
                if (sequencerExpressionTextColor == default) {
                    sequencerExpressionTextColor = new PortableColor(0xFF, 0xF5, 0xF4, 0xFA);
                }
                return sequencerExpressionTextColor;
            }
            set {
                var portable = value;
                if (portable == default) {
                    portable = new PortableColor(0xFF, 0xF5, 0xF4, 0xFA);
                }
                if (sequencerExpressionTextColor != portable) {
                    sequencerExpressionTextColor = portable;
                    RaisePropertyChanged();
                }
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public bool IsEditable => Name == "Custom" || Name == "Alternative Custom";

        public ColorSchema() {
        }
    }
}