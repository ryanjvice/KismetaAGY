using System;
using System.IO;
using UnityEngine;

namespace Kismeta.UI.Diagnostics
{
    internal static class DebugSessionLog
    {
        const string SessionId = "a10709";
        static readonly string LogPath = Path.Combine(Application.dataPath, "..", "debug-a10709.log");

        public static void Write(string hypothesisId, string location, string message, string dataJson)
        {
            try
            {
                var line = "{\"sessionId\":\"" + SessionId + "\",\"hypothesisId\":\"" + hypothesisId +
                           "\",\"location\":\"" + location + "\",\"message\":\"" + message +
                           "\",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                           ",\"data\":" + dataJson + "}\n";
                File.AppendAllText(LogPath, line);
            }
            catch { /* ignore */ }
        }
    }
}
