using Microsoft.AspNetCore.Http;

namespace WorkshopAdmin.SharedKernel.Database;

/// <summary>
/// Completes the per-request DB session: commit when the pipeline finishes normally,
/// rollback when it throws. Sits inside ExceptionHandlingMiddleware so the rollback
/// happens before the exception is turned into a 500 response.
/// </summary>
public sealed class DbSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDbSession session)
    {
        try
        {
            await next(context);
        }
        catch
        {
            await session.RollbackAsync(CancellationToken.None);
            throw;
        }

        await session.CommitAsync(CancellationToken.None);
    }
}
