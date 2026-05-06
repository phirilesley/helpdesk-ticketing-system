using HelpDeskSystem.Domain.Interfaces;
using HelpDeskSystem.Persistence.Context;
using System.Threading.Tasks;

namespace HelpDeskSystem.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HelpDeskDbContext _context;

        public UnitOfWork(HelpDeskDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Rollback()
        {
            // Entity Framework tracks changes, so a rollback is typically just not calling SaveChanges
            // or clearing the tracker if needed.
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
