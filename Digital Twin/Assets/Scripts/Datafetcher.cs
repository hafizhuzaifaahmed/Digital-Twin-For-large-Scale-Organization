using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string url = "https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/floors&CompanyCode=4QiqsHB9u5";
        string headerKey = "X-Oc-Restadmin-Id";
        string headerValue = "B4n0VFhrUJ5SEegAXC0YcZ3gI3YF1YW";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Add the header to the request
                client.DefaultRequestHeaders.Add(headerKey, headerValue);

                // Send GET request
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response as a string
                    string responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response Data:");
                    Console.WriteLine(responseData);
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred:");
            Console.WriteLine(ex.Message);
        }
    }
}
