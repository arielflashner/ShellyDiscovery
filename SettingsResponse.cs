namespace ShellyDiscovery
{
    public class SettingsResponse
    {
        public string name { get; set; }
        public Relay[] relays { get; set; }
    }

    public class Relay
    {
        public string name { get; set; }
    }
}