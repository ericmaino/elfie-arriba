using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arriba.Server.Models
{

    public partial class Token
    {
        [JsonProperty("aud")]
        public Guid Aud { get; set; }

        [JsonProperty("iss")]
        public Uri Iss { get; set; }

        [JsonProperty("iat")]
        public long Iat { get; set; }

        [JsonProperty("nbf")]
        public long Nbf { get; set; }

        [JsonProperty("exp")]
        public long Exp { get; set; }

        [JsonProperty("aio")]
        public string Aio { get; set; }

        [JsonProperty("azp")]
        public Guid Azp { get; set; }

        [JsonProperty("azpacr")]
        public string Azpacr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("oid")]
        public Guid Oid { get; set; }

        [JsonProperty("preferred_username")]
        public string PreferredUsername { get; set; }

        [JsonProperty("rh")]
        public string Rh { get; set; }

        [JsonProperty("roles")]
        public string[] Roles { get; set; }

        [JsonProperty("scp")]
        public string Scp { get; set; }

        [JsonProperty("sub")]
        public string Sub { get; set; }

        [JsonProperty("tid")]
        public Guid Tid { get; set; }

        [JsonProperty("uti")]
        public string Uti { get; set; }

        [JsonProperty("ver")]
        public string Ver { get; set; }
    }

    public partial class Token
    {
        public static Token FromJson(string json) => JsonConvert.DeserializeObject<Token>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Token self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }


}

