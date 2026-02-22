using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DataGridSample.Models;
using Xunit;

namespace DataGridSample.Tests;

public sealed class PixelItemTests
{
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
    {
        "Alpha",
        "Beta",
        "Gamma",
        "Delta",
        "Omega"
    };

    private static readonly HashSet<string> Adjectives = new(StringComparer.Ordinal)
    {
        "Bright",
        "Calm",
        "Swift",
        "Bold",
        "Misty",
        "Silent",
        "Verdant",
        "Crimson"
    };

    private static readonly HashSet<string> Nouns = new(StringComparer.Ordinal)
    {
        "Forest",
        "Canyon",
        "River",
        "Valley",
        "Harbor",
        "Summit",
        "Meadow",
        "Coast"
    };

    [Fact]
    public void Create_ProducesExpectedShapeAndVocabulary()
    {
        var item = PixelItem.Create(42, new Random(17));

        Assert.Equal(42, item.Id);
        Assert.Contains(item.Category, Categories);

        string[] nameParts = item.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, nameParts.Length);
        Assert.Contains(nameParts[0], Adjectives);
        Assert.Contains(nameParts[1], Nouns);

        string descriptionPattern = $"^{Regex.Escape(item.Name)} sample text line 1( {Regex.Escape(item.Name)} sample text line [23]){{0,2}}$";
        Assert.Matches(descriptionPattern, item.Description);

        string notesPrefix = "Note #42: ";
        Assert.StartsWith(notesPrefix, item.Notes, StringComparison.Ordinal);
        string[] notesWords = item.Notes[notesPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, notesWords.Length);
        Assert.All(notesWords, word => Assert.Contains(word, Adjectives));
    }

    [Fact]
    public void Create_IsDeterministic_ForSameSeedAndCallOrder()
    {
        var random1 = new Random(7);
        var random2 = new Random(7);

        for (int id = 1; id <= 50; id++)
        {
            var first = PixelItem.Create(id, random1);
            var second = PixelItem.Create(id, random2);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal(first.Name, second.Name);
            Assert.Equal(first.Category, second.Category);
            Assert.Equal(first.Description, second.Description);
            Assert.Equal(first.Notes, second.Notes);
        }
    }
}
