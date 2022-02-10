
using Supla;

Console.WriteLine("Welcome!\nEntry data below.");
Console.WriteLine("Server URI:");
var uri = Console.ReadLine();

Console.WriteLine("Bearer:");
var bearer = Console.ReadLine();

SuplaOAuth.Data = new OAuthData
{
    BaseUrl = uri,
    AccessToken = bearer
};

Console.WriteLine($"URI: {uri} \nBearer: {bearer}");

var api = new SuplaApi();
Console.WriteLine($"Getting devices...");
var devices = (await api.IODevicesAsync()).Data;

Console.WriteLine($"Got devices: {devices.Count()}");
var device = devices.First(a => a.DeviceName.Contains("MEW"));

Console.WriteLine($"Getting measurements for device: {device.DeviceName} id: {device.Id} location: {device.LocationId}");
var measurements = await api.GetPowerMeasurementsAsync(device.ChannelsIDs.First(), DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now, 5);

Console.WriteLine($"Got {measurements.Data.Count()} measurements.");

var tmp = new List<Measurement>();

var i = 0;
foreach (var m in measurements.Data)
{
    i++;
    tmp.Add(new Measurement
    {
        Date = long.Parse(m.DateTimestamp).UnixTimeToDateTime(),
        Device = device,
        Number = i,
        Phase1FAE = m.Phase1Fae.ToDouble() / 100_000,
        Phase1RAE = m.Phase1Rae.ToDouble() / 100_000,
        Phase1RRE = m.Phase1Rre.ToDouble() / 100_000,
        Phase1FRE = m.Phase1Fre.ToDouble() / 100_000,

        Phase2FAE = m.Phase2Fae.ToDouble() / 100_000,
        Phase2RAE = m.Phase2Rae.ToDouble() / 100_000,
        Phase2RRE = m.Phase2Rre.ToDouble() / 100_000,
        Phase2FRE = m.Phase2Fre.ToDouble() / 100_000,

        Phase3FAE = m.Phase3Fae.ToDouble() / 100_000,
        Phase3RAE = m.Phase3Rae.ToDouble() / 100_000,
        Phase3RRE = m.Phase3Rre.ToDouble() / 100_000,
        Phase3FRE = m.Phase3Fre.ToDouble() / 100_000,

        SumFAE = (m.Phase1Fae.ToDouble() + m.Phase2Fae.ToDouble() + m.Phase3Fae.ToDouble()) / 100_000,
        SumFRE = (m.Phase1Fre.ToDouble() + m.Phase2Fre.ToDouble() + m.Phase3Fre.ToDouble()) / 100_000,
        SumRAE = (m.Phase1Rae.ToDouble() + m.Phase2Rae.ToDouble() + m.Phase3Rae.ToDouble()) / 100_000,
        SumRRE = (m.Phase1Rre.ToDouble() + m.Phase2Rre.ToDouble() + m.Phase3Rre.ToDouble()) / 100_000,
    });
}
