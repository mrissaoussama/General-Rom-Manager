using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AltoMultiThreadDownloadManager.NativeMessages
{
    /// <summary>
    /// Provides methods to get native message from external application
    /// </summary>
    public static class Receiver
    {
        /// <summary>
        /// Extracts text message from native message
        /// </summary>
        /// <param name="data">The native message data</param>
        /// <returns></returns>
        public static string? ProcessMessage(JsonObject? data)
        {
            if (data == null)
                return null;
            return data["text"]?.GetValue<string>();
        }
        /// <summary>
        /// Reads the native message using stdio
        /// </summary>
        /// <returns></returns>
        public static JsonObject? Read()
        {
            var stdin = Console.OpenStandardInput();

            var lengthBytes = new byte[4];
            stdin.Read(lengthBytes, 0, 4);
            var length = BitConverter.ToInt32(lengthBytes, 0);

            var buffer = new char[length];
            using (var reader = new StreamReader(stdin))
            {
                reader.Read(buffer, 0, buffer.Length);
            }

            return JsonNode.Parse(new string(buffer))?.AsObject();
        }
        /// <summary>
        /// Gets the native message as parsed to DownloadMessage
        /// </summary>
        /// <returns></returns>
        public static DownloadMessage? ReadDownloadMessage()
        {
            var jobj = Read();
            if (jobj == null)
                return null;
            var prc = ProcessMessage(jobj);
            if (prc == null)
                return null;
            return JsonSerializer.Deserialize<DownloadMessage>(prc);
        }
    }
    
}
