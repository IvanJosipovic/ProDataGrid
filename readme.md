# ProDataGrid

[![Build](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/build.yml)

[![Release](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/release.yml/badge.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/wieslawsoltes/Avalonia.Controls.DataGrid.svg)](https://github.com/wieslawsoltes/Avalonia.Controls.DataGrid/releases)

[![NuGet](https://img.shields.io/nuget/v/ProDataGrid.svg)](https://www.nuget.org/packages/ProDataGrid/)

## About

`ProDataGrid` is a hard fork of the original `Avalonia.Controls.DataGrid` control for [Avalonia](https://github.com/AvaloniaUI/Avalonia).

It displays repeating data in a customizable grid with enhanced features and improved performance.

## Features

| Area | Highlights |
| --- | --- |
| Virtualization & scrolling | ScrollViewer-based `ILogicalScrollable` presenter, smooth wheel/gesture handling, snap points, anchor support, predictive row prefetch, frozen columns. |
| Columns | Text, template, checkbox columns; auto/star/pixel sizing; reordering, resizing, visibility control, frozen sections. |
| Rows | Variable-height support with pluggable estimators; row details; grouping headers; selection modes; row headers. |
| Editing & navigation | In-place editing, commit/cancel, keyboard navigation, clipboard copy modes, current cell tracking. |
| Data operations | Sorting, grouping, paging, currency management via `DataGridCollectionView` family. |
| Styling & theming | Fluent/Simple v2 ScrollViewer templates, row/cell styling, template overrides, theme resources, focus/selection visuals. |

## Usage

Basic setup with common column types and width modes (pixel, auto, star):

```xml
<DataGrid Items="{Binding People}"
          AutoGenerateColumns="False"
          CanUserResizeColumns="True"
          UseLogicalScrollable="True"
          GridLinesVisibility="Horizontal">
  <DataGrid.Columns>
    <!-- Pixel width -->
    <DataGridTextColumn Header="ID"
                        Binding="{Binding Id}"
                        Width="60" />

    <!-- Auto sizes to content -->
    <DataGridTextColumn Header="Name"
                        Binding="{Binding Name}"
                        Width="Auto" />

    <!-- Fixed pixel width checkbox column -->
    <DataGridCheckBoxColumn Header="Active"
                            Binding="{Binding IsActive}"
                            Width="80" />

    <!-- Star sizing shares remaining space -->
    <DataGridTextColumn Header="Department"
                        Binding="{Binding Department}"
                        Width="*" />

    <!-- Template column with custom content and weighted star width -->
    <DataGridTemplateColumn Header="Notes"
                            Width="2*">
      <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
          <TextBlock Text="{Binding Notes}"
                     TextWrapping="Wrap" />
        </DataTemplate>
      </DataGridTemplateColumn.CellTemplate>
    </DataGridTemplateColumn>
  </DataGrid.Columns>
</DataGrid>
```

Widths accept pixel values (`"80"`), `Auto` (content-based), `*` or weighted stars (e.g., `2*`) that share remaining space.

## Package Rename

This package has been renamed from `Avalonia.Controls.DataGrid` to `ProDataGrid`.

The new hard fork lives at: https://github.com/wieslawsoltes/ProDataGrid

### Migration

To migrate from the original package, update your NuGet reference:

```xml
<!-- Old -->
<PackageReference Include="Avalonia.Controls.DataGrid" Version="..." />

<!-- New -->
<PackageReference Include="ProDataGrid" Version="..." />
```

## ScrollViewer-based implementation (v2)

ProDataGrid now ships a ScrollViewer-based template that implements `ILogicalScrollable` on `DataGridRowsPresenter`. This removes the custom `PART_VerticalScrollbar`/`PART_HorizontalScrollbar` pair and lets Avalonia handle scroll bars, scroll chaining, and inertia.

Add the v2 theme to opt into the new template (enables `UseLogicalScrollable` by default):

```xml
<!-- Fluent -->
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml" />

<!-- or Simple -->
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Simple.v2.xaml" />
```

If you use a custom control template, wrap `DataGridRowsPresenter` in a `ScrollViewer` named `PART_ScrollViewer` and set `UseLogicalScrollable="True"`. Keep the column headers in a separate row so they stay fixed while rows scroll.

## Row height estimators

Scrolling with variable row heights is now driven by pluggable estimators via `RowHeightEstimator`:

- `AdvancedRowHeightEstimator` (default): regional averages + Fenwick tree for accurate offsets.
- `CachingRowHeightEstimator`: caches per-row heights for predictable datasets.
- `DefaultRowHeightEstimator`: average-based for uniform rows.

Override the estimator per grid:

```xml
<!-- declare xmlns:controls="clr-namespace:Avalonia.Controls;assembly=ProDataGrid" -->
<DataGrid UseLogicalScrollable="True">
  <DataGrid.RowHeightEstimator>
    <controls:CachingRowHeightEstimator />
  </DataGrid.RowHeightEstimator>
</DataGrid>
```

## Migrating existing usage

- Prefer the v2 theme or update your template to use the ScrollViewer pattern; legacy scroll bars remain available when `UseLogicalScrollable="False"`.
- Replace direct access to template scroll bars with `ScrollViewer` APIs (`ScrollChanged`, `Offset`, `Extent`, `Viewport`).
- When handling wheel/gesture input, rely on the built-in logic (it routes through `UpdateScroll` when `UseLogicalScrollable` is true).
- For theme v2, ensure frozen columns and header separators are kept in sync with horizontal offset (the supplied templates already do this).
- If you depend on stable scroll positioning with dynamic row heights, choose the estimator that matches your data set and reset it after data source changes if needed.

## License

ProDataGrid is licensed under the [MIT License](licence.md).

The original Avalonia.Controls.DataGrid license is preserved in [licence-avalonia.md](licence-avalonia.md).
