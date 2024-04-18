using Api.Data.Data;
using Api.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories.Core
{
    public interface IModuleRepository : IRepository<Module>
    {
        Task<Module?> FindByCode(string code);
    }

    public class ModuleRepository : BaseRepository<Module>, IModuleRepository
    {
        public ModuleRepository(BaseApiDbContext context) : base(context) { }

        public Task<Module?> FindByCode(string code)
        {
            return _dbSet.SingleOrDefaultAsync(m => m.Code == code);
        }
    }
}
