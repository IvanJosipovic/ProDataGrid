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
    sealed class DataGridImageColumnDefinition : DataGridBoundColumnDefinition
    {
        private double? _imageWidth;
        private double? _imageHeight;
        private Stretch? _stretch;
        private StretchDirection? _stretchDirection;
        private bool? _allowEditing;
        private string _watermark;

        public double? ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        public double? ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }

        public Stretch? Stretch
        {
            get => _stretch;
            set => SetProperty(ref _stretch, value);
        }

        public StretchDirection? StretchDirection
        {
            get => _stretchDirection;
            set => SetProperty(ref _stretchDirection, value);
        }

        public bool? AllowEditing
        {
            get => _allowEditing;
            set => SetProperty(ref _allowEditing, value);
        }

        public string Watermark
        {
            get => _watermark;
            set => SetProperty(ref _watermark, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridImageColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            base.ApplyColumnProperties(column, context);

            if (column is DataGridImageColumn imageColumn)
            {
                if (Watermark != null)
                {
                    imageColumn.Watermark = Watermark;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.WatermarkProperty);
                }

                if (ImageWidth.HasValue)
                {
                    imageColumn.ImageWidth = ImageWidth.Value;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.ImageWidthProperty);
                }

                if (ImageHeight.HasValue)
                {
                    imageColumn.ImageHeight = ImageHeight.Value;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.ImageHeightProperty);
                }

                if (Stretch.HasValue)
                {
                    imageColumn.Stretch = Stretch.Value;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.StretchProperty);
                }

                if (StretchDirection.HasValue)
                {
                    imageColumn.StretchDirection = StretchDirection.Value;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.StretchDirectionProperty);
                }

                if (AllowEditing.HasValue)
                {
                    imageColumn.AllowEditing = AllowEditing.Value;
                }
                else
                {
                    imageColumn.ClearValue(DataGridImageColumn.AllowEditingProperty);
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

            if (column is not DataGridImageColumn imageColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(Watermark):
                    if (Watermark != null)
                    {
                        imageColumn.Watermark = Watermark;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.WatermarkProperty);
                    }
                    return true;
                case nameof(ImageWidth):
                    if (ImageWidth.HasValue)
                    {
                        imageColumn.ImageWidth = ImageWidth.Value;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.ImageWidthProperty);
                    }
                    return true;
                case nameof(ImageHeight):
                    if (ImageHeight.HasValue)
                    {
                        imageColumn.ImageHeight = ImageHeight.Value;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.ImageHeightProperty);
                    }
                    return true;
                case nameof(Stretch):
                    if (Stretch.HasValue)
                    {
                        imageColumn.Stretch = Stretch.Value;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.StretchProperty);
                    }
                    return true;
                case nameof(StretchDirection):
                    if (StretchDirection.HasValue)
                    {
                        imageColumn.StretchDirection = StretchDirection.Value;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.StretchDirectionProperty);
                    }
                    return true;
                case nameof(AllowEditing):
                    if (AllowEditing.HasValue)
                    {
                        imageColumn.AllowEditing = AllowEditing.Value;
                    }
                    else
                    {
                        imageColumn.ClearValue(DataGridImageColumn.AllowEditingProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
