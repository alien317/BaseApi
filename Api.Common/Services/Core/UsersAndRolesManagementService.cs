using Api.Common.Models.ApiRequests;
using Api.Common.Models.DTOs.Core;
using Api.Data.Models.Core;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Common.Services.Core
{
    public interface IUsersAndRolesManagementService
    {
        Task CreateUser(string username, string password);
        Task<UserDTO?> GetUser(GetUserRequest request);
        Task<UserDTO?> UpdateUser(UpdateUserRequest request);
        Task DeleteUser(string username);
        Task<List<UserDTO>> GetAllUsers();
        Task<List<RoleDTO>> GetUserRoles(ApplicationUser? user);
        Task CreateRole(string roleName);
        Task<List<RoleDTO>> GetAllRoles();
    }

    public class UsersAndRolesManagementService : IUsersAndRolesManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public UsersAndRolesManagementService(UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager, SignInManager<ApplicationUser> signInManager,
            IUserStore<ApplicationUser> userStore,
            IMapper mapper, ILogger<UsersAndRolesManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _mapper = mapper;
            _logger = logger;
        }

        #region Public methods
        public async Task CreateUser(string username, string password)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var firstUser = _userManager.Users.Count() == 0;
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, username, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, password);

                {
                    if (result.Succeeded)
                    {
                        if (firstUser)
                        {
                            user.EmailConfirmed = true;
                            await _userManager.UpdateAsync(user);
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }

                        _logger.LogInformation("User created a new account with password.");
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        return;
                    }
                    string errorMessage = string.Empty;

                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Chyba při vytváření uživatele {username}");
                        errorMessage += $"{error.Description};";
                    }
                    throw new Exception($"Nastala chyba při vytváření uživatele:{errorMessage}");
                }
            }
        }

        public async Task<UserDTO?> GetUser(GetUserRequest request)
        {
            if (request.Id != null)
                return _mapper.Map<UserDTO>(await _userManager.FindByIdAsync(request.Id.Value.ToString()));
            if (!string.IsNullOrEmpty(request.UserName))
                return _mapper.Map<UserDTO>(await _userManager.FindByEmailAsync(request.UserName));
            return null;
        }

        public async Task<UserDTO?> UpdateUser(UpdateUserRequest request)
        {
            ApplicationUser? user = null;

            if (request.Id != null)
                user = await _userManager.FindByIdAsync(request.Id.Value.ToString());
            if (user == null && !string.IsNullOrEmpty(request.UserName))
                user = await _userManager.FindByEmailAsync(request.UserName);

            if (user == null) return null;

            if (!string.IsNullOrEmpty(request.NewPassword) && !string.IsNullOrEmpty(request.OldPassword))
                await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (request.User != null)
            {
                //Tahle čuněčina hlídá null hodnoty na booleanech u UserDTO a pokud jsou null, nastaví je na hodnoty z ApplicationUser instance, protože jinak tam mapper mapoval default hodnoty
                foreach (var property in request.User.GetType().GetProperties())
                {
                    var type = property.PropertyType;
                    var boolType = typeof(bool?);

                    if (property.PropertyType == typeof(bool?) && property.GetValue(request.User) == null)
                    {
                        property.SetValue(request.User, user.GetType().GetProperty(property.Name)?.GetValue(user));
                    }
                }

                await _userManager.UpdateAsync(_mapper.Map(request.User, user));
            }

            return _mapper.Map<UserDTO>(await _userManager.FindByIdAsync(user.Id.ToString()));
        }

        public async Task DeleteUser(string username)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(username);
            if (user == null) throw new ArgumentException($"Uživatel s emailem '{username}' nebyl nalezen.");
            await _userManager.DeleteAsync(user);
        }

        public async Task<List<UserDTO>> GetAllUsers()
        {
            return _mapper.Map<List<UserDTO>>(await _userManager.Users.ToListAsync());
        }

        public async Task<List<RoleDTO>> GetUserRoles(ApplicationUser? user)
        {
            if (user == null) return new();

            List<Role> roles = new();
            foreach (var roleName in await _userManager.GetRolesAsync(user))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    roles.Add(role);
                }
            }
            return _mapper.Map<List<RoleDTO>>(roles);
        }

        public async Task CreateRole(string roleName)
        {
            var result = await _roleManager.CreateAsync(new Role(roleName));

            if (result.Succeeded)
            {
                _logger.LogInformation($"Role '{roleName}' byla vytvořena.");
                return;
            }
            string errorMessage = string.Empty;

            foreach (var error in result.Errors)
            {
                _logger.LogError($"Chyba při vytváření role '{roleName}'");
                errorMessage += $"{error.Description};";
            }
            throw new Exception($"Nastala chyba při vytváření role:{errorMessage}");
        }

        public async Task<List<RoleDTO>> GetAllRoles()
        {
            return _mapper.Map<List<RoleDTO>>(await _roleManager.Roles.ToListAsync());
        }
        #endregion

        #region Private methods
        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
        #endregion
    }
}
