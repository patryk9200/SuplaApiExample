using Newtonsoft.Json;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace Supla
{
    internal static class SuplaHttpClient
    {
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, string content)
        {
            Console.WriteLine($"Link: {requestUri} content: {content}");
            return PatchAsync(client, new Uri(requestUri), new StringContent(content, Encoding.UTF8, "application/json"));
        }

        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent iContent)
            => PatchAsync(client, new Uri(requestUri), iContent);

        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            var response = new HttpResponseMessage();

            response = await client.SendAsync(request);
            return response;
        }
    }

    public class SuplaApi
    {
        public async Task Async()
        {
            var result = await TokenRequestAsync();

            if (result)
                Console.WriteLine("SUPLA USER: " + JsonConvert.SerializeObject((await GetUserInfoAsync()).Data));
        }

        public async Task<bool> TokenRequestAsync()
        {
            try
            {
                await SuplaOAuth.GetAccessTokenAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> CheckAuthorizationAsync()
        {
            using (var client = new HttpClient())
            {
                // create the URL string.
                var url = $"{SuplaOAuth.Data.BaseUrl}/api/users/current";
                try
                {
                    var method = Method.Get;
                    Console.WriteLine($"Request {url} method: {method}");
                    client.BaseAddress = new Uri(SuplaOAuth.Data.BaseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SuplaOAuth.Data.AccessToken}");

                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"PATH<T> for URL {url} have thrown error {e}");
                    return false;
                }
            }
        }

        public async Task<Result<T>> PatchAsync<T>(string path, Dictionary<string, object> data = null, Method method = Method.Get)
        {
            //await SuplaOAuth.GetAccessTokenAsync();

            using (var client = new HttpClient())
            {
                // create the URL string.
                var url = $"{SuplaOAuth.Data.BaseUrl}/api/v2.3.0{path}";
                try
                {
                    Console.WriteLine($"Request {url} method: {method}");
                    client.BaseAddress = new Uri(SuplaOAuth.Data.BaseUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add the Authorization header with the AccessToken.
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SuplaOAuth.Data.AccessToken}");

                    HttpResponseMessage response = null;

                    if (data != null)
                    {
                        switch (method)
                        {
                            case Method.Get:
                                url += "/?";
                                foreach (var element in data)
                                    url += $"{HttpUtility.UrlEncode(element.Key)}={HttpUtility.UrlEncode(element.Value.ToString())}";

                                // make the request
                                response = await client.GetAsync(url);
                                break;

                            case Method.Patch:
                                var dataToSend = JsonConvert.SerializeObject(data);
                                Console.WriteLine($"PATH Data {dataToSend}");
                                // make the request
                                response = await client.PatchAsync(url, dataToSend);
                                break;
                        }
                    }
                    else
                    {
                        switch (method)
                        {
                            case Method.Get:
                                response = await client.GetAsync(url);
                                break;
                            case Method.Patch:
                                response = await client.PatchAsync(url, "");
                                break;
                        }
                    }

                    // parse the response and return the data.
                    try
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"SUPLA server response code: {response.StatusCode}");

                        return new Result<T>
                        {
                            Data = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(response.Content.ReadAsStream()),
                            StatusCode = response.StatusCode
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);

                        if (response.IsSuccessStatusCode)
                            return new Result<T>();
                        else
                            throw;
                    }

                    if (response.IsSuccessStatusCode)
                        return new Result<T>();
                    else
                        throw new HttpRequestException($"HTTP response code: {response.StatusCode}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"PATH<T> for URL {url} have thrown error {e}");
                    throw;
                }
            }
        }

        public Task<Result<Any>> GetChannelsAsync()
            => PatchAsync<Any>("/channels?include=iodevice");

        public Task<Result<PowerMeasurement[]>> GetPowerMeasurementsAsync(int channel, int limit) 
            => PatchAsync<PowerMeasurement[]>($"/channels/{channel}/measurement-logs?limit={limit}");

        public Task<Result<PowerMeasurement[]>> GetPowerMeasurementsAsync(int channel, DateTimeOffset from, DateTimeOffset to, int points = 50)
            => PatchAsync<PowerMeasurement[]>($"/channels/{channel}/measurement-logs?sparse={points}&afterTimestamp={from.ToUnixTimeSeconds()}&beforeTimestamp={to.ToUnixTimeSeconds()}&order=DES");

        public Task<Result<Any>> GetServerInfoAsync()
            => PatchAsync<Any>("server-info");

        public Task<Result<Any>> GetUserInfoAsync()
            => PatchAsync<Any>("/users/current");

        public Task<Result<Any>> LocationsAsync()
            => PatchAsync<Any>("/locations");

        public Task<Result<Any>> AccessIDsAsync()
            => PatchAsync<Any>("/accessids");

        public Task<Result<Device[]>> IODevicesAsync()
            => PatchAsync<Device[]>("/iodevices");

        public Task<Result<Any>> IODeviceAsync(int deviceId)
            => PatchAsync<Any>($"/iodevices/{deviceId}");

        public Task<Result<Any>> ChannelAsync(int channel)
            => PatchAsync<Any>($"/channels/{channel}");

        public Task<Result<Any>> ChannelSetRGBWAsync(int channel, string color, int colorBrightness, int brightness)
        {
            var dic = new Dictionary<string, object>
            {
                { "color", color },
                { "color_brightness", colorBrightness },
                { "brightness", brightness }
            };
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelSetBrightnessAsync(int channel, int brightness)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("brightness", brightness);
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelTurnOnAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "turn-on");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelTurnOffAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "turn-off");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> GetChannelStateAsync(int channel)
        {
            return PatchAsync<Any>($"/channels/{channel}", null, Method.Get);
        }

        public Task<Result<Any>> ChannelOpenAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "open");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelCloseAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "close");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelOpenCloseAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "open-close");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelShutAsync(int channel, int percent = 100)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "shut");
            dic.Add("percent", percent);
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelRevealAsync(int channel, int percent = 100)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "reveal");
            dic.Add("percent", percent);
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public Task<Result<Any>> ChannelStopAsync(int channel)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("action", "stop");
            return PatchAsync<Any>($"/channels/{channel}", dic, Method.Patch);
        }

        public enum Method
        {
            Get = 0,
            Post = 1,
            Patch = 2,
            Put = 3,
            Del = 4
        }
    }

    public class Result<T>
    {
        public T Data { get; set; }

        public HttpStatusCode StatusCode { get; set; }

    }

    public class DevicesList
    {
        [JsonPropertyName("iodevices")]
        public Device[] Devices { get; set; }
    }

    public class Device
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 1;

        [JsonPropertyName("gUIDString")]
        public string DeviceGuid { get; set; } = string.Empty;

        [JsonPropertyName("locationId")]
        public int LocationId { get; set; }

        [JsonPropertyName("name")]
        public string DeviceName { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("manufacturer")]
        public Manufacturer Manufacturer { get; set; }

        [JsonPropertyName("regDate")]
        public string Registration { get; set; }

        [JsonPropertyName("lastConnected")]
        public string LastConnected { get; set; }

        [JsonPropertyName("guid")]
        public string SuplaDeviceGuid { get; set; } = string.Empty;

        [JsonPropertyName("softwareVersion")]
        public string SoftwareVersion { get; set; }

        [JsonPropertyName("channels")]
        public DeviceChannel[] Channels { get; set; }

        [JsonPropertyName("channelsIds")]
        public int[] ChannelsIDs { get; set; }
    }

    public class Manufacturer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("caption")]
        public string Caption { get; set; }
    }

    public class TimeAndSource
    {
        [JsonPropertyName("date")]
        public int Date { get; set; }

        [JsonPropertyName("ip_v4")]
        public string IPv4 { get; set; }

        [JsonPropertyName("ip_v6")]
        public string IPv6 { get; set; }
    }

    public class DeviceChannel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("chnnel_number")]
        public int ChannelNumber { get; set; }

        [JsonPropertyName("caption")]
        public dynamic Caption { get; set; }

        [JsonPropertyName("type")]
        public ChannelTypeInfo Type { get; set; }

        [JsonPropertyName("function")]
        public ChannelFunctionInfo Function { get; set; }
    }

    public class ChannelTypeInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        public ChannelType ChannelType
        {
            get
            {
                switch (Name)
                {
                    case "TYPE_RELAY":
                        return ChannelType.Relay;

                    default:
                        Console.WriteLine($"ChannelType: {Name} Id: {Id}");
                        return ChannelType.Unknown;
                }
            }
        }
    }

    public class ChannelFunctionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        public ChannelFunction ChannelFunction
        {
            get
            {
                switch (Name)
                {
                    case "FNC_LIGHTSWITCH":
                        return ChannelFunction.LightSwitch;

                    case "FNC_CONTROLLINGTHEROLLERSHUTTER":
                        return ChannelFunction.Rollershutter;

                    case "FNC_ELECTRICITYMETER":
                        return ChannelFunction.ElectricityMeter;

                    default:
                        Console.WriteLine($"ChannelType: {Name} Id: {Id}");
                        return ChannelFunction.Unknown;
                }
            }
        }
    }

    public class PowerMeasurement
    {
        [JsonPropertyName("date_timestamp")]
        public string DateTimestamp { get; set; }

        [JsonPropertyName("phase1_fae")]
        public string Phase1Fae { get; set; }

        [JsonPropertyName("phase1_rae")]
        public string Phase1Rae { get; set; }

        [JsonPropertyName("phase1_fre")]
        public string Phase1Fre { get; set; }

        [JsonPropertyName("phase1_rre")]
        public string Phase1Rre { get; set; }

        [JsonPropertyName("phase2_fae")]
        public string Phase2Fae { get; set; }

        [JsonPropertyName("phase2_rae")]
        public string Phase2Rae { get; set; }

        [JsonPropertyName("phase2_fre")]
        public string Phase2Fre { get; set; }

        [JsonPropertyName("phase2_rre")]
        public string Phase2Rre { get; set; }

        [JsonPropertyName("phase3_fae")]
        public string Phase3Fae { get; set; }

        [JsonPropertyName("phase3_rae")]
        public string Phase3Rae { get; set; }

        [JsonPropertyName("phase3_fre")]
        public string Phase3Fre { get; set; }

        [JsonPropertyName("phase3_rre")]
        public string Phase3Rre { get; set; }

        [JsonPropertyName("fae_balanced")]
        public string FaeBalanced { get; set; }

        [JsonPropertyName("rae_balanced")]
        public string RaeBalanced { get; set; }
    }

    public enum ChannelType
    {
        Unknown = 0,
        Relay = 1,

        ElectricityMeter = 310
    }

    public enum ChannelFunction
    {
        Unknown = 0,
        LightSwitch = 1,
        Rollershutter = 2,
        ElectricityMeter = 310
    }

    public class Any
    { }
}
