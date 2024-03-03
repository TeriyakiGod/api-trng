using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace WebSocketsSample.Controllers;

public class WebSocketController : ControllerBase
{
    [Route("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        var stopwatch = new Stopwatch();
        long totalBits = 0;
        var lockObject = new object();

        var writer = new BinaryWriter(new FileStream("data.bin", FileMode.Append));

        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (sender, e) =>
        {
            lock (lockObject)
            {
                double kilobytesPerSecond = totalBits / (8.0 * 1024) / stopwatch.Elapsed.TotalSeconds;
                Console.Clear();
                Console.WriteLine($"Kilobytes per second: {kilobytesPerSecond}");

                totalBits = 0;
                stopwatch.Restart();
            }
        };
        timer.Start();

        while (!result.CloseStatus.HasValue)
        {
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                uint message = BitConverter.ToUInt32(buffer, 0);

                lock (lockObject)
                {
                    totalBits += 32;
                }

                // Write the received word to the binary file
                writer.Write(message);
            }
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        timer.Stop();
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
}