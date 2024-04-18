
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models.Core
{
    public class ApplicationUser : IdentityUser<int>
    {
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }

        public ApplicationUser()
        {
            RefreshTokens = new List<RefreshToken>();
        }
    }
    public class Role : IdentityRole<int>
    {
        public string? Description { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }

        public Role() : base()
        {
            Transactions = new List<Transaction>();
        }
        public Role(string roleName) : this()
        {
            this.Name = roleName;
        }
    }
    public class UserLogin : IdentityUserLogin<int>
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public UserLogin() : base() { }
    }
    public class UserClaim : IdentityUserClaim<int>
    {
        public UserClaim() : base() { }
    }
    public class UserRole : IdentityUserRole<int>
    {
        public UserRole() : base() { }
    }
    public class UserToken : IdentityUserToken<int>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public UserToken() : base() { }
    }
    public class RoleClaim : IdentityRoleClaim<int>
    {
        public RoleClaim() : base() { }
    }
}
