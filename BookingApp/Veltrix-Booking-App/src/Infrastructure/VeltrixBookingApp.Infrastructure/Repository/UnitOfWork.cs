using System.Threading.Tasks;
using VeltrixBookingApp.Domain.Repositories;
using VeltrixBookingApp.Infrastructure.Persistence;

namespace VeltrixBookingApp.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
