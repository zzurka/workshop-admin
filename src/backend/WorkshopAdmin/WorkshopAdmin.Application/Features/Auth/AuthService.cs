namespace WorkshopAdmin.Application.Features.Auth;

using System.Data.Common;
using System.Security.Cryptography;
using FluentValidation;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.External;
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
    IExternalLoginRepository externalLoginRepository,
    IExternalAuthRegistry externalAuthRegistry,
    IExternalStateCache externalStateCache,
    IExternalHandoffCache externalHandoffCache,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator,
    IValidator<ExternalExchangeRequest> externalExchangeValidator) : IAuthService
{
    private const string LoginMethodPassword = "password";

    // Returned to the client for every credential / state failure so the API
    // never reveals which check failed (no user enumeration). The specific
    // reason is still written to auth.login_history.
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    
    private const string ExternalLoginRejectedMessage = "External sign-in could not be completed.";

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

        string? failureReason = EvaluatePasswordLoginFailure(user, request.Password);
        if (failureReason is not null)
        {
            await loginHistoryRepository.RecordAsync(
                user.Id, LoginMethodPassword, ipAddress, userAgent, success: false, failureReason, connection, transaction: null, cancellationToken);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return await IssueLoginAsync(user, LoginMethodPassword, ipAddress, userAgent, connection, postCommit: null, cancellationToken);
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

    public async Task<ExternalStartResponse> ExternalStartAsync(
        string providerCode, string redirectUri, CancellationToken cancellationToken)
    {
        IExternalAuthClient client = externalAuthRegistry.Get(providerCode)
            ?? throw new NotFoundException("ExternalAuthProvider", providerCode);

        string codeVerifier = GenerateUrlSafeToken(64);
        string codeChallenge = PkceChallenge(codeVerifier);
        string state = GenerateUrlSafeToken(32);

        await externalStateCache.SetAsync(state, new ExternalStateEntry(providerCode, codeVerifier, redirectUri), cancellationToken);

        string authorizeUrl = await client.BuildAuthorizeUrlAsync(state, codeChallenge, redirectUri, cancellationToken);
        return new ExternalStartResponse(authorizeUrl);
    }

    public async Task<string> ExternalCallbackAsync(
        string providerCode, string code, string state, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        ExternalStateEntry? stateEntry = await externalStateCache.TakeAsync(state, cancellationToken);
        if (stateEntry is null || !string.Equals(stateEntry.Provider, providerCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException(ExternalLoginRejectedMessage);
        }

        IExternalAuthClient client = externalAuthRegistry.Get(providerCode)
            ?? throw new NotFoundException("ExternalAuthProvider", providerCode);

        ExternalIdentity identity = await client.ExchangeCodeAsync(code, stateEntry.CodeVerifier, stateEntry.RedirectUri, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        string loginMethod = providerCode;

        // Match: existing link → user; else verified-email → auto-link.
        ExternalLoginRecord? link = await externalLoginRepository.FindAsync(identity.Provider, identity.Subject, connection, cancellationToken);

        AuthUser? user;
        Guid? linkIdToTouch;
        bool createLink;

        if (link is not null)
        {
            user = await userRepository.FindByIdAsync(link.UserId, connection, cancellationToken);
            linkIdToTouch = link.Id;
            createLink = false;
        }
        else
        {
            if (!identity.EmailVerified)
            {
                throw new UnauthorizedException(ExternalLoginRejectedMessage);
            }

            user = await userRepository.FindByEmailAsync(identity.Email, connection, cancellationToken);
            linkIdToTouch = null;
            createLink = user is not null;
        }

        if (user is null)
        {
            // No invitation. login_history.user_id is NOT NULL, so this is not recorded.
            throw new UnauthorizedException(ExternalLoginRejectedMessage);
        }

        string? accountFailure = EvaluateAccountState(user);
        if (accountFailure is not null)
        {
            await loginHistoryRepository.RecordAsync(
                user.Id, loginMethod, ipAddress, userAgent, success: false, accountFailure, connection, transaction: null, cancellationToken);
            throw new UnauthorizedException(ExternalLoginRejectedMessage);
        }

        LoginResponse login = await IssueLoginAsync(
            user, loginMethod, ipAddress, userAgent, connection,
            postCommit: async (conn, tx) =>
            {
                if (createLink)
                {
                    await externalLoginRepository.InsertAsync(
                        user.Id, identity.Provider, identity.Subject, identity.Email, conn, tx, cancellationToken);
                }
                else if (linkIdToTouch is Guid linkId)
                {
                    await externalLoginRepository.UpdateLastLoginAsync(linkId, conn, tx, cancellationToken);
                }
            },
            cancellationToken);

        string handoffCode = GenerateUrlSafeToken(32);
        await externalHandoffCache.SetAsync(handoffCode, login, cancellationToken);
        return handoffCode;
    }

    public async Task<LoginResponse> ExternalExchangeAsync(ExternalExchangeRequest request, CancellationToken cancellationToken)
    {
        await externalExchangeValidator.ValidateAndThrowAsync(request, cancellationToken);

        LoginResponse? payload = await externalHandoffCache.TakeAsync(request.HandoffCode, cancellationToken);
        if (payload is null)
        {
            throw new UnauthorizedException("Handoff code is invalid or has expired.");
        }

        return payload;
    }

    /// <summary>
    /// Shared end-of-login path: loads roles + permissions, issues an access
    /// token, inserts a new refresh token, records login_history success, and
    /// (optionally) runs <paramref name="postCommit"/> work inside the same tx
    /// before commit.
    /// </summary>
    private async Task<LoginResponse> IssueLoginAsync(
        AuthUser user,
        string loginMethod,
        string? ipAddress,
        string? userAgent,
        DbConnection connection,
        Func<DbConnection, DbTransaction, Task>? postCommit,
        CancellationToken cancellationToken)
    {
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
                user.Id, loginMethod, ipAddress, userAgent, success: true, failureReason: null, connection, transaction, cancellationToken);

            if (postCommit is not null)
            {
                await postCommit(connection, transaction);
            }

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

    /// <summary>
    /// Returns a login_history failure reason, or null when the credentials and
    /// account state are all valid.
    /// </summary>
    private string? EvaluatePasswordLoginFailure(AuthUser user, string password)
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

    private static string GenerateUrlSafeToken(int byteLength)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string PkceChallenge(string codeVerifier)
    {
        byte[] hash = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
