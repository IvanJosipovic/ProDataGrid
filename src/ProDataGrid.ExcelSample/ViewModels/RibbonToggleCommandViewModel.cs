using System;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class RibbonToggleCommandViewModel : ReactiveObject, IRibbonCommandViewModel
{
    private readonly Action<bool> _onToggled;
    private bool _isChecked;

    public RibbonToggleCommandViewModel(string label, bool isChecked, Action<bool> onToggled, string? glyph = null)
    {
        Label = label;
        Glyph = glyph;
        _isChecked = isChecked;
        _onToggled = onToggled ?? throw new ArgumentNullException(nameof(onToggled));
    }

    public string Label { get; }

    public string? Glyph { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _isChecked, value);
            _onToggled(value);
        }
    }
}
