using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Api.Data.Models.Core;
using System.Text.Json;

namespace Api.Data.Data
{
    public class BaseApiDbContext : IdentityDbContext<ApplicationUser, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
    {
        public BaseApiDbContext(DbContextOptions<BaseApiDbContext> options) : base(options)
        {
        }

        public DbSet<Application> Applications { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Transaction> Transactions { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<UserClaim>().ToTable("UserClaims");
            builder.Entity<UserLogin>().ToTable("UserLogins");
            builder.Entity<UserToken>().ToTable("UserTokens");

            builder.Entity<UserRole>().ToTable("UserRoles");

            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<Role>().Navigation(r => r.Transactions).AutoInclude();
            builder.Entity<RoleClaim>().ToTable("RoleClaims");

            builder.Entity<Module>().Property(m => m.Code).HasMaxLength(25);
            builder.Entity<Module>().HasIndex(m => m.Code).IsUnique();
            builder.Entity<Module>().Navigation(m => m.Applications).AutoInclude();
            builder.Entity<Module>()
                .HasMany(m => m.Applications)
                .WithOne(a => a.Module);
            //.HasForeignKey(a => a.ModuleId);

            builder.Entity<Application>().Property(m => m.Code).HasMaxLength(25);
            builder.Entity<Application>().HasIndex(m => m.Code);
            builder.Entity<Application>().Navigation(m => m.Transactions).AutoInclude();
            builder.Entity<Application>()
                .HasMany(a => a.Transactions)
                .WithOne(t => t.Application);
            //.HasForeignKey(t => t.ApplicationId);

            builder.Entity<Transaction>().Property(tr => tr.Code).HasMaxLength(50);
            builder.Entity<Transaction>().HasIndex(tr => tr.Code).IsUnique();
            builder.Entity<Transaction>()
                .HasMany(tr => tr.Roles)
                .WithMany(r => r.Transactions);
        }

        public async Task Seed()
        {
            List<Role> roles = new List<Role>()
            {
                new Role("Admin"){
                    Name = "Admin",
                    Description = "Základní role pro správu aplikace. Je vytvořena při prvním spuštění, nejvyšší oprávnění",
                    NormalizedName = "ADMIN"
                }
            };

            foreach (var role in roles)
            {
                if (!Roles.Any(r => r.Name == role.Name))
                {
                    await Roles.AddAsync(role);
                };
            };

            await LoadJSONTransaction();

        }



        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetDates();

            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            SetDates();

            return base.SaveChanges();
        }

        private void SetDates()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                if (entry.Entity is BaseEntity baseEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            baseEntity.DateCreated = DateTime.Now;
                            break;

                        case EntityState.Modified:
                            entry.Property("DateCreated").IsModified = false;
                            if (baseEntity.DateDeleted == null)
                            {
                                baseEntity.DateUpdated = DateTime.Now;
                            }
                            else
                            {
                                entry.Property("DateUpdated").IsModified = false;
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Načtení struktury modulu / apliací / transakcí z JSON souboru
        /// </summary>
        private async Task LoadJSONTransaction()
        {

            // Načtení obsahu JSON souboru do řetězce          
            string json = File.ReadAllText("./Config/Transactions.json");
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty(nameof(Module), out JsonElement modules))
                {
                    foreach (JsonElement module in modules.EnumerateArray())
                    {
                        Module moduleObj = new()
                        {
                            Name = module.GetProperty("Name").GetString(),
                            Code = module.GetProperty("Code").GetString()
                        };

                        if (!Modules.Any(m => m.Code == moduleObj.Code)) { await Modules.AddAsync(moduleObj); }
                    }

                    await SaveChangesAsync();

                    Dictionary<string, Module> moduleList = new Dictionary<string, Module>();
                    foreach (Module module in Modules)
                    {
                        moduleList[module.Code] = module;
                    }

                    foreach (JsonElement module in modules.EnumerateArray())
                    {
                        if (module.TryGetProperty(nameof(Application), out JsonElement applications))
                        {
                            foreach (JsonElement application in applications.EnumerateArray())
                            {
                                Application applicationObj = new()
                                {
                                    Module = moduleList[module.GetProperty("Code").GetString()],
                                    Name = application.GetProperty("Name").GetString(),
                                    Code = application.GetProperty("Code").GetString(),
                                    Order = application.GetProperty("Order").GetInt32(),

                                };

                                if (!Applications.Any(a => a.Code == applicationObj.Code))
                                {
                                    await Applications.AddAsync(applicationObj);
                                    Console.WriteLine("Ukládám aplikaci " + applicationObj.Code);
                                }
                                else
                                {
                                    Console.WriteLine("NEUKLÁDÁM " + applicationObj.Code);
                                }
                            }
                        }
                    }

                    await SaveChangesAsync();

                    Dictionary<string, Application> applicationList = new Dictionary<string, Application>();
                    foreach (Application application in Applications)
                    {
                        applicationList[application.Code] = application;
                        Console.WriteLine(application.Code);
                    }

                    foreach (JsonElement module in modules.EnumerateArray())
                    {
                        if (module.TryGetProperty(nameof(Application), out JsonElement applications))
                        {
                            foreach (JsonElement application in applications.EnumerateArray())
                            {
                                if (application.TryGetProperty(nameof(Transaction), out JsonElement transactions))
                                {
                                    foreach (JsonElement transaction in transactions.EnumerateArray())
                                    {
                                        Transaction transacionObj = new()
                                        {
                                            Application = applicationList[application.GetProperty("Code").GetString()],
                                            Name = transaction.GetProperty("Name").GetString(),
                                            TransactionUrl = transaction.GetProperty("TransactionUrl").GetString().ToLower(),
                                            Order = transaction.GetProperty("Order").GetInt32(),
                                            ShowInMenu = transaction.GetProperty("ShowInMenu").GetBoolean(),
                                            Code = transaction.GetProperty("Code").GetString(),
                                        };

                                        if (!Transactions.Any(a => a.Code == transacionObj.Code))
                                        {
                                            await Transactions.AddAsync(transacionObj);
                                            Console.WriteLine("Ukládám transakci " + transacionObj.Code);
                                        }
                                        else
                                        {
                                            Console.WriteLine("NEUKLÁDÁM " + transacionObj.Code);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await SaveChangesAsync();
                }
            }
        }
    }
}
