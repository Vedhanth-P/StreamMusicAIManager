using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StreamMusicAIManager.Core
{
    public class ObsWebSocketManager
    {
        private ClientWebSocket? _client;
        private readonly Uri _serverUri;

        public bool IsConnected => _client != null && _client.State == WebSocketState.Open;

        public ObsWebSocketManager(string url = "ws://localhost:4455")
        {
            _serverUri = new Uri(url);
        }

        public async Task ConnectAsync(string password = "")
        {
            try
            {
                _client = new ClientWebSocket();
                using var cts = new CancellationTokenSource(3000);
                await _client.ConnectAsync(_serverUri, cts.Token);
                var hello = await ReceiveJsonAsync(cts.Token);
                var identify = new Dictionary<string, object?> { ["op"] = 1, ["d"] = new { rpcVersion = 3 } };
                if (!string.IsNullOrEmpty(password) && hello.TryGetProperty("d", out var helloData) &&
                    helloData.TryGetProperty("authentication", out var auth))
                {
                    var secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + auth.GetProperty("salt").GetString())));
                    var authResponse = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + auth.GetProperty("challenge").GetString())));
                    identify["d"] = new { rpcVersion = 3, authentication = authResponse };
                }
                await SendJsonAsync(identify, cts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OBS Connection Failed: {ex.Message}");
            }
        }

        public async Task SwitchPlaylistModeAsync(string modeName, string playlistFilePath)
        {
            if (!IsConnected) return;

            await SendJsonAsync(new
            {
                op = 6,
                d = new
                {
                    requestType = "SetInputSettings",
                    requestId = "StreamMusicAI",
                    requestData = new
                    {
                        inputName = "StreamBGM",
                        inputSettings = new { local_file = playlistFilePath }
                    }
                }
            }, CancellationToken.None);
        }

        private async Task SendJsonAsync(object payload, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            await _client!.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task<JsonElement> ReceiveJsonAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            using var stream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _client!.ReceiveAsync(buffer, cancellationToken);
                stream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            using var document = JsonDocument.Parse(stream.ToArray());
            return document.RootElement.Clone();
        }
    }
}