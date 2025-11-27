using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using DataGridSample.Models;

namespace DataGridSample.ViewModels
{
    public class BasicViewModel
    {
        public BasicViewModel()
        {
            RegionSortDescription = DataGridSortDescription.FromPath(
                nameof(Country.Region),
                ListSortDirection.Ascending,
                new ReversedStringComparer());

            CountriesView = new DataGridCollectionView(Countries.All);
            CountriesView.SortDescriptions.Add(RegionSortDescription);
        }

        public DataGridCollectionView CountriesView { get; }

        public DataGridSortDescription RegionSortDescription { get; }

        public void EnsureCustomSort(string propertyPath)
        {
            if (propertyPath == RegionSortDescription.PropertyPath &&
                !CountriesView.SortDescriptions.Contains(RegionSortDescription))
            {
                CountriesView.SortDescriptions.Add(RegionSortDescription);
            }
        }

        private sealed class ReversedStringComparer : IComparer<object?>, IComparer
        {
            public int Compare(object? x, object? y)
            {
                if (x is string left && y is string right)
                {
                    var reversedLeft = new string(left.Reverse().ToArray());
                    var reversedRight = new string(right.Reverse().ToArray());
                    return reversedLeft.CompareTo(reversedRight);
                }

                return Comparer.Default.Compare(x, y);
            }
        }
    }
}
