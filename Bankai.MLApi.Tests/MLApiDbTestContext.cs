using Bankai.MLApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Bankai.MLApi.Tests;

public record MLApiDbTestContext : IDisposable
{
    private readonly MLApiDbContext _mlApiDatabaseContext = new(
        new DbContextOptionsBuilder<MLApiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public MLApiDbContext MLApiDatabaseContext() =>
        _mlApiDatabaseContext;

    public async Task ChangeAsync(Func<MLApiDbContext, Task> actionAsync)
    {
        await actionAsync(_mlApiDatabaseContext);
        await _mlApiDatabaseContext.SaveChangesAsync();
    }

    public void Dispose() =>
        _mlApiDatabaseContext.Dispose();
}
