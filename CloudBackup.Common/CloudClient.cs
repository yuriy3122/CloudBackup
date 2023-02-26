using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Jose;

namespace CloudBackup.Common
{
    public class CloudClient : ICloudClient
    {
        private readonly StringContent _content;
        private readonly string _cloudProviderUrl = "nv";

        public CloudClient(CloudCredentials credentials)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var payload = new Dictionary<string, object>()
            {
                { "aud", $"https://iam.api.cloud.{_cloudProviderUrl}.net/iam/v1/tokens" },
                { "iss", credentials?.ServiceAccountId ?? string.Empty },
                { "iat", now },
                { "exp", now + 3600 }
            };

            var headers = new Dictionary<string, object>() { { "kid", credentials?.KeyId ?? string.Empty } };

            string encodedToken = string.Empty;

            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(credentials?.PrivateKey?.ToCharArray());
                encodedToken = JWT.Encode(payload, rsa, JwsAlgorithm.PS256, headers);
            }

            var input = new { jwt = encodedToken };
            var json = JsonConvert.SerializeObject(input);
            _content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        public async Task<CloudList?> GetCloudList()
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var cloudListResponse = await httpClient.GetAsync(new Uri($"https://resource-manager.api.cloud.{_cloudProviderUrl}.net/resource-manager/v1/clouds"));
            var cloudListResult = await cloudListResponse.Content.ReadAsStringAsync();
            var clouds = JsonConvert.DeserializeObject<CloudList>(cloudListResult);

            return clouds;
        }

        public async Task<CloudFolderList?> GetCloudFolderList(string cloudId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var folderListResponse = await httpClient.GetAsync(new Uri($"https://resource-manager.api.cloud.{_cloudProviderUrl}.net/resource-manager/v1/folders?cloudId={cloudId}"));
            var folderListResult = await folderListResponse.Content.ReadAsStringAsync();
            var folders = JsonConvert.DeserializeObject<CloudFolderList>(folderListResult);

            return folders;
        }

        protected async Task AddAuthorizationHeaders(HttpClient httpClient)
        {
            var response = await httpClient.PostAsync(new Uri($"https://iam.api.cloud.{_cloudProviderUrl}.net/iam/v1/tokens"), _content);
            var result = await response.Content.ReadAsStringAsync();
            var accessToken = JsonConvert.DeserializeObject<IamTokenResult>(result)?.IamToken;

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }
    }
}
