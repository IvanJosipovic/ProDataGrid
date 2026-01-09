using System;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Diagnostics.UnitTests;

public class PropertyValueEditorViewTests
{
    [AvaloniaFact]
    public void Editor_is_reused_for_same_property_type()
    {
        var view = CreateView();
        var first = CreatePropertyViewModel(new TestTarget { Flag = true }, nameof(TestTarget.Flag));
        var second = CreatePropertyViewModel(new TestTarget { Flag = false }, nameof(TestTarget.Flag));

        view.DataContext = first;
        var initialEditor = view.Content;

        view.DataContext = second;

        Assert.Same(initialEditor, view.Content);
    }

    [AvaloniaFact]
    public void Editor_changes_for_different_property_type()
    {
        var view = CreateView();
        view.DataContext = CreatePropertyViewModel(new TestTarget { Flag = true }, nameof(TestTarget.Flag));
        var initialEditor = view.Content;

        view.DataContext = CreatePropertyViewModel(new TestTarget { Name = "Hello" }, nameof(TestTarget.Name));

        Assert.NotSame(initialEditor, view.Content);
    }

    [AvaloniaFact]
    public void Readonly_state_is_applied_to_checkbox_editor()
    {
        var view = CreateView();
        view.DataContext = CreatePropertyViewModel(new TestTarget(), nameof(TestTarget.ReadOnlyFlag));

        var editor = Assert.IsType<CheckBox>(view.Content);
        Assert.False(editor.IsEnabled);
    }

    private static UserControl CreateView()
    {
        var viewType = typeof(DevToolsExtensions).Assembly
            .GetType("Avalonia.Diagnostics.Views.PropertyValueEditorView", throwOnError: true);
        return (UserControl)Activator.CreateInstance(viewType!, nonPublic: true)!;
    }

    private static object CreatePropertyViewModel(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        var viewModelType = typeof(DevToolsExtensions).Assembly
            .GetType("Avalonia.Diagnostics.ViewModels.ClrPropertyViewModel", throwOnError: true);
        return Activator.CreateInstance(
            viewModelType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { target, property! },
            culture: CultureInfo.InvariantCulture)!;
    }

    private sealed class TestTarget
    {
        public bool Flag { get; set; }

        public string? Name { get; set; }

        public bool ReadOnlyFlag => true;
    }
}
