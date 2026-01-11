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
    sealed class DataGridProgressBarColumnDefinition : DataGridBoundColumnDefinition
    {
        private double? _minimum;
        private double? _maximum;
        private bool? _showProgressText;
        private string _progressTextFormat;
        private IBrush _foreground;
        private IBrush _background;
        private double? _height;

        public double? Minimum
        {
            get => _minimum;
            set => SetProperty(ref _minimum, value);
        }

        public double? Maximum
        {
            get => _maximum;
            set => SetProperty(ref _maximum, value);
        }

        public bool? ShowProgressText
        {
            get => _showProgressText;
            set => SetProperty(ref _showProgressText, value);
        }

        public string ProgressTextFormat
        {
            get => _progressTextFormat;
            set => SetProperty(ref _progressTextFormat, value);
        }

        public IBrush Foreground
        {
            get => _foreground;
            set => SetProperty(ref _foreground, value);
        }

        public IBrush Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        public double? Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridProgressBarColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridProgressBarColumn progressColumn)
            {
                if (ProgressTextFormat != null)
                {
                    progressColumn.ProgressTextFormat = ProgressTextFormat;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.ProgressTextFormatProperty);
                }

                if (Foreground != null)
                {
                    progressColumn.Foreground = Foreground;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.ForegroundProperty);
                }

                if (Background != null)
                {
                    progressColumn.Background = Background;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.BackgroundProperty);
                }

                if (Minimum.HasValue)
                {
                    progressColumn.Minimum = Minimum.Value;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.MinimumProperty);
                }

                if (Maximum.HasValue)
                {
                    progressColumn.Maximum = Maximum.Value;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.MaximumProperty);
                }

                if (ShowProgressText.HasValue)
                {
                    progressColumn.ShowProgressText = ShowProgressText.Value;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.ShowProgressTextProperty);
                }

                if (Height.HasValue)
                {
                    progressColumn.Height = Height.Value;
                }
                else
                {
                    progressColumn.ClearValue(DataGridProgressBarColumn.HeightProperty);
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

            if (column is not DataGridProgressBarColumn progressColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(ProgressTextFormat):
                    if (ProgressTextFormat != null)
                    {
                        progressColumn.ProgressTextFormat = ProgressTextFormat;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.ProgressTextFormatProperty);
                    }
                    return true;
                case nameof(Foreground):
                    if (Foreground != null)
                    {
                        progressColumn.Foreground = Foreground;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.ForegroundProperty);
                    }
                    return true;
                case nameof(Background):
                    if (Background != null)
                    {
                        progressColumn.Background = Background;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.BackgroundProperty);
                    }
                    return true;
                case nameof(Minimum):
                    if (Minimum.HasValue)
                    {
                        progressColumn.Minimum = Minimum.Value;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.MinimumProperty);
                    }
                    return true;
                case nameof(Maximum):
                    if (Maximum.HasValue)
                    {
                        progressColumn.Maximum = Maximum.Value;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.MaximumProperty);
                    }
                    return true;
                case nameof(ShowProgressText):
                    if (ShowProgressText.HasValue)
                    {
                        progressColumn.ShowProgressText = ShowProgressText.Value;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.ShowProgressTextProperty);
                    }
                    return true;
                case nameof(Height):
                    if (Height.HasValue)
                    {
                        progressColumn.Height = Height.Value;
                    }
                    else
                    {
                        progressColumn.ClearValue(DataGridProgressBarColumn.HeightProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
