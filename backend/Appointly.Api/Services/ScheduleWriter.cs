using Appointly.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Services;

// Every booking, cancellation, status change, and availability edit uses this same policy.
public sealed class ScheduleWriter(AppointlyDbContext database)
{
    public async Task<T> WriteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            await database.Database.OpenConnectionAsync(cancellationToken);
            var connection = (SqliteConnection)database.Database.GetDbConnection();
            // Lock before reading rules so competing writes cannot invalidate those reads.
            using var transaction = connection.BeginTransaction(deferred: false);
            await using var enlisted = await database.Database.UseTransactionAsync(transaction, cancellationToken);
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            throw new BookingException(409, "This slot or date already exists. Refresh and try again.");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6)
        {
            throw Busy();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 5 or 6 })
        {
            throw Busy();
        }
        finally { await database.Database.CloseConnectionAsync(); }
    }

    private static BookingException Busy() => new(409, "The schedule is busy. Refresh and try again.");
}
