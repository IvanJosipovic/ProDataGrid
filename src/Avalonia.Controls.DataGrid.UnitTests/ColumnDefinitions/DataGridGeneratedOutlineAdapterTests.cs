// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.DataGridPivoting;
using Avalonia.Controls.DataGridReporting;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedOutlineAdapterTests
{
    [Fact]
    public void Generated_fields_create_ordered_reflection_free_outline_model()
    {
        var fields = new IDataGridGeneratedAnalyticsField[]
        {
            new DataGridGeneratedAnalyticsField<Row, decimal>(
                "actual",
                DataGridGeneratedAnalyticsRole.OutlineDetail,
                1,
                static row => row.Actual,
                name: "Actual",
                format: "C0",
                aggregate: (int)DataGridAggregateType.Average),
            new DataGridGeneratedAnalyticsField<Row, string>(
                "team",
                DataGridGeneratedAnalyticsRole.OutlineGroup,
                1,
                static row => row.Team,
                name: "Team"),
            new DataGridGeneratedAnalyticsField<Row, string>(
                "region",
                DataGridGeneratedAnalyticsRole.OutlineGroup,
                0,
                static row => row.Region,
                name: "Region"),
            new DataGridGeneratedAnalyticsField<Row, decimal>(
                "planned",
                DataGridGeneratedAnalyticsRole.OutlineDetail,
                0,
                static row => row.Planned,
                name: "Planned",
                format: "C0",
                aggregate: (int)DataGridAggregateType.Sum)
        };
        Row[] rows =
        {
            new("North", "Alpha", 100m, 90m),
            new("North", "Alpha", 200m, 210m),
            new("South", "Beta", 50m, 60m)
        };

        using OutlineReportModel model = DataGridGeneratedOutlineAdapter.CreateModel(
            rows,
            fields,
            static report =>
            {
                report.Layout.ShowGrandTotal = true;
                report.Layout.ShowDetailRows = true;
            });

        Assert.Equal(new[] { "region", "team" }, model.GroupFields.Select(static field => field.Key));
        Assert.Equal(new[] { "planned", "actual" }, model.ValueFields.Select(static field => field.Key));
        Assert.Equal(PivotAggregateType.Sum, model.ValueFields[0].AggregateType);
        Assert.Equal(PivotAggregateType.Average, model.ValueFields[1].AggregateType);
        Assert.All(model.GroupFields, static field => Assert.Null(field.PropertyPath));
        Assert.All(model.ValueFields, static field => Assert.Null(field.PropertyPath));
        Assert.Equal("North", model.GroupFields[0].ValueSelector!(rows[0]));
        Assert.Equal(100m, model.ValueFields[0].ValueSelector!(rows[0]));
        Assert.NotEmpty(model.Rows);
        Assert.NotEmpty(model.ColumnDefinitions);
    }

    private sealed record Row(string Region, string Team, decimal Planned, decimal Actual);
}
