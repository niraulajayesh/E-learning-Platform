using DataLayer.Entities;
namespace DataLayer.Repositories.Interfaces;

public interface ISiteSettingRepository : IRepository<SiteSetting>
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
}
