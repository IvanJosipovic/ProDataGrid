using System.Collections.Generic;

namespace DataGridSample.Models
{
    public sealed class StateSampleItem
    {
        public StateSampleItem(int id, string name, string category, string group, double amount)
        {
            Id = id;
            Name = name;
            Category = category;
            Group = group;
            Amount = amount;
        }

        public int Id { get; }

        public string Name { get; }

        public string Category { get; }

        public string Group { get; }

        public double Amount { get; }

        public static List<StateSampleItem> CreateSamples(int count)
        {
            var items = new List<StateSampleItem>(count);
            for (int i = 0; i < count; i++)
            {
                var category = i % 2 == 0 ? "Alpha" : "Beta";
                var group = i % 3 == 0 ? "North" : "South";
                var amount = 100 + (i * 2.5);
                items.Add(new StateSampleItem(i, $"Item {i}", category, group, amount));
            }

            return items;
        }
    }
}
