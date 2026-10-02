using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Scoreboard.WebApp.Data;

public static class DbErrors
{
    public static async Task SaveUniqueAsync(this DbContext db, string constraintFragment, Func<ArgumentException> conflict)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
            && violation.ConstraintName?.Contains(constraintFragment, StringComparison.OrdinalIgnoreCase) == true)
        {
            throw conflict();
        }
    }

    public static Task SaveUniqueAsync(this DbContext db, string constraintFragment, string message) =>
        db.SaveUniqueAsync(constraintFragment, () => new ArgumentException(message));
}
