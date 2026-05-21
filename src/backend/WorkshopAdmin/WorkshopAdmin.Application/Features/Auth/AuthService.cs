namespace WorkshopAdmin.Application.Features.Auth;

using System.Data.Common;
using FluentValidation;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Login;
using WorkshopAdmin.Application.Features.Auth.Logout;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Application.Features.Auth.Refresh;
using WorkshopAdmin.Domain.Exceptions;

public sealed class AuthService(
    IDbConnectionFactory connectionFactory,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ILoginHistoryRepository loginHistoryRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator) : IAuthService
{
    private const string LoginMethod = "password";

    // Returned to the client for every credential / state failure so the API
    // never reveals which check failed (no user enumeration). The specific
    // reason is still written to auth.login_history.
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        AuthUser? user = await userRepository.FindByEmailAsync(request.Email, connection, cancellationToken);

        // auth.login_history.user_id is NOT NULL, so a failed attempt for an unknown email cannot be recorded.
        if (user is null)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        string? failureReason = EvaluateLoginFailure(user, request.Password);
        if (failureReason is not null)
        {
            await loginHistoryRepository.RecordAsync(
                user.Id, LoginMethod, ipAddress, userAgent, success: false, failureReason, connection, transaction: null, cancellationToken);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        IReadOnlyList<string> roles = await userRepository.GetRoleNamesAsync(user.Id, connection, cancellationToken);
        IReadOnlyList<string> permissions = await userRepository.GetPermissionNamesAsync(user.Id, connection, cancellationToken);

        AccessToken accessToken = jwtTokenService.GenerateAccessToken(user, roles, permissions);
        GeneratedRefreshToken refreshToken = jwtTokenService.GenerateRefreshToken();

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await refreshTokenRepository.InsertAsync(
                new RefreshTokenRecord
                {
                    UserId = user.Id,
                    TokenHash = refreshToken.TokenHash,
                    ExpiresAt = refreshToken.ExpiresAt
                },
                connection, transaction, cancellationToken);

            await loginHistoryRepository.RecordAsync(
                user.Id, LoginMethod, ipAddress, userAgent, success: true, failureReason: null, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.RawToken,
            refreshToken.ExpiresAt,
            new AuthenticatedUser(user.Id, user.Email, user.FirstName, user.LastName, user.TenantId, roles));
    }

    public async Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        RefreshTokenRecord? stored = await refreshTokenRepository.FindByHashAsync(tokenHash, connection, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // A presented-but-already-revoked token means the token was either
        // rotated normally (and replayed) or stolen. Treat conservatively:
        // revoke the user's entire active token family.
        if (stored.RevokedAt is not null)
        {
            await using DbTransaction reuseTx = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, connection, reuseTx, cancellationToken);
                await reuseTx.CommitAsync(cancellationToken);
            }
            catch
            {
                await reuseTx.RollbackAsync(cancellationToken);
                throw;
            }

            throw new UnauthorizedException("Refresh token has been revoked.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        AuthUser? user = await userRepository.FindByIdAsync(stored.UserId, connection, cancellationToken);
        if (user is null || EvaluateAccountState(user) is not null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        IReadOnlyList<string> roles = await userRepository.GetRoleNamesAsync(user.Id, connection, cancellationToken);
        IReadOnlyList<string> permissions = await userRepository.GetPermissionNamesAsync(user.Id, connection, cancellationToken);

        AccessToken accessToken = jwtTokenService.GenerateAccessToken(user, roles, permissions);
        GeneratedRefreshToken newRefreshToken = jwtTokenService.GenerateRefreshToken();

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            Guid newTokenId = await refreshTokenRepository.InsertAsync(
                new RefreshTokenRecord
                {
                    UserId = user.Id,
                    TokenHash = newRefreshToken.TokenHash,
                    ExpiresAt = newRefreshToken.ExpiresAt
                },
                connection, transaction, cancellationToken);

            await refreshTokenRepository.RevokeAsync(stored.Id, newTokenId, connection, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new RefreshResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            newRefreshToken.RawToken,
            newRefreshToken.ExpiresAt);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await logoutValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        string tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        RefreshTokenRecord? stored = await refreshTokenRepository.FindByHashAsync(tokenHash, connection, cancellationToken);

        // Unknown or already-revoked: no-op, idempotent.
        if (stored is null || stored.RevokedAt is not null)
        {
            return;
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await refreshTokenRepository.RevokeAsync(stored.Id, replacedByTokenId: null, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await refreshTokenRepository.RevokeAllForUserAsync(userId, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Returns a login_history failure reason, or null when the credentials and
    /// account state are all valid.
    /// </summary>
    private string? EvaluateLoginFailure(AuthUser user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            // External-auth account (AD / Google) — no local password to verify.
            return "external_auth_only";
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            return "invalid_password";
        }

        return EvaluateAccountState(user);
    }

    private static string? EvaluateAccountState(AuthUser user)
    {
        if (!user.IsActive)
        {
            return "account_inactive";
        }

        // Platform admins have no tenant (TenantId NULL); tenant users must
        // belong to an active, non-deleted tenant.
        if (user.TenantId is not null && user.TenantIsActive != true)
        {
            return "tenant_inactive";
        }

        return null;
    }
}
