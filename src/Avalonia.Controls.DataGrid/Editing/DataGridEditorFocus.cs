// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.DataGridEditing
{
    internal static class DataGridEditorFocus
    {
        public static void ConfigureSingleTabStop(Control editor)
        {
            KeyboardNavigation.SetTabNavigation(editor, KeyboardNavigationMode.None);
        }

        public static bool FocusTextInput(Control editor, bool selectAll)
        {
            if (TryFocusTextInput(editor, selectAll))
            {
                return true;
            }

            Dispatcher.UIThread.Post(
                () =>
                {
                    if (editor.IsAttachedToVisualTree)
                    {
                        TryFocusTextInput(editor, selectAll);
                    }
                },
                DispatcherPriority.Loaded);

            return false;
        }

        private static bool TryFocusTextInput(Control editor, bool selectAll)
        {
            editor.ApplyTemplate();

            var textBox = editor as TextBox ?? editor.FindDescendantOfType<TextBox>();
            if (textBox == null)
            {
                return editor.Focus();
            }

            if (selectAll)
            {
                textBox.SelectAll();
            }

            return textBox.Focus() || editor.Focus();
        }
    }
}
