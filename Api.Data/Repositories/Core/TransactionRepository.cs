using Api.Data.Data;
using Api.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories.Core
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<Transaction?> FindByCode(string code);
    }

    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(BaseApiDbContext dbContext) : base(dbContext)
        {
        }

        public Task<Transaction?> FindByCode(string code)
        {
            return _dbSet.SingleOrDefaultAsync(t => t.Code == code);
        }
    }
}
