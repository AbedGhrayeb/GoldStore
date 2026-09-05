// <copyright file="TenantHttpErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace WebUI.Middleware;

internal static class TenantHttpErrors
{
    public static async Task WriteAsync(HttpContext context, int statusCode, string code, string description)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new { code, description }, context.RequestAborted);
    }
}
