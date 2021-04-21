using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShellyDiscovery
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter single IP address or range to scan (for example, 192.168.1.10-20):");
            var startIp = Console.ReadLine();
            var endIp = 255;
            if (startIp.Contains("-")) {
                var ips = startIp.Split("-");
                startIp = ips[0];
                if (ips.Length != 2) {
                    Console.WriteLine("Invalid range specified - only one dash please ...");
                    return;
                }

                if (!int.TryParse(ips[1], out endIp)) {
                    Console.WriteLine("Unable to parse upper range specified '" + ips[1] + "'");
                    return;
                }

                if (endIp < 0 || endIp > 255) {
                    Console.WriteLine("Upper IP range should be between 0-255");
                    return;
                }
            }
            
            Console.WriteLine("Would you like to show devices grouped according to something? for example device.type");
            var groupBy = Console.ReadLine();

            if (!IPAddress.TryParse(startIp, out var ip))
            {
                Console.WriteLine("Unable to parse ip '" + startIp + "'");
                return;
            }

            var ipBytes = ip.GetAddressBytes();
            var ipPrefix = ipBytes[0] + "." + ipBytes[1] + "." + ipBytes[2] + ".";
            if (ipBytes[3] > endIp) {
                Console.WriteLine("Lower IP range is higher than upper range");
                return;
            }

            Console.WriteLine("Enter username that should be used for restricted devices (can be left empty in case you have no restricted devices):");
            var username = Console.ReadLine();
            var password = "";
            if (!String.IsNullOrWhiteSpace(username)) {
                Console.WriteLine("Enter password that should be used for restricted devices:");
                password = Console.ReadLine();
            }

            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(username))
                client.DefaultRequestHeaders.Add($"Authorization", $"Basic {Base64Encode($"{username}:{password}")}");

            client.Timeout = TimeSpan.FromMilliseconds(600);
            var data = new Dictionary<string, dynamic>();
            for (int i = ipBytes[3]; i < endIp; i++)
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

                    var settings = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    data.Add(ipString, settings);

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

            var groupedData = data.GroupBy(i => i.Value.SelectToken(groupBy));
            foreach (var item in groupedData)
            {
                var groupValue = item.Key;
                Console.WriteLine(groupValue + ":");
                foreach (var device in item.ToArray())
                {
                    var deviceIp = device.Key;
                    var deviceName = device.Value.name;
                    Console.WriteLine("\t" + deviceIp + "\t" + deviceName);
                }
                
            }
        }

        private static string GetRelayNames(dynamic settings) {
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
