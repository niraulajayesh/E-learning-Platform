using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class SiteSettingRepository : Repository<SiteSetting>, ISiteSettingRepository
{
    public SiteSettingRepository(AppDbContext context) : base(context) { }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var setting = await _dbSet.FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }
}
