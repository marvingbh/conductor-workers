using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Workers.Dtos;
using File = System.IO.File;

namespace APIClient
{
    public class FlorenceApi
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _baseAuthUrl;
        private string _bearerToken;
        private DateTime _tokenExpiration;

        public FlorenceApi(string baseUrl, string baseAuthUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _baseAuthUrl = baseAuthUrl;
        }

        public async Task AuthenticateAsync(string clientId, string clientSecret, string audience)
        {
            var tokenEndpoint = $"{_baseAuthUrl}/oauth/token";
            var requestBody = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials",
                audience = audience
            };
            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), null, "application/json");
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                _bearerToken = token.AccessToken;
                _tokenExpiration = DateTime.Now.AddSeconds(token.ExpiresIn);
            }
            else
            {
                throw new Exception($"Error authenticating: {response.StatusCode}");
            }
        }

        public async Task<string> GetAsync(string endpoint, string queryString)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}?{queryString}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        public async Task<string> GetFileAsync(string endpoint, string queryString, string folderPath)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}?{queryString}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var contentDisposition = response.Content.Headers.FirstOrDefault(h => h.Key == "Content-Disposition");
                if (contentDisposition.Value != null)
                {
                    var filename = contentDisposition.Value.First().Replace("attachment; filename=", "").Replace("\"", "");
                    var filePath = Path.Combine(folderPath, filename);

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = File.Create(filePath))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(fileStream);
                        }
                    }

                    return filePath;
                }

                throw new Exception("Content-Disposition header is missing");
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        public async Task<(byte[], string)> GetFileAsync(string endpoint)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsByteArrayAsync();
                var fileName = GetFileNameFromContentDispositionHeader(response.Content.Headers.ContentDisposition);
                return (responseBody, fileName);
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        private static string GetFileNameFromContentDispositionHeader(ContentDispositionHeaderValue contentDisposition)
        {
            var fileName = contentDisposition?.FileName?.Trim('"');
            return fileName;
        }

        public async Task<string> PostFileAsync(string endpoint, string filePath, string teamId, string binderId, string folderId)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            var formContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = Path.GetFileName(filePath)
            };
            formContent.Add(fileContent);
            formContent.Add(new StringContent(teamId), "teamId");
            formContent.Add(new StringContent(binderId), "binderId");
            formContent.Add(new StringContent(folderId), "folderId");

            request.Content = formContent;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        public async Task<string> PostBinaryAsync(string endpoint, byte[] fileContent, string fileName, string teamId, string binderId, string folderId)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            var boundary = Guid.NewGuid().ToString();

            var formContent = new MultipartFormDataContent(boundary);
            var fileContentBytes = new ByteArrayContent(fileContent);
            fileContentBytes.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = fileName
            };
            formContent.Add(fileContentBytes);
            formContent.Add(new StringContent(teamId), "teamId");
            formContent.Add(new StringContent(binderId), "binderId");
            formContent.Add(new StringContent(folderId), "folderId");

            request.Content = formContent;

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        public async Task<string> PostAsync(string endpoint, object requestBody)
        {
            if (IsTokenExpired())
            {
                await RefreshTokenAsync();
            }

            var url = $"{_baseUrl}/{endpoint}";
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }

            throw new Exception($"Error calling API: {response.StatusCode}");
        }

        private async Task RefreshTokenAsync()
        {
            var tokenEndpoint = $"{_baseUrl}/auth/token";
            var requestBody = new
            {
                grant_type = "refresh_token",
                refresh_token = _bearerToken
            };
            var requestContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8,
                "application/json");
            var response = await _httpClient.PostAsync(tokenEndpoint, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var token = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                _bearerToken = token.AccessToken;
                _tokenExpiration = DateTime.Now.AddSeconds(token.ExpiresIn);
            }
            else
            {
                throw new Exception($"Error refreshing token: {response.StatusCode}");
            }
        }

        private bool IsTokenExpired()
        {
            return _bearerToken == null || DateTime.Now > _tokenExpiration;
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")] public string AccessToken { get; set; }
            [JsonProperty("token_type")] public string TokenType { get; set; }
            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
            [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
            [JsonProperty("scope")] public string Scope { get; set; }
        }
    }
}
