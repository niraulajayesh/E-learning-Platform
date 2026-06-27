using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;

namespace DataLayer.Repositories.Implementations;

public class BannerRepository : Repository<Banner>, IBannerRepository
{
    public BannerRepository(AppDbContext context) : base(context) { }
}
