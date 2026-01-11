// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System.Windows.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridButtonColumnDefinition : DataGridColumnDefinition
    {
        private object _content;
        private string _contentTemplateKey;
        private ICommand _command;
        private object _commandParameter;
        private ClickMode? _clickMode;
        private KeyGesture _hotKey;

        public object Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public string ContentTemplateKey
        {
            get => _contentTemplateKey;
            set => SetProperty(ref _contentTemplateKey, value);
        }

        public ICommand Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        public object CommandParameter
        {
            get => _commandParameter;
            set => SetProperty(ref _commandParameter, value);
        }

        public ClickMode? ClickMode
        {
            get => _clickMode;
            set => SetProperty(ref _clickMode, value);
        }

        public KeyGesture HotKey
        {
            get => _hotKey;
            set => SetProperty(ref _hotKey, value);
        }

        protected override DataGridColumn CreateColumnCore()
        {
            return new DataGridButtonColumn();
        }

        protected override void ApplyColumnProperties(DataGridColumn column, DataGridColumnDefinitionContext context)
        {
            if (column is DataGridButtonColumn buttonColumn)
            {
                if (Content != null)
                {
                    buttonColumn.Content = Content;
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.ContentProperty);
                }

                if (ContentTemplateKey != null)
                {
                    buttonColumn.ContentTemplate = context?.ResolveResource<IDataTemplate>(ContentTemplateKey);
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.ContentTemplateProperty);
                }

                if (Command != null)
                {
                    buttonColumn.Command = Command;
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.CommandProperty);
                }

                if (CommandParameter != null)
                {
                    buttonColumn.CommandParameter = CommandParameter;
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.CommandParameterProperty);
                }

                if (ClickMode.HasValue)
                {
                    buttonColumn.ClickMode = ClickMode.Value;
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.ClickModeProperty);
                }

                if (HotKey != null)
                {
                    buttonColumn.HotKey = HotKey;
                }
                else
                {
                    buttonColumn.ClearValue(DataGridButtonColumn.HotKeyProperty);
                }
            }
        }

        protected override bool ApplyColumnPropertyChange(
            DataGridColumn column,
            DataGridColumnDefinitionContext context,
            string propertyName)
        {
            if (column is not DataGridButtonColumn buttonColumn)
            {
                return false;
            }

            switch (propertyName)
            {
                case nameof(Content):
                    if (Content != null)
                    {
                        buttonColumn.Content = Content;
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.ContentProperty);
                    }
                    return true;
                case nameof(ContentTemplateKey):
                    if (ContentTemplateKey != null)
                    {
                        buttonColumn.ContentTemplate = context?.ResolveResource<IDataTemplate>(ContentTemplateKey);
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.ContentTemplateProperty);
                    }
                    return true;
                case nameof(Command):
                    if (Command != null)
                    {
                        buttonColumn.Command = Command;
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.CommandProperty);
                    }
                    return true;
                case nameof(CommandParameter):
                    if (CommandParameter != null)
                    {
                        buttonColumn.CommandParameter = CommandParameter;
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.CommandParameterProperty);
                    }
                    return true;
                case nameof(ClickMode):
                    if (ClickMode.HasValue)
                    {
                        buttonColumn.ClickMode = ClickMode.Value;
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.ClickModeProperty);
                    }
                    return true;
                case nameof(HotKey):
                    if (HotKey != null)
                    {
                        buttonColumn.HotKey = HotKey;
                    }
                    else
                    {
                        buttonColumn.ClearValue(DataGridButtonColumn.HotKeyProperty);
                    }
                    return true;
            }

            return false;
        }
    }
}
