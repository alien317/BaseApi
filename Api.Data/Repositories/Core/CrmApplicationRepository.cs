using Api.Data.Data;
using Api.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories.Core
{
    public interface ICrmApplicationRepository : IRepository<Application>
    {
        Task<Application?> FindByCode(string code);
    }

    public class CrmApplicationRepository : BaseRepository<Application>, ICrmApplicationRepository
    {
        public CrmApplicationRepository(BaseApiDbContext context) : base(context) { }

        public Task<Application?> FindByCode(string code)
        {
            return _dbSet.SingleOrDefaultAsync(a => a.Code == code);
        }
    }
}
