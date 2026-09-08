// <copyright file="TestAuth.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace Application.IntegrationTests;

/// <summary>
/// Performs a real cookie login against the MVC app. The full flow is exercised:
/// GET the login page, extract the antiforgery token, POST the credentials, and let
/// the factory client follow the redirect. Cookie and antiforgery state is handled by
/// the client itself (HandleCookies = true), so the returned client is authenticated.
/// </summary>
public static class TestAuth
{
    private static readonly Regex TokenPattern = new(
        @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task<bool> LoginAsync(HttpClient client, string email, string password)
    {
        HttpResponseMessage page = await client.GetAsync("/Account/Login");
        if (!page.IsSuccessStatusCode)
        {
            return false;
        }

        string html = await page.Content.ReadAsStringAsync();
        Match match = TokenPattern.Match(html);
        if (!match.Success)
        {
            return false;
        }

        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["Username"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = match.Groups[1].Value,
        });

        HttpResponseMessage response = await client.PostAsync("/Account/Login", form);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        // Successful login redirects away from the login page to the home page.
        return !response.RequestMessage?.RequestUri?.AbsolutePath.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>Fetches an antiforgery token by rendering a page/form that contains one.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task<string?> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        Match match = TokenPattern.Match(await response.Content.ReadAsStringAsync());
        return match.Success ? match.Groups[1].Value : null;
    }
}
