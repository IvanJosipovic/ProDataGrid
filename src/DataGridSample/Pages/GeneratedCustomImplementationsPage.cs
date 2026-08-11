// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using DataGridSample.ViewModels;
using ReactiveUI;
using RxVoid = ReactiveUI.Primitives.RxVoid;

namespace DataGridSample.Pages;

public sealed class GeneratedCustomImplementationsPage : GeneratedCustomImplementationsGeneratedView
{
    private static readonly IPropertyInfo s_rejectInvalidEditCommandProperty = CommandProperty(
        nameof(GeneratedCustomImplementationsViewModel.RejectInvalidEditCommand),
        static viewModel => viewModel.RejectInvalidEditCommand);

    private static readonly IPropertyInfo s_applyValidEditCommandProperty = CommandProperty(
        nameof(GeneratedCustomImplementationsViewModel.ApplyValidEditCommand),
        static viewModel => viewModel.ApplyValidEditCommand);

    private static readonly IPropertyInfo s_sortSeverityCommandProperty = CommandProperty(
        nameof(GeneratedCustomImplementationsViewModel.SortSeverityCommand),
        static viewModel => viewModel.SortSeverityCommand);

    private static readonly IPropertyInfo s_restoreCommandProperty = CommandProperty(
        nameof(GeneratedCustomImplementationsViewModel.RestoreCommand),
        static viewModel => viewModel.RestoreCommand);

    private static readonly IPropertyInfo s_statusProperty = new ClrPropertyInfo(
        nameof(GeneratedCustomImplementationsViewModel.Status),
        static target => target is GeneratedCustomImplementationsViewModel viewModel ? viewModel.Status : string.Empty,
        setter: null,
        typeof(string));

    public GeneratedCustomImplementationsPage()
    {
    }

    public GeneratedCustomImplementationsPage(GeneratedCustomImplementationsViewModel viewModel)
        : base(viewModel)
    {
    }

    protected override void ConfigureGeneratedDataGrid(DataGrid dataGrid)
    {
        dataGrid.Tag = "custom-generated-view-hook";
        dataGrid.Classes.Add("custom-implementations-grid");
        dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
    }

    protected override Control? CreateGeneratedToolbar()
    {
        Control? generated = base.CreateGeneratedToolbar();
        if (generated is not ContentControl slot)
        {
            return generated;
        }

        var commands = new WrapPanel();
        commands.Children.Add(CreateCommandButton("Reject invalid effort", s_rejectInvalidEditCommandProperty));
        commands.Children.Add(CreateCommandButton("Apply valid effort", s_applyValidEditCommandProperty));
        commands.Children.Add(CreateCommandButton("Sort by severity policy", s_sortSeverityCommandProperty));
        commands.Children.Add(CreateCommandButton("Restore", s_restoreCommandProperty));

        var status = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        status[!TextBlock.TextProperty] = CreateBinding(s_statusProperty);

        slot.Content = new StackPanel
        {
            Spacing = 6d,
            Children =
            {
                new TextBlock
                {
                    Text = "This toolbar and grid configuration come from protected generated-view hooks."
                },
                commands,
                status
            }
        };
        AutomationProperties.SetName(slot, "User-defined generated view toolbar");
        return slot;
    }

    private static Button CreateCommandButton(string text, IPropertyInfo property)
    {
        var button = new Button { Content = text };
        button[!Button.CommandProperty] = CreateBinding(property);
        return button;
    }

    private static IPropertyInfo CommandProperty(
        string name,
        Func<GeneratedCustomImplementationsViewModel, ReactiveCommand<RxVoid, RxVoid>> getter) =>
        new ClrPropertyInfo(
            name,
            target => target is GeneratedCustomImplementationsViewModel viewModel ? getter(viewModel) : null,
            setter: null,
            typeof(ReactiveCommand<RxVoid, RxVoid>));

    private static CompiledBindingExtension CreateBinding(IPropertyInfo property) =>
        new()
        {
            DataType = typeof(GeneratedCustomImplementationsViewModel),
            Mode = BindingMode.OneWay,
            Path = new CompiledBindingPathBuilder()
                .Property(
                    property,
                    Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
                .Build()
        };
}
