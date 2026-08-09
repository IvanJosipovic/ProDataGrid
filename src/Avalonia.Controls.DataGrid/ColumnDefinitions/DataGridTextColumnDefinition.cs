// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Media;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridTextColumnDefinition : DataGridBoundColumnDefinition
    {
        private FontFamily _fontFamily;
        private double? _fontSize;
        private FontStyle? _fontStyle;
        private FontWeight? _fontWeight;
        private FontStretch? _fontStretch;
        private IBrush _foreground;
        private string _watermark;
        private bool _useDirectTextCell;
        private bool _useDirectTextContent;
        private bool _trackDirectTextValueChanges = true;

        public FontFamily FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        public double? FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public FontStyle? FontStyle
        {
            get => _fontStyle;
            set => SetProperty(ref _fontStyle, value);
        }

        public FontWeight? FontWeight
        {
            get => _fontWeight;
            set => SetProperty(ref _fontWeight, value);
        }

        public FontStretch? FontStretch
        {
            get => _fontStretch;
            set => SetProperty(ref _fontStretch, value);
        }

        public IBrush Foreground
        {
            get => _foreground;
            set => SetProperty(ref _foreground, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        /// <summary>
        /// Gets or sets whether generated text columns use the optimized retained direct-text cell.
        /// </summary>
        public bool UseDirectTextCell
        {
            get => _useDirectTextCell;
            set => SetProperty(ref _useDirectTextCell, value);
        }

        /// <summary>
        /// Gets or sets whether ordinary retained cells use their typed accessor directly in
        /// the retained text element, without replacing the cell or its Avalonia template.
        /// </summary>
        public bool UseDirectTextContent
        {
            get => _useDirectTextContent;
            set => SetProperty(ref _useDirectTextContent, value);
        }

        /// <summary>
        /// Gets or sets whether generated direct text cells subscribe to row-item property changes.
        /// Disable this only when the displayed row values are immutable.
        /// </summary>
        public bool TrackDirectTextValueChanges
        {
            get => _trackDirectTextValueChanges;
            set => SetProperty(ref _trackDirectTextValueChanges, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridTextColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridTextColumn textColumn)
            {
                textColumn.UseDirectTextCell = UseDirectTextCell;
                textColumn.UseDirectTextContent = UseDirectTextContent;
                textColumn.TrackDirectTextValueChanges = TrackDirectTextValueChanges;
                if (FontFamily != null)
                {
                    textColumn.FontFamily = FontFamily;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.FontFamilyProperty);
                }

                if (Foreground != null)
                {
                    textColumn.Foreground = Foreground;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.ForegroundProperty);
                }

                if (Watermark != null)
                {
                    textColumn.Watermark = Watermark;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.WatermarkProperty);
                }

                if (FontSize.HasValue)
                {
                    textColumn.FontSize = FontSize.Value;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.FontSizeProperty);
                }

                if (FontStyle.HasValue)
                {
                    textColumn.FontStyle = FontStyle.Value;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.FontStyleProperty);
                }

                if (FontWeight.HasValue)
                {
                    textColumn.FontWeight = FontWeight.Value;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.FontWeightProperty);
                }

                if (FontStretch.HasValue)
                {
                    textColumn.FontStretch = FontStretch.Value;
                }
                else
                {
                    textColumn.ClearValue(DataGridTextColumn.FontStretchProperty);
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

            if (column is not DataGridTextColumn textColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(UseDirectTextCell):
                    textColumn.UseDirectTextCell = UseDirectTextCell;
                    return true;
                case nameof(UseDirectTextContent):
                    textColumn.UseDirectTextContent = UseDirectTextContent;
                    return true;
                case nameof(TrackDirectTextValueChanges):
                    textColumn.TrackDirectTextValueChanges = TrackDirectTextValueChanges;
                    return true;
                case nameof(FontFamily):
                    if (FontFamily != null)
                    {
                        textColumn.FontFamily = FontFamily;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.FontFamilyProperty);
                    }
                    return true;
                case nameof(Foreground):
                    if (Foreground != null)
                    {
                        textColumn.Foreground = Foreground;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.ForegroundProperty);
                    }
                    return true;
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        textColumn.Watermark = Watermark;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(FontSize):
                    if (FontSize.HasValue)
                    {
                        textColumn.FontSize = FontSize.Value;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.FontSizeProperty);
                    }
                    return true;
                case nameof(FontStyle):
                    if (FontStyle.HasValue)
                    {
                        textColumn.FontStyle = FontStyle.Value;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.FontStyleProperty);
                    }
                    return true;
                case nameof(FontWeight):
                    if (FontWeight.HasValue)
                    {
                        textColumn.FontWeight = FontWeight.Value;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.FontWeightProperty);
                    }
                    return true;
                case nameof(FontStretch):
                    if (FontStretch.HasValue)
                    {
                        textColumn.FontStretch = FontStretch.Value;
                    }
                    else
                    {
                        textColumn.ClearValue(DataGridTextColumn.FontStretchProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
