// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System.Globalization;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridMaskedTextColumnDefinition : DataGridBoundColumnDefinition
    {
        private string _mask;
        private char? _promptChar;
        private bool? _asciiOnly;
        private bool? _hidePromptOnLeave;
        private bool? _resetOnPrompt;
        private bool? _resetOnSpace;
        private CultureInfo _culture;
        private string _watermark;

        public string Mask
        {
            get => _mask;
            set => SetProperty(ref _mask, value);
        }

        public char? PromptChar
        {
            get => _promptChar;
            set => SetProperty(ref _promptChar, value);
        }

        public bool? AsciiOnly
        {
            get => _asciiOnly;
            set => SetProperty(ref _asciiOnly, value);
        }

        public bool? HidePromptOnLeave
        {
            get => _hidePromptOnLeave;
            set => SetProperty(ref _hidePromptOnLeave, value);
        }

        public bool? ResetOnPrompt
        {
            get => _resetOnPrompt;
            set => SetProperty(ref _resetOnPrompt, value);
        }

        public bool? ResetOnSpace
        {
            get => _resetOnSpace;
            set => SetProperty(ref _resetOnSpace, value);
        }

        public CultureInfo Culture
        {
            get => _culture;
            set => SetProperty(ref _culture, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridMaskedTextColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridMaskedTextColumn maskedColumn)
            {
                if (Mask != null)
                {
                    maskedColumn.Mask = Mask;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.MaskProperty);
                }

                if (Culture != null)
                {
                    maskedColumn.Culture = Culture;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.CultureProperty);
                }

                if (Watermark != null)
                {
                    maskedColumn.Watermark = Watermark;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.WatermarkProperty);
                }

                if (PromptChar.HasValue)
                {
                    maskedColumn.PromptChar = PromptChar.Value;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.PromptCharProperty);
                }

                if (AsciiOnly.HasValue)
                {
                    maskedColumn.AsciiOnly = AsciiOnly.Value;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.AsciiOnlyProperty);
                }

                if (HidePromptOnLeave.HasValue)
                {
                    maskedColumn.HidePromptOnLeave = HidePromptOnLeave.Value;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.HidePromptOnLeaveProperty);
                }

                if (ResetOnPrompt.HasValue)
                {
                    maskedColumn.ResetOnPrompt = ResetOnPrompt.Value;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.ResetOnPromptProperty);
                }

                if (ResetOnSpace.HasValue)
                {
                    maskedColumn.ResetOnSpace = ResetOnSpace.Value;
                }
                else
                {
                    maskedColumn.ClearValue(DataGridMaskedTextColumn.ResetOnSpaceProperty);
                }
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (base.ApplyColumnPropertyChange(column, context, propertyName))
            {
                return true;
            }

            if (column is not DataGridMaskedTextColumn maskedColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(Mask):
                    if (Mask != null)
                    {
                        maskedColumn.Mask = Mask;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.MaskProperty);
                    }
                    return true;
                case nameof(Culture):
                    if (Culture != null)
                    {
                        maskedColumn.Culture = Culture;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.CultureProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        maskedColumn.Watermark = Watermark;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(PromptChar):
                    if (PromptChar.HasValue)
                    {
                        maskedColumn.PromptChar = PromptChar.Value;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.PromptCharProperty);
                    }
                    return true;
                case nameof(AsciiOnly):
                    if (AsciiOnly.HasValue)
                    {
                        maskedColumn.AsciiOnly = AsciiOnly.Value;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.AsciiOnlyProperty);
                    }
                    return true;
                case nameof(HidePromptOnLeave):
                    if (HidePromptOnLeave.HasValue)
                    {
                        maskedColumn.HidePromptOnLeave = HidePromptOnLeave.Value;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.HidePromptOnLeaveProperty);
                    }
                    return true;
                case nameof(ResetOnPrompt):
                    if (ResetOnPrompt.HasValue)
                    {
                        maskedColumn.ResetOnPrompt = ResetOnPrompt.Value;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.ResetOnPromptProperty);
                    }
                    return true;
                case nameof(ResetOnSpace):
                    if (ResetOnSpace.HasValue)
                    {
                        maskedColumn.ResetOnSpace = ResetOnSpace.Value;
                    }
                    else
                    {
                        maskedColumn.ClearValue(DataGridMaskedTextColumn.ResetOnSpaceProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
