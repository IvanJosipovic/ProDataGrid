# Column Chooser

The column chooser provides a simple UI for end users to show or hide columns at runtime. It surfaces the DataGrid column collection in a checklist and keeps the grid in sync, including columns generated from column definitions.

## Quick start (XAML columns)

Use `DataGridColumnChooser` and bind it to the grid. The default control theme renders a `DropDownButton` and hosts the checklist in a flyout. Customize the button content via `Header`/`HeaderTemplate`.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:dg="clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls.DataGrid">
  <Grid RowDefinitions="Auto,*" RowSpacing="8">
    <dg:DataGridColumnChooser Width="220"
                              DataGrid="{Binding ElementName=MyGrid}">
      <dg:DataGridColumnChooser.Header>
        <TextBlock Text="Columns" />
      </dg:DataGridColumnChooser.Header>
    </dg:DataGridColumnChooser>

    <DataGrid x:Name="MyGrid"
              Grid.Row="1"
              AutoGenerateColumns="False">
      <DataGrid.Columns>
        <DataGridTextColumn Header="First Name" Binding="{Binding FirstName}" CanUserHide="False" />
        <DataGridTextColumn Header="Last Name" Binding="{Binding LastName}" />
        <DataGridTextColumn Header="Status" Binding="{Binding Status}" />
        <DataGridTextColumn Header="Optional Status" Binding="{Binding OptionalStatus}" IsVisible="False" />
      </DataGrid.Columns>
    </DataGrid>
  </Grid>
</UserControl>
```

- `CanUserHide` locks a column in the chooser.
- `IsVisible` can be used to start a column hidden.
- `DataGrid.CanUserHideColumns` is the grid-wide default for columns that do not specify `CanUserHide`.
- Retemplate `DataGridColumnChooser` (via `ControlTheme`) if you prefer an inline list instead of the default drop-down.

## Column definitions

The chooser also works with `ColumnDefinitionsSource`. When a column is backed by a `DataGridColumnDefinition`, the chooser updates the definition so visibility persists across definition refreshes.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:dg="clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls.DataGrid">
  <DataGrid x:Name="DefinitionGrid"
            ItemsSource="{Binding Items}"
            ColumnDefinitionsSource="{Binding ColumnDefinitions}"
            AutoGenerateColumns="False" />

  <dg:DataGridColumnChooser DataGrid="{Binding ElementName=DefinitionGrid}" />
</UserControl>
```

```csharp
public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; } = new()
{
    new DataGridTextColumnDefinition
    {
        Header = "First Name",
        Binding = ColumnDefinitionBindingFactory.CreateBinding<Person, string>(
            nameof(Person.FirstName),
            p => p.FirstName,
            (p, v) => p.FirstName = v),
        CanUserHide = false
    },
    new DataGridTextColumnDefinition
    {
        Header = "Optional Status",
        Binding = ColumnDefinitionBindingFactory.CreateBinding<Person, PersonStatus?>(
            nameof(Person.OptionalStatus),
            p => p.OptionalStatus,
            (p, v) => p.OptionalStatus = v),
        IsVisible = false
    }
};
```

## Customize the item template

The chooser uses the `DataGridColumnChooserItemTemplate` resource by default. Override it in your scope to change the checklist look. The item data context is `DataGridColumnChooserItem` and exposes `Header`, `IsVisible`, `CanUserHide`, and `Column`.

```xml
<DataTemplate x:Key="DataGridColumnChooserItemTemplate"
              x:DataType="dg:DataGridColumnChooserItem">
  <CheckBox IsChecked="{Binding IsVisible, Mode=TwoWay}"
            IsEnabled="{Binding CanUserHide}">
    <StackPanel Orientation="Horizontal" Spacing="6">
      <TextBlock Text="{Binding Header}" />
      <TextBlock Text="(hideable)"
                 Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                 IsVisible="{Binding CanUserHide}" />
    </StackPanel>
  </CheckBox>
</DataTemplate>
```

## Inline chooser template

If you want to host the list directly (for example, inside your own flyout or a side panel), apply a custom `ControlTheme` that replaces the drop-down template with an `ItemsPresenter`.

```xml
<ControlTheme x:Key="InlineColumnChooserTheme"
              TargetType="dg:DataGridColumnChooser"
              BasedOn="{StaticResource {x:Type dg:DataGridColumnChooser}}">
  <Setter Property="Template">
    <ControlTemplate>
      <Border Padding="8">
        <ItemsPresenter />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

```xml
<dg:DataGridColumnChooser Theme="{StaticResource InlineColumnChooserTheme}"
                          DataGrid="{Binding ElementName=MyGrid}" />
```

## Show or hide all columns

`DataGridColumnChooser` exposes `ShowAll()` and `HideAll()` helpers so you can wire quick actions to buttons or commands.

```csharp
ColumnChooser.ShowAll();
ColumnChooser.HideAll();
```

## Notes

- The chooser orders columns by `DisplayIndex` so it tracks user reordering.
- Hidden columns stay listed so users can bring them back.
