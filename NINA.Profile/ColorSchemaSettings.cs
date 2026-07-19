#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.ColorSchema;
using NINA.Profile.Interfaces;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    [KnownType(typeof(ColorSchema))]
    public class ColorSchemaSettings : Settings, IColorSchemaSettings {

        public ColorSchemaSettings() : base() {
            SetDefaultValues();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context) {
            Initialize();
        }

        private ColorSchema _altColorSchema;

        [DataMember]
        public ColorSchema AltColorSchema {
            get => _altColorSchema;
            set {
                if (_altColorSchema != value) {
                    if (_altColorSchema != null) {
                        _altColorSchema.PropertyChanged -= _colorSchema_PropertyChanged;
                    }
                    _altColorSchema = value;
                    _altColorSchema.PropertyChanged += _colorSchema_PropertyChanged;
                    RaisePropertyChanged();
                }
            }
        }

        private ColorSchema _colorSchema;

        [DataMember]
        public ColorSchema ColorSchema {
            get => _colorSchema;
            set {
                if (_colorSchema != value) {
                    if (_colorSchema != null) {
                        _colorSchema.PropertyChanged -= _colorSchema_PropertyChanged;
                    }
                    _colorSchema = value;
                    _colorSchema.PropertyChanged += _colorSchema_PropertyChanged;
                    RaisePropertyChanged();
                }
            }
        }

        private void _colorSchema_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            RaisePropertyChanged("Settings");
        }

        public ColorSchemas ColorSchemas { get; set; }

        private void Initialize() {
            var index = ColorSchemas.Items.FindIndex(x => x.Name == ColorSchema.Name);
            if (index > -1) {
                if (ColorSchema.Name == "Custom" || ColorSchema.Name == "Alternative Custom") {
                    // Apply custom colors to the custom schema
                    ColorSchemas.Items[index] = ColorSchema;
                } else {
                    // Ensure the reference is the same as in the collection to get the combobox populated
                    ColorSchema = ColorSchemas.Items[index];
                }
            }

            var index2 = ColorSchemas.Items.FindIndex(x => x.Name == AltColorSchema.Name);
            if (index2 > -1) {
                if (ColorSchema.Name == "Custom" || ColorSchema.Name == "Alternative Custom") {
                    // Apply custom colors to the custom schema
                    ColorSchemas.Items[index2] = AltColorSchema;
                } else {
                    // Ensure the reference is the same as in the collection to get the combobox populated
                    AltColorSchema = ColorSchemas.Items[index2];
                }
            }
        }

        protected override void SetDefaultValues() {
            ColorSchemas = ColorSchemas.ReadColorSchemas();
            ColorSchema = ColorSchemas.Items.Where(x => x.Name == "Persian Faint").FirstOrDefault();

            AltColorSchema = ColorSchemas.Items.Where(x => x.Name == "Dark").FirstOrDefault();
        }

        public void ToggleSchema() {
            var tmp = ColorSchema;
            ColorSchema = AltColorSchema;
            AltColorSchema = tmp;
        }

        public void CopyToCustom() {
            var schema = ColorSchemas.Items.Where((x) => x.Name == "Custom").First();

            schema.PrimaryColorPortable = ColorSchema.PrimaryColorPortable;
            schema.SecondaryColorPortable = ColorSchema.SecondaryColorPortable;
            schema.BorderColorPortable = ColorSchema.BorderColorPortable;
            schema.BackgroundColorPortable = ColorSchema.BackgroundColorPortable;
            schema.SecondaryBackgroundColorPortable = ColorSchema.SecondaryBackgroundColorPortable;
            schema.TertiaryBackgroundColorPortable = ColorSchema.TertiaryBackgroundColorPortable;
            schema.ButtonBackgroundColorPortable = ColorSchema.ButtonBackgroundColorPortable;
            schema.ButtonBackgroundSelectedColorPortable = ColorSchema.ButtonBackgroundSelectedColorPortable;
            schema.ButtonForegroundColorPortable = ColorSchema.ButtonForegroundColorPortable;
            schema.ButtonForegroundDisabledColorPortable = ColorSchema.ButtonForegroundDisabledColorPortable;
            schema.CrosshairColorPortable = ColorSchema.CrosshairColorPortable;
            schema.NotificationWarningColorPortable = ColorSchema.NotificationWarningColorPortable;
            schema.NotificationWarningTextColorPortable = ColorSchema.NotificationWarningTextColorPortable;
            schema.NotificationErrorColorPortable = ColorSchema.NotificationErrorColorPortable;
            schema.NotificationErrorTextColorPortable = ColorSchema.NotificationErrorTextColorPortable;
            schema.SequencerExpressionTextColorPortable = ColorSchema.SequencerExpressionTextColorPortable;
            ColorSchema = schema;
        }

        public void CopyToAltCustom() {
            var schema = ColorSchemas.Items.Where((x) => x.Name == "Alternative Custom").First();

            schema.PrimaryColorPortable = AltColorSchema.PrimaryColorPortable;
            schema.SecondaryColorPortable = AltColorSchema.SecondaryColorPortable;
            schema.BorderColorPortable = AltColorSchema.BorderColorPortable;
            schema.BackgroundColorPortable = AltColorSchema.BackgroundColorPortable;
            schema.SecondaryBackgroundColorPortable = AltColorSchema.SecondaryBackgroundColorPortable;
            schema.TertiaryBackgroundColorPortable = AltColorSchema.TertiaryBackgroundColorPortable;
            schema.ButtonBackgroundColorPortable = AltColorSchema.ButtonBackgroundColorPortable;
            schema.ButtonBackgroundSelectedColorPortable = AltColorSchema.ButtonBackgroundSelectedColorPortable;
            schema.ButtonForegroundColorPortable = AltColorSchema.ButtonForegroundColorPortable;
            schema.ButtonForegroundDisabledColorPortable = AltColorSchema.ButtonForegroundDisabledColorPortable;
            schema.CrosshairColorPortable = AltColorSchema.CrosshairColorPortable;
            schema.NotificationWarningColorPortable = AltColorSchema.NotificationWarningColorPortable;
            schema.NotificationWarningTextColorPortable = AltColorSchema.NotificationWarningTextColorPortable;
            schema.NotificationErrorColorPortable = AltColorSchema.NotificationErrorColorPortable;
            schema.NotificationErrorTextColorPortable = AltColorSchema.NotificationErrorTextColorPortable;
            schema.SequencerExpressionTextColorPortable = AltColorSchema.SequencerExpressionTextColorPortable;
            AltColorSchema = schema;
        }
    }
}