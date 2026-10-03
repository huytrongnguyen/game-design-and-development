using System.Net.WebSockets;
using System.Text;

namespace M17.Net;

/// <summary>Reassembles one complete text message from WebSocket frames, with a hard size cap.</summary>
public static class WebSocketText
{
    public static async Task<ReadResult> ReadAsync(WebSocket socket, int maxBytes, CancellationToken ct)
    {
        var buffer = new byte[1024];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return new ReadResult(ReadStatus.Closed);
            }

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                return new ReadResult(ReadStatus.Binary);
            }

            message.Write(buffer, 0, result.Count);
            if (message.Length > maxBytes)
            {
                return new ReadResult(ReadStatus.TooBig);
            }

            if (result.EndOfMessage)
            {
                return new ReadResult(ReadStatus.Text, Encoding.UTF8.GetString(message.ToArray()));
            }
        }
    }

    public static Task SendAsync(WebSocket socket, string text, CancellationToken ct) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, ct);
}
