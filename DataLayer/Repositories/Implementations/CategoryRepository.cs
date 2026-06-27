using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;

namespace DataLayer.Repositories.Implementations;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }
}
