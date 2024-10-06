using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Set the port and the base URL
        int port = 8080;
        string baseUrl = $"http://localhost:{port}/";

        // Create a HTTP listener
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(baseUrl);

        

        try
        {
            listener.Start();
            Console.WriteLine($"Listening on {baseUrl}. Press Ctrl+C to stop.");

            while (true)
            {
                // Wait for an incoming request
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest req = context.Request;
                Console.WriteLine($"Received request for {req.Url}");

                //Console.WriteLine(JsonSerializer.Serialize(context));
            }
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Stop the listener when done
            listener.Close();
        }
    }
}