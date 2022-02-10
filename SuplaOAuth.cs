using Newtonsoft.Json;

using RestSharp;

using System.Net.Http.Headers;

namespace Supla
{

    public class SuplaOAuth
    {
        public static OAuthData Data { get; set; } = new OAuthData();

        public static async Task OldGetAccessToken()
        {
            if (Data.ExpirationTime > DateTime.Now)
                return;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Data.BaseUrl);

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Build up the data to POST.
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>
                {
                    ///client_id
                    new KeyValuePair<string, string>("client_id", Data.ClientId),
                    new KeyValuePair<string, string>("client_secret", Data.ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "password")
                };
                FormUrlEncodedContent content = new FormUrlEncodedContent(postData);

                // Post to the Server and parse the response.
                HttpResponseMessage response = await client.PostAsync("/oauth/v2/token", content);
                string jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SUPLA server response: {jsonString}");
                object responseData = JsonConvert.DeserializeObject(jsonString);

                // return the Access Token.
                Data.ExpirationTime = DateTime.Now.AddSeconds(30);
                Data.AccessToken = ((dynamic)responseData).access_token;
                Data.RefreshToken = ((dynamic)responseData).refresh_token;
            }
        }

        public static Task GetAccessTokenAsync()
        {
            return Task.Run(() =>
            {
                if (Data.ExpirationTime > DateTime.Now)
                    return;

                var client = new RestClient($"{Data.BaseUrl}/oauth/v2/auth");
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", $"auth?client_id={Data.ClientId}&scope=account_r&state=example-state&response_type=code&redirect_uri=http%3A%2F%2Fepp.exala.pl", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                    Console.WriteLine($"Is login successfull: {response.IsSuccessful}");
                else
                    Console.WriteLine($"Failed to login in supla. Info: {response.StatusCode} {response.Content}");
            });
        }
    }

    public class OAuthData
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; }
        public DateTime ExpirationTime { get; set; } = DateTime.Now.AddMinutes(-120);
    }
}
