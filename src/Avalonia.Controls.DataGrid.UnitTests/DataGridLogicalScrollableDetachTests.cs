// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridLogicalScrollableDetachTests
{
    [AvaloniaFact]
    public void Reparent_between_windows_with_layout_transform_does_not_throw()
    {
        var items = new ObservableCollection<AutoHideItem>(
            Enumerable.Range(1, 200).Select(i => new AutoHideItem
            {
                Name = $"Item {i:000}",
                Value = i
            }));

        var mainWindow = new Window
        {
            Width = 480,
            Height = 320
        };

        mainWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var inlineHost = CreateHost();
        mainWindow.Content = inlineHost;

        var grid = new DataGrid
        {
            ItemsSource = items,
            UseLogicalScrollable = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            KeepRecycledContainersInVisualTree = true,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(AutoHideItem.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Value", Binding = new Binding(nameof(AutoHideItem.Value)) });

        Exception? exception = null;
        try
        {
            exception = Record.Exception(() =>
            {
                mainWindow.Show();
                inlineHost.Content = grid;
                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                grid.ScrollIntoView(items[^1], grid.Columns[0]);
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                for (var i = 0; i < 5; i++)
                {
                    inlineHost.Content = null;
                    var toolWindow = CreateToolWindow(grid, out var toolHost);
                    try
                    {
                        toolWindow.Show(mainWindow);
                        Dispatcher.UIThread.RunJobs();
                        toolWindow.UpdateLayout();
                        grid.UpdateLayout();
                    }
                    finally
                    {
                        toolHost.Content = null;
                        toolWindow.Close();
                    }

                    Dispatcher.UIThread.RunJobs();
                    inlineHost.Content = grid;
                    Dispatcher.UIThread.RunJobs();
                    mainWindow.UpdateLayout();
                    grid.UpdateLayout();
                }
            });
        }
        finally
        {
            mainWindow.Close();
        }

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Reparent_between_content_presenters_with_layout_transform_does_not_throw()
    {
        var items = new ObservableCollection<AutoHideItem>(
            Enumerable.Range(1, 200).Select(i => new AutoHideItem
            {
                Name = $"Item {i:000}",
                Value = i
            }));

        var mainWindow = new Window
        {
            Width = 480,
            Height = 320
        };

        mainWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var inlineHost = new ContentPresenter();
        mainWindow.Content = inlineHost;

        var toolHost = new ContentPresenter();
        var toolTransform = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(0.97, 0.97),
            Child = toolHost
        };
        var toolWindow = new Window
        {
            Width = 420,
            Height = 260,
            Content = new Border
            {
                Padding = new Thickness(8),
                Child = toolTransform
            }
        };
        toolWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var grid = new DataGrid
        {
            ItemsSource = items,
            UseLogicalScrollable = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            KeepRecycledContainersInVisualTree = true,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(AutoHideItem.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Value", Binding = new Binding(nameof(AutoHideItem.Value)) });

        Exception? exception = null;
        try
        {
            exception = Record.Exception(() =>
            {
                mainWindow.Show();
                inlineHost.Content = grid;
                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                grid.ScrollIntoView(items[^1], grid.Columns[0]);
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                for (var i = 0; i < 5; i++)
                {
                    inlineHost.Content = null;
                    Dispatcher.UIThread.RunJobs();

                    toolHost.Content = grid;
                    toolWindow.Show(mainWindow);
                    Dispatcher.UIThread.RunJobs();
                    toolWindow.UpdateLayout();
                    grid.UpdateLayout();

                    toolHost.Content = null;
                    toolWindow.Hide();
                    Dispatcher.UIThread.RunJobs();

                    inlineHost.Content = grid;
                    Dispatcher.UIThread.RunJobs();
                    mainWindow.UpdateLayout();
                    grid.UpdateLayout();
                }
            });
        }
        finally
        {
            toolWindow.Close();
            mainWindow.Close();
        }

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Reparent_during_selection_changes_does_not_throw()
    {
        var items = new ObservableCollection<AutoHideItem>(
            Enumerable.Range(1, 120).Select(i => new AutoHideItem
            {
                Name = $"Item {i:000}",
                Value = i
            }));

        var selectionModel = new SelectionModel<object?>();

        var grid = new DataGrid
        {
            ItemsSource = items,
            Selection = selectionModel,
            SelectionMode = DataGridSelectionMode.Extended,
            UseLogicalScrollable = true,
            KeepRecycledContainersInVisualTree = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(AutoHideItem.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Value", Binding = new Binding(nameof(AutoHideItem.Value)) });

        var selectionView = new ItemsControl
        {
            ItemsSource = selectionModel.SelectedItems
        };

        var content = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                grid,
                selectionView
            }
        };

        var mainWindow = new Window
        {
            Width = 480,
            Height = 320
        };
        mainWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var inlineHost = CreateHost();
        var reparentHost = CreateHost();
        var reparentTransform = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(0.97, 0.97),
            Child = reparentHost
        };

        mainWindow.Content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                inlineHost,
                reparentTransform
            }
        };

        var reparented = false;
        EventHandler<SelectionModelSelectionChangedEventArgs> selectionChanged = (_, __) =>
        {
            if (reparented)
                return;

            reparented = true;
            inlineHost.Content = null;
            reparentHost.Content = content;
        };

        Exception? exception = null;
        try
        {
            exception = Record.Exception(() =>
            {
                mainWindow.Show();
                inlineHost.Content = content;
                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                Assert.Same(grid.CollectionView, selectionModel.Source);

                selectionModel.SelectionChanged += selectionChanged;

                selectionModel.Select(0);
                selectionModel.Select(1);
                selectionModel.Select(2);

                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                reparentHost.Content = null;
                inlineHost.Content = content;
                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();
            });
        }
        finally
        {
            selectionModel.SelectionChanged -= selectionChanged;
            mainWindow.Close();
        }

        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Reparent_between_windows_during_selection_changes_does_not_throw()
    {
        var items = new ObservableCollection<AutoHideItem>(
            Enumerable.Range(1, 120).Select(i => new AutoHideItem
            {
                Name = $"Item {i:000}",
                Value = i
            }));

        var selectionModel = new SelectionModel<object?>();

        var grid = new DataGrid
        {
            ItemsSource = items,
            Selection = selectionModel,
            SelectionMode = DataGridSelectionMode.Extended,
            UseLogicalScrollable = true,
            KeepRecycledContainersInVisualTree = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(AutoHideItem.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Value", Binding = new Binding(nameof(AutoHideItem.Value)) });

        var selectionView = new ItemsControl
        {
            ItemsSource = selectionModel.SelectedItems
        };

        var content = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                grid,
                selectionView
            }
        };

        var mainWindow = new Window
        {
            Width = 480,
            Height = 320
        };
        mainWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var inlineHost = CreateHost();
        mainWindow.Content = inlineHost;

        var toolHost = CreateHost();
        var toolTransform = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(0.97, 0.97),
            Child = toolHost
        };
        var toolWindow = new Window
        {
            Width = 420,
            Height = 260,
            Content = new Border
            {
                Padding = new Thickness(8),
                Child = toolTransform
            }
        };
        toolWindow.SetThemeStyles(DataGridTheme.SimpleV2);

        var reparented = false;
        EventHandler<SelectionModelSelectionChangedEventArgs> selectionChanged = (_, __) =>
        {
            if (reparented)
                return;

            reparented = true;
            Dispatcher.UIThread.Post(() =>
            {
                inlineHost.Content = null;
                mainWindow.Hide();
                toolHost.Content = content;
                toolWindow.Show();
            });
        };

        Exception? exception = null;
        try
        {
            exception = Record.Exception(() =>
            {
                mainWindow.Show();
                inlineHost.Content = content;
                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();

                Assert.Same(grid.CollectionView, selectionModel.Source);

                selectionModel.SelectionChanged += selectionChanged;

                selectionModel.Select(0);
                selectionModel.Select(1);
                selectionModel.Select(2);

                Dispatcher.UIThread.RunJobs();
                Dispatcher.UIThread.RunJobs();
                toolWindow.UpdateLayout();
                grid.UpdateLayout();

                toolHost.Content = null;
                toolWindow.Hide();
                inlineHost.Content = content;
                mainWindow.Show();

                Dispatcher.UIThread.RunJobs();
                mainWindow.UpdateLayout();
                grid.UpdateLayout();
            });
        }
        finally
        {
            selectionModel.SelectionChanged -= selectionChanged;
            toolWindow.Close();
            mainWindow.Close();
        }

        Assert.Null(exception);
    }

    private sealed class AutoHideItem
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    private static ContentControl CreateHost()
    {
        return new ContentControl
        {
            Template = new FuncControlTemplate<ContentControl>((parent, _) =>
            {
                return new ContentPresenter
                {
                    [!ContentPresenter.ContentProperty] = parent[!ContentControl.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ContentControl.ContentTemplateProperty],
                    [!ContentPresenter.HorizontalContentAlignmentProperty] = parent[!ContentControl.HorizontalContentAlignmentProperty],
                    [!ContentPresenter.VerticalContentAlignmentProperty] = parent[!ContentControl.VerticalContentAlignmentProperty]
                };
            })
        };
    }

    private static Window CreateToolWindow(Control content, out ContentControl host)
    {
        host = CreateHost();
        host.Content = content;

        var toolTransform = new LayoutTransformControl
        {
            LayoutTransform = new ScaleTransform(0.97, 0.97),
            Child = host
        };

        var toolWindow = new Window
        {
            Width = 420,
            Height = 260,
            Content = new Border
            {
                Padding = new Thickness(8),
                Child = toolTransform
            }
        };

        toolWindow.SetThemeStyles(DataGridTheme.SimpleV2);
        return toolWindow;
    }
}
