// <copyright file="Error.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SharedKernel.Result;

public record Error
{
    private Error(string code, string description, ErrorType type)
    {
        this.Code = code;
        this.Description = description;
        this.Type = type;
    }

    public string Code { get; }

    public string Description { get; }

    public ErrorType Type { get; }

    public static Error Failure(string code = nameof(Failure), string description = "General failure.")
        => new(code, description, ErrorType.Failure);

    public static Error Unexpected(string code = nameof(Unexpected), string description = "Unexpected error.")
        => new(code, description, ErrorType.Unexpected);

    public static Error Validation(string code = nameof(Validation), string description = "Validation error")
        => new(code, description, ErrorType.Validation);

    public static Error Conflict(string code = nameof(Conflict), string description = "Conflict error")
        => new(code, description, ErrorType.Conflict);

    public static Error NotFound(string code = nameof(NotFound), string description = "Not found error")
        => new(code, description, ErrorType.NotFound);

    public static Error Unauthorized(string code = nameof(Unauthorized), string description = "Unauthorized error")
        => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code = nameof(Forbidden), string description = "Forbidden error")
        => new(code, description, ErrorType.Forbidden);

    public static Error Create(int type, string code, string description)
        => new(code, description, (ErrorType)type);
}
