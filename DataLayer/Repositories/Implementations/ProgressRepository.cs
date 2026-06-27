using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;

namespace DataLayer.Repositories.Implementations;

public class ProgressRepository : Repository<Progress>, IProgressRepository
{
    public ProgressRepository(AppDbContext context) : base(context) { }
}
