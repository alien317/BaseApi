using Blazored.LocalStorage;

namespace Api.Common.Services.Core
{
    public interface IClientLocalStorageService
    {
        Task SetLocalStorageAsync(string key, string value);
        Task RemoveLocalStorageAsync(string key);
        Task<string?> GetLocalStorageAsync(string key);
    }

    public class ClientLocalStorageService(ILocalStorageService localStorageService) : IClientLocalStorageService
    {
        private readonly ILocalStorageService _localStorageService = localStorageService;

        public async Task SetLocalStorageAsync(string key, string value) => await _localStorageService.SetItemAsync<string>(key, value);

        public async Task RemoveLocalStorageAsync(string key) => await _localStorageService.RemoveItemAsync(key);

        public async Task<string?> GetLocalStorageAsync(string key) => await _localStorageService.GetItemAsync<string>(key);
    }
}
