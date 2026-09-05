// <copyright file="Karat.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Common;

public enum Karat
{
    K18 = 18,
    K21 = 21,
    K24 = 24,
}

public static class KaratExtentions
{
    public static string KaratLabel(this Karat karat)
    {
        return karat switch
        {
            Karat.K18 => "عيار 18",
            Karat.K21 => "عيار 21",
            Karat.K24 => "عيار 24",
            _ => karat.ToString(),

        };
    }
}
