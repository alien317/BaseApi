using Api.Data.Models.Core;
using Api.Data.Repositories.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Api.Common.Middleware
{
    public class AdminMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, RoleManager<Role> roleManager, ITransactionRepository transactionsRepository)
        {
            var transactions = await transactionsRepository.GetAll();
            var role = await roleManager.FindByNameAsync("Admin");

            foreach(var transaction in transactions)
            {
                if(!role?.Transactions.Contains(transaction) ?? false) role?.Transactions.Add(transaction);
            }

            await _next(context);
        }
    }
}
