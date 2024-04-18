using Api.Data.Data;
using Api.Data.Models;
using Api.Data.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories.Core
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {
        Task RemoveOldRefreshTokens(ApplicationUser applicationUser, int RefreshTokensTTL);
        Task<ApplicationUser?> FindByRefreshToken(string refreshToken);
    }

    public class ApplicationUserRepository : BaseRepository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(BaseApiDbContext context) : base(context)
        {

        }

        public override Task<ApplicationUser> Insert(ApplicationUser entity)
        {
            throw new InvalidOperationException("Direct Insert operation with ApplicationUser entity is not allowed. Use UserManager to insert new ApplicationUser instance.");
        }

        public override Task Delete(ApplicationUser entity)
        {
            throw new InvalidOperationException("Direct Delete operation with ApplicationUser entity is not allowed. Use UserManager to delete ApplicationUser instance.");
        }

        public Task RemoveOldRefreshTokens(ApplicationUser applicationUser, int RefreshTokensTTL)
        {
            ((List<RefreshToken>)applicationUser.RefreshTokens).RemoveAll(t => !t.IsActive && t.DateCreated.AddDays(RefreshTokensTTL) <= DateTime.Now);
            return Task.CompletedTask;
        }

        public async Task<ApplicationUser?> FindByRefreshToken(string refreshToken)
        {
            return await _dbSet.Include(u => u.RefreshTokens).SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));
        }
    }
}
