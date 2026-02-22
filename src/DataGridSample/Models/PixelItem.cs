using System;
using System.Diagnostics.CodeAnalysis;

namespace DataGridSample.Models
{
    /// <summary>
    /// Simple model to showcase pixel-based column widths and horizontal scrolling.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class PixelItem
    {
        private static readonly string[] Categories = { "Alpha", "Beta", "Gamma", "Delta", "Omega" };
        private static readonly string[] Adjectives = { "Bright", "Calm", "Swift", "Bold", "Misty", "Silent", "Verdant", "Crimson" };
        private static readonly string[] Nouns = { "Forest", "Canyon", "River", "Valley", "Harbor", "Summit", "Meadow", "Coast" };

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public static PixelItem Create(int id, Random random)
        {
            string name = $"{Adjectives[random.Next(Adjectives.Length)]} {Nouns[random.Next(Nouns.Length)]}";
            int lineCount = random.Next(1, 4);
            string description = lineCount switch
            {
                1 => $"{name} sample text line 1",
                2 => $"{name} sample text line 1 {name} sample text line 2",
                _ => $"{name} sample text line 1 {name} sample text line 2 {name} sample text line 3"
            };

            string notes = $"Note #{id}: {Adjectives[random.Next(Adjectives.Length)]} {Adjectives[random.Next(Adjectives.Length)]} {Adjectives[random.Next(Adjectives.Length)]}";

            return new PixelItem
            {
                Id = id,
                Name = name,
                Category = Categories[random.Next(Categories.Length)],
                Description = description,
                Notes = notes
            };
        }
    }
}
