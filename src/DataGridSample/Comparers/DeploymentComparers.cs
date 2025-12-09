// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using DataGridSample.Models;

namespace DataGridSample.Comparers
{
    public sealed class RingComparer : IComparer
    {
        private static readonly Dictionary<string, int> Order = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Canary"] = 0,
            ["R0"] = 1,
            ["R1"] = 2,
            ["R2"] = 3,
            ["R3"] = 4
        };

        public static RingComparer Instance { get; } = new RingComparer();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            var left = x as Deployment;
            var right = y as Deployment;

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int leftRank = GetRank(left.Ring);
            int rightRank = GetRank(right.Ring);

            int rankCompare = leftRank.CompareTo(rightRank);
            if (rankCompare != 0)
            {
                return rankCompare;
            }

            return string.Compare(left.Ring, right.Ring, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetRank(string ring)
        {
            if (string.IsNullOrWhiteSpace(ring))
            {
                return int.MaxValue;
            }

            return Order.TryGetValue(ring, out int rank) ? rank : int.MaxValue;
        }
    }

    public sealed class StatusComparer : IComparer
    {
        private static readonly Dictionary<string, int> Order = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Blocked"] = 0,
            ["Investigating"] = 1,
            ["Paused"] = 2,
            ["Rolling Out"] = 3,
            ["Completed"] = 4
        };

        public static StatusComparer Instance { get; } = new StatusComparer();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            var left = x as Deployment;
            var right = y as Deployment;

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int leftRank = GetRank(left.Status);
            int rightRank = GetRank(right.Status);

            int rankCompare = leftRank.CompareTo(rightRank);
            if (rankCompare != 0)
            {
                return rankCompare;
            }

            return string.Compare(left.Status, right.Status, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetRank(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return int.MaxValue;
            }

            return Order.TryGetValue(status, out int rank) ? rank : int.MaxValue;
        }
    }

    public sealed class ServiceNaturalComparer : IComparer
    {
        public static ServiceNaturalComparer Instance { get; } = new ServiceNaturalComparer();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            var left = x as Deployment;
            var right = y as Deployment;

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return CompareNatural(left.Service, right.Service);
        }

        private static int CompareNatural(string? left, string? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int i = 0;
            int j = 0;

            while (i < left.Length && j < right.Length)
            {
                char leftChar = left[i];
                char rightChar = right[j];

                if (char.IsDigit(leftChar) && char.IsDigit(rightChar))
                {
                    int leftNumber = ReadNumber(left, ref i);
                    int rightNumber = ReadNumber(right, ref j);

                    int numberCompare = leftNumber.CompareTo(rightNumber);
                    if (numberCompare != 0)
                    {
                        return numberCompare;
                    }
                }
                else
                {
                    leftChar = char.ToUpperInvariant(leftChar);
                    rightChar = char.ToUpperInvariant(rightChar);

                    int charCompare = leftChar.CompareTo(rightChar);
                    if (charCompare != 0)
                    {
                        return charCompare;
                    }

                    i++;
                    j++;
                }
            }

            return (left.Length - i).CompareTo(right.Length - j);
        }

        private static int ReadNumber(string text, ref int index)
        {
            int start = index;
            while (index < text.Length && char.IsDigit(text[index]))
            {
                index++;
            }

            if (int.TryParse(text.AsSpan(start, index - start), out int value))
            {
                return value;
            }

            return 0;
        }
    }
}
