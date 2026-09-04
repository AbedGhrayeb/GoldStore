using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Features.Identity;
using Application.Features.Identity.Dtos;
using Application.Features.Identity.GetUserInfo;
using Application.Features.Identity.Queries.RefreshTokens;
using Application.Features.Identity.Token.GenerateToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.ApiControllers;

public sealed class IdentityController(
    IQueryHandler<GenerateTokenQuery, TokenResponse> tokenHandler,
    IQueryHandler<GetUserInfoByIdQuery, UserDto> userInfoHandler,
    IQueryHandler<RefreshTokenQuery, TokenResponse> refreshTokenHandler
    ) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Generates an access and refresh token for a valid user.")]
    [EndpointDescription("Authenticates a user using provided credentials and returns a JWT token pair.")]
    [EndpointName("GenerateToken")]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenQuery request, CancellationToken ct)
    {
        var result = await tokenHandler.Handle(request, ct);
        return result.Match(
            response => Ok(response),
            Problem);
    }
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Refreshes access token using a valid refresh token.")]
    [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair.")]
    [EndpointName("RefreshToken")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenQuery request, CancellationToken ct)
    {
        var result = await refreshTokenHandler.Handle(request, ct);
        return result.Match(
            response => Ok(response),
            Problem);
    }
    [HttpGet("current-user")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Gets the current authenticated user's info.")]
    [EndpointDescription("Returns user information for the currently authenticated user based on the access token.")]
    [EndpointName("GetCurrentUserInfo")]
    public async Task<IActionResult> GetCurrentUserInfo(CancellationToken ct)
    {
        var result = await userInfoHandler.Handle(new GetUserInfoByIdQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}
