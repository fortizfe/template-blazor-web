using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Template.Blazor.Web.Services.HttpServices
{
    public interface IAuthService
    {
        Task<string> GetAccessToken();
    }

    public class AuthService : IAuthService
    {
        private HttpClient _client;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _config;

        private static readonly object _lock = new object();

        private const int cacheExpirationInDays = 1;

        private class AccessTokenItem
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime Expiry { get; set; }
        }

        public AuthService(IDistributedCache cache, IConfiguration config)
        {
            _cache = cache;
            _config = config;

            if (_client == null)
            {
                ConfigureClient();
            }
        }

        public async Task<string> GetAccessToken()
        {
            string clientId = "ClientCredentials";
            string secret = "538D080E-BDF6-4178-8F07-29AFBCFD755F";
            string scope = "ApiTemplateApi";

            AccessTokenItem accessToken = GetFromCache(clientId);

            if (accessToken != null && accessToken.Expiry > DateTime.UtcNow)
            {
                return accessToken.AccessToken;
            }

            // Token not cached, or token is expired. Request new token from auth server
            AccessTokenItem newAccessToken = await RequestNewToken(clientId, scope, secret);
            AddToCache(clientId, newAccessToken);

            return newAccessToken.AccessToken;
        }

        private AccessTokenItem GetFromCache(string key)
        {
            var item = _cache.GetString(key);
            if (item != null)
            {
                return JsonSerializer.Deserialize<AccessTokenItem>(item);
            }

            return null;
        }

        private void AddToCache(string key, AccessTokenItem accessTokenItem)
        {
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(cacheExpirationInDays));

            lock (_lock)
            {
                _cache.SetString(key, JsonSerializer.Serialize(accessTokenItem), options);
            }
        }

        private async Task<AccessTokenItem> RequestNewToken(string clientId, string scope, string secret)
        {
            try
            {
                var discovery = await HttpClientDiscoveryExtensions.GetDiscoveryDocumentAsync(
                    _client, "http://localhost:5010");

                if (discovery.IsError)
                {
                    throw new InvalidOperationException($"Error: {discovery.Error}");
                }

                var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Scope = scope,
                    ClientSecret = secret,
                    Address = discovery.TokenEndpoint,
                    ClientId = clientId
                });

                if (tokenResponse.IsError)
                {
                    throw new InvalidOperationException($"Error: {tokenResponse.Error}");
                }

                return new AccessTokenItem
                {
                    Expiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    AccessToken = tokenResponse.AccessToken
                };
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Exception {e}");
            }
        }

        #region UTILS

        private void ConfigureClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(_config.GetValue<string>("IdentityServer:BaseAddress"))
            };

            // Add http headers here
        }

        #endregion UTILS
    }
}