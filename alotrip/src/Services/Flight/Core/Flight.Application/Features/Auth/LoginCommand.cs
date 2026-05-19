using Flight.Application.Interfaces;
using FluentValidation;
using MediatR;
using Shared.BuildingBlocks.CQRS;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// LOGIN
// ─────────────────────────────────────────────────────────────────────────────

public sealed record LoginCommand(string AgentCode, string Password)
    : ICommand<TokenResultDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.AgentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(IJwtTokenService tokenService)
    : ICommandHandler<LoginCommand, TokenResultDto>
{
    public async Task<TokenResultDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var result = await tokenService.LoginAsync(cmd.AgentCode, cmd.Password, ct);

        if (result is null)
            throw new UnauthorizedException("Invalid agent code or password.");

        return result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// REFRESH
// ─────────────────────────────────────────────────────────────────────────────

public sealed record RefreshTokenCommand(string RefreshToken)
    : ICommand<TokenResultDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(IJwtTokenService tokenService)
    : ICommandHandler<RefreshTokenCommand, TokenResultDto>
{
    public async Task<TokenResultDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var result = await tokenService.RefreshAsync(cmd.RefreshToken, ct);

        if (result is null)
            throw new UnauthorizedException("Refresh token is invalid or expired.");

        return result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// REVOKE (logout)
// ─────────────────────────────────────────────────────────────────────────────

public sealed record RevokeTokenCommand(string RefreshToken)
    : ICommand<Unit>;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RevokeTokenCommandHandler(IJwtTokenService tokenService)
    : ICommandHandler<RevokeTokenCommand, Unit>
{
    public async Task<Unit> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        await tokenService.RevokeAsync(cmd.RefreshToken, ct);
        return Unit.Value;
    }
}
