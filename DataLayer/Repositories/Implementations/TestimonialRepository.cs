using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;

namespace DataLayer.Repositories.Implementations;

public class TestimonialRepository : Repository<Testimonial>, ITestimonialRepository
{
    public TestimonialRepository(AppDbContext context) : base(context) { }
}
