using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HttpAdapter.Models
{
    public class JwtModel
    {
        public JwtModel(Claim[] claims)
        {
            var k = claims.Select(x => new { x.Type, x.Value });
            Method = GetValue(claims, nameof(Method));
            Url = GetValue(claims, nameof(Url));
            Body = GetValue(claims, nameof(Body));
            MediaType = GetValue(claims, nameof(MediaType));
            Header = GetDictionary(claims, nameof(Header));
        }

        private static Dictionary<string, string> GetDictionary(Claim[] claims, string type)
        {
            var dictionary = new Dictionary<string, string>();

            var json = GetValue(claims, type);

            if (string.IsNullOrEmpty(json))
            {
                return dictionary;
            }

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            if (!JsonDocument.TryParseValue(ref reader, out var doc))
            {
                return dictionary;
            }

            var list = doc.RootElement.EnumerateObject().ToDictionary(y => y.Name, y => y.Value.GetString());

            return list;
        }

        private static string GetValue(Claim[] claims, string type)
        {
            return claims.FirstOrDefault(x => string.Equals(x.Type, type, StringComparison.CurrentCultureIgnoreCase))?.Value;
        }

        public string Method { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public string Body { get; set; }
        public string MediaType { get; set; } = "application/json";
    }
}