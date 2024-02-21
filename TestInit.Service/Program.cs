using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TestInit.Service // Updated namespace
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string remoteUrl = Environment.GetEnvironmentVariable("REMOTE_URL");
            string sleepTimeString = Environment.GetEnvironmentVariable("SLEEP_TIME");
            int sleepTime;

            // Check if environment variables are set
            if (remoteUrl != null && sleepTimeString != null)
            {
                // Parse sleepTime from environment variable, default to 10 seconds if not specified or invalid
                if (!int.TryParse(sleepTimeString, out sleepTime) || sleepTime <= 0)
                {
                    sleepTime = 10000; // Default to 10 seconds
                }
            }
            else
            {
                // Handle case where environment variables are not set
                Console.WriteLine("REMOTE_URL or SLEEP_TIME environment variable is not set.");
                return;
            }

            while (true)
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(remoteUrl);
                        string responseBody = await response.Content.ReadAsStringAsync();
                        int responseCode = (int)response.StatusCode;

                        Console.WriteLine($"Response code: {responseCode}");

                        if (responseCode >= 200 && responseCode <= 399)
                        {
                            dynamic jsonData = JObject.Parse(responseBody);
                            string initiateStatus = jsonData.InitiateStatus;

                            switch (initiateStatus)
                            {
                                case "Fail":
                                    Console.WriteLine("Init process has failed. Exiting with error.");
                                    Environment.Exit(1);
                                    break;
                                case "InProgress":
                                    Console.WriteLine("Init process is in progress. Continuing to check.");
                                    break;
                                case "NotTriggered":
                                    Console.WriteLine("No change has been applied. Exiting with code 0.");
                                    Environment.Exit(0);
                                    break;
                                case "Success":
                                    Console.WriteLine("Init container completed successfully. Exiting with code 0.");
                                    Environment.Exit(0);
                                    break;
                                default:
                                    Console.WriteLine($"Unknown InitiateStatus: {initiateStatus}");
                                    Environment.Exit(1);
                                    break;
                            }
                        }
                        else if (responseCode >= 400 && responseCode <= 599)
                        {
                            Console.WriteLine($"Server {remoteUrl} is not available (Error {responseCode}). Continuing to check.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                // Sleep for the specified duration before checking again
                await Task.Delay(sleepTime);
            }
        }
    }
}
