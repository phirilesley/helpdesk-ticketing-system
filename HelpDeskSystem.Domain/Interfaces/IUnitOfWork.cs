using System;
using System.Threading.Tasks;

namespace HelpDeskSystem.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        void Rollback();
    }
}
