using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ShellyDiscovery
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter IP address to start scan from (for example, 192.168.1.10):");
            var startIp = Console.ReadLine();

            if (!IPAddress.TryParse(startIp, out var ip))
            {
                Console.WriteLine("Unable to parse ip '" + startIp + "'");
                return;
            }

            Console.WriteLine("Enter username that should be used for restricted devices (can be left empty in case you have no restricted devices):");
            var username = Console.ReadLine();
            var password = "";
            if (!String.IsNullOrWhiteSpace(username)) {
                Console.WriteLine("Enter password that should be used for restricted devices:");
                password = Console.ReadLine();
            }

            var ipBytes = ip.GetAddressBytes();
            var ipPrefix = ipBytes[0] + "." + ipBytes[1] + "." + ipBytes[2] + ".";

            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(username))
                client.DefaultRequestHeaders.Add($"Authorization", $"Basic {Base64Encode($"{username}:{password}")}");

            client.Timeout = TimeSpan.FromMilliseconds(600);
            for (int i = ipBytes[3]; i < 255; i++)
            {
                var ipString = ipPrefix + i.ToString();
                try
                {
                    var response = client.GetAsync("http://" + ipString + "/settings").Result;
                    if (response.StatusCode == HttpStatusCode.Unauthorized) {
                        Console.WriteLine(ipString + "\t" + "Unauthorized");
                        continue;
                    }
                    else if (response.StatusCode != HttpStatusCode.OK) {
                        Console.WriteLine(ipString + "\t" + "Response code: " + response.StatusCode);
                        continue;
                    }

                    var settings = JsonSerializer.Deserialize<SettingsResponse>(response.Content.ReadAsStringAsync().Result);

                    Console.WriteLine(ipString + "\t" + settings.name + GetRelayNames(settings));
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("A task was canceled")) {
                        Console.WriteLine(ipString + "\t" + "Timeout while trying to connect ...");
                        continue;
                    }

                    Console.WriteLine(ipString + "\t" + "Error: " + e.Message);
                }
            }
        }

        private static string GetRelayNames(SettingsResponse settings) {
            var relayNames = "";
            foreach (var relay in settings.relays)
            {
                relayNames += "\t" + relay.name;
            }

            return relayNames;
        }

        public static string Base64Encode(string textToEncode)
        {
            byte[] textAsBytes = Encoding.UTF8.GetBytes(textToEncode);
            return Convert.ToBase64String(textAsBytes);
        }
    }
}
