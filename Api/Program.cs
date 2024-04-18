using Api.Common.Attributes;
using Api.Common.Middleware;
using Api.Common.Models.Configuration;
using Api.Common.Services.Core;
using Api.Common.Utils;
using Api.Data.Data;
using Api.Data.Models.Core;
using Api.Data.Repositories.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;
            var connectionString = builder.Configuration.GetConnectionString("BaseApiDbContext") ?? throw new InvalidOperationException("Connection string 'crmDbContext' not found.");
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(serilogLogger);

            //configuration from AppSettings.json
            services.Configure<JwtConfiguration>(builder.Configuration.GetSection("Jwt"));

            services.AddDbContext<BaseApiDbContext>(options => options.UseSqlServer(connectionString));

            services.AddControllers(options =>
            {
                options.Conventions.Add(new ControllerDocumentationConvention());
            });

            services.AddDefaultIdentity<ApplicationUser>(
                options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<Role>()
                .AddEntityFrameworkStores<BaseApiDbContext>();
            
            services.AddTransient<IApplicationUserRepository, ApplicationUserRepository>();
            services.AddTransient<ICrmApplicationRepository, CrmApplicationRepository>();
            services.AddTransient<IModuleRepository, ModuleRepository>();
            services.AddTransient<ITransactionRepository, TransactionRepository>();

            services.AddTransient<ApiAuthorizeFilter>();

            services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
            services.AddScoped<IUsersAndRolesManagementService, UsersAndRolesManagementService>();
            services.AddScoped<ITransactionsService, TransactionsService>();
            services.AddScoped<IJwtUtils, JwtUtils>();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new() { 
                    Title = "Api", 
                    Version = "v1",
                    Description = "Api pro pøístup.",
                    Contact = new OpenApiContact()
                    {
                        Name = "Radek Novák",
                        Email = "novakr.radek@seznam.cz"
                    }
                });

                // JWT authorization
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT autorizace. \r\n\r\n Vložte Váš token.\r\n\r\nNapø: \"easyTest345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });


                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), true);
            });

            services.AddAutoMapper(config =>
            {
                config.AddProfile<AutomapperConfig>();
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(setup =>
            {
                setup.RoutePrefix = "swagger";
                setup.SwaggerEndpoint("v1/swagger.json", "CRM");
            });

            app.UseMiddleware<JwtMiddleware>()
                .UseMiddleware<AdminMiddleware>();
            app.MapControllers();

            //Checks all migrations, wether they were already applied to database
            //If they weren't, applies them and calls seeding method od db context
            using(var scope = app.Services.CreateScope())
            {
                BaseApiDbContext context = scope.ServiceProvider.GetRequiredService<BaseApiDbContext>();
                //For testing purposes, if empty database is desired, uncomment following line. This will delete entire database during start of the program and build database from scratch
                //context.Database.EnsureDeleted();
                if(context.Database.GetPendingMigrations().Count() > 0)
                {
                    var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();

                    migrator.Migrate();
                }

                context.Seed().Wait();
            }

            app.Run();
        }
    }
}
