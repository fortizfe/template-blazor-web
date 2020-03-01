using IdentityModel.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Template.Blazor.Web.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Template.Blazor.Web.Services.HttpServices
{
    public interface IHttpService
    {
        Task<ICollection<WorkerModel>> GetWorkers();
    }

    public class HttpService : IHttpService
    {
        private HttpClient _client;
        private readonly IConfiguration _config;
        private readonly IAuthService _authService;

        public HttpService(IConfiguration config, IAuthService authService)
        {
            _config = config;
            _authService = authService;

            if (_client == null)
            {
                ConfigureClient();
            }
        }

        public async Task<ICollection<WorkerModel>> GetWorkers()
        {
            var endpoint = _config.GetValue<string>("ApiConfig:Workers:Get");

            return await GetAsync<ICollection<WorkerModel>>(endpoint);
        }

        #region UTILS

        private async Task<TResult> GetAsync<TResult>(string endpoint)
        {
            var accessToken = await _authService.GetAccessToken();
            _client.SetBearerToken(accessToken);

            endpoint = $"{_config.GetValue<string>("ApiConfig:Version")}{endpoint}";
            var response = await _client.GetJsonAsync<ApiResponseModel<TResult>>(endpoint);

            if (!response.Succeeded)
                throw new HttpRequestException(string.Join("\r\n", response.Errors));

            return response.Result;
        }

        private void ConfigureClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(_config.GetValue<string>("ApiConfig:BaseAddress"))
            };

            // Add http headers here
        }

        #endregion UTILS
    }
}