using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ResoniteMediaReporter
{
    public class Config
    {
        [JsonPropertyName("port")]
        public int Port { get; set; }
        [JsonPropertyName("ignorePlayers")]
        public string[] IgnorePlayers { get; set; }
        public int LyricsPort { get; set; }
        public List<string> DisableLyricsFor { get; set; } = new();


    }
}
