using System;
using Avalonia.Controls;
using Avalonia.Diagnostics.Services;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics.Views
{
    partial class PropertyValueEditorView : UserControl
    {
        private readonly PropertyValueEditorService _editorService = new();

        private PropertyViewModel? Property => (PropertyViewModel?)DataContext;

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            UpdateEditor();
        }

        private void UpdateEditor()
        {
            if (Property?.PropertyType is not { } propertyType)
            {
                Content = null;
                return;
            }

            Content = _editorService.GetOrCreateEditor(Property, propertyType);
        }
    }
}
