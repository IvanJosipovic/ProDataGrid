using System;
using System.ComponentModel;

namespace Avalonia.Diagnostics.ViewModels
{
    internal sealed class ResourceEntryPropertyViewModel : PropertyViewModel
    {
        private readonly Func<object?> _getter;
        private readonly Action<object?>? _setter;
        private readonly string _name;
        private readonly Type _propertyType;
        private readonly Type? _declaringType;
        private object? _value;
        private Type _assignedType;

        public ResourceEntryPropertyViewModel(
            string name,
            Type propertyType,
            Func<object?> getter,
            Action<object?>? setter,
            Type? declaringType)
        {
            _name = name;
            _propertyType = propertyType;
            _getter = getter;
            _setter = setter;
            _declaringType = declaringType;
            _assignedType = propertyType;
            Update();
        }

        public override object Key => _name;
        public override string Name => _name;
        public override string Group => IsPinned ? "Pinned" : "Resources";
        public override Type AssignedType => _assignedType;
        public override Type? DeclaringType => _declaringType;
        public override Type PropertyType => _propertyType;
        public override bool IsReadonly => _setter is null;
        public override string Priority => string.Empty;
        public override bool? IsAttached => null;

        public override object? Value
        {
            get => _value;
            set
            {
                if (IsReadonly)
                {
                    return;
                }

                try
                {
                    _setter?.Invoke(value);
                    Update();
                }
                catch
                {
                }
            }
        }

        public override void Update()
        {
            object? value;
            Type? valueType = null;

            try
            {
                value = _getter();
                valueType = value?.GetType();
            }
            catch (Exception e)
            {
                value = e.GetBaseException();
            }

            RaiseAndSetIfChanged(ref _value, value, nameof(Value));
            RaiseAndSetIfChanged(ref _assignedType, valueType ?? _propertyType, nameof(AssignedType));
            RaisePropertyChanged(nameof(Type));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsPinned))
            {
                RaisePropertyChanged(nameof(Group));
            }
        }
    }
}
