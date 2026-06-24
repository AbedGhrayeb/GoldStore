using System;
using System.Collections.Generic;
using System.Text;
using Application.Todos.Create;
using Application.Users.Login;
using FluentValidation;

namespace Application.Features.Users.Login;

    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(c => c.Email).NotEmpty().EmailAddress();
            RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(20);
        }
    }
