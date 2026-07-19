namespace WorkshopAdmin.Application.Features.Auth.PasswordReset;

public sealed record CompletePasswordResetRequest(string Token, string NewPassword);
