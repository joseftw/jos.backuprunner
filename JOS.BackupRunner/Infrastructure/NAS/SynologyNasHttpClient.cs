using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JOS.BackupRunner.Infrastructure.NAS
{
    public class SynologyNasHttpClient
    {
        private static readonly SemaphoreSlim _loginSemaphoreSlim;
        private static string _sid;
        private readonly HttpClient _httpClient;
        private readonly SynologyNasOptions _synologyNasOptions;
        private static readonly JsonSerializerOptions _jsonSerializerOptions;

        public SynologyNasHttpClient(HttpClient httpClient, SynologyNasOptions synologyNasOptions)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _synologyNasOptions = synologyNasOptions ?? throw new ArgumentNullException(nameof(synologyNasOptions));
        }

        static SynologyNasHttpClient()
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _loginSemaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public async Task UploadFile(Stream file, string targetPath, string filename)
        {
            var sid = await GetSid();
            var formData = new MultipartFormDataContent("AaB03x")
            {
                {new StringContent("SYNO.FileStation.Upload"), "api"},
                {new StringContent("2"), "version"},
                {new StringContent("upload"), "method"},
                {new StringContent(targetPath), "path"},
                {new StringContent("true"), "create_parents"},
                {new StringContent("true"), "overwrite"},
                {new StreamContent(file), "file", filename},
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"/webapi/entry.cgi?_sid={sid}")
            {
                Content = formData
            };
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> GetSid()
        {
            if (!string.IsNullOrWhiteSpace(_sid))
            {
                return _sid;
            }

            await _loginSemaphoreSlim.WaitAsync();

            if (!string.IsNullOrWhiteSpace(_sid))
            {
                return _sid;
            }

            _ = await Login();

            _loginSemaphoreSlim.Release();
            return _sid;
        }

        private async Task<string> Login()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/webapi/auth.cgi?api=SYNO.API.Auth&version=3&method=login&account={_synologyNasOptions.Username}&passwd={_synologyNasOptions.Password}&session=FileStation&format=sid");
            
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<SynologyApiResponse<ApiAuthLoginResponse>>(responseStream, _jsonSerializerOptions);
            if (!result.Success)
            {
                throw new Exception("Success was false when trying to login");
            }
            _sid = result.Data.Sid;
            return _sid;
        }
    }
}
