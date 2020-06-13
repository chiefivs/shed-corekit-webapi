using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Serialization;

namespace Shed.CoreKit.WebApi
{
    internal static class ContentHelper
    {
        private static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static readonly string[] AllowedContentTypes = new[]
        {
            ContentTypes.Xml,
            ContentTypes.Json
        };

        public static string SerializeBody(object obj, string contentType)
        {
            if (obj == null)
                return string.Empty;

            if (contentType == ContentTypes.Xml)
            {
                using (var writer = new StringWriter())
                {
                    var serializer = new XmlSerializer(obj.GetType());
                    serializer.Serialize(writer, obj);

                    return writer.ToString();
                }
            }
            else if (contentType == ContentTypes.Json)
            {
                return JsonConvert.SerializeObject(obj, JsonSerializerSettings);
            }

            return string.Empty;
        }

        public static object DeserializeBodyFromRequest(Type type, HttpRequest request)
        {
            var contentType = GetContentType(request.ContentType);
            return Deserialize(type, request.Body, contentType);
        }

        public static object DeserializeBodyFromResponse(Type type, HttpResponseMessage response)
        {
            var contentType = GetContentType(response.Content.Headers.ContentType?.ToString());
            var bodyStreamTask = response.Content.ReadAsStreamAsync();
            bodyStreamTask.Wait();
            
            return Deserialize(type, bodyStreamTask.Result, contentType);
        }

        private static object Deserialize(Type type, Stream bodyStream, string contentType)
        {
            var text = ReadStream(bodyStream);
            if (contentType == ContentTypes.Xml)
            {
                using (var reader = new StringReader(text))
                {
                    var serializer = new XmlSerializer(type);
                    return serializer.Deserialize(reader);
                }
            }
            else if (contentType == ContentTypes.Json)
            {
                return JsonConvert.DeserializeObject(text, type, JsonSerializerSettings);
            }

            else return null;
        }

        public static string GetContentType(string header)
        {
            var values = (header ?? string.Empty).Split(';').Select(i => i.Trim());
            return values.FirstOrDefault(v => AllowedContentTypes.Contains(v)) ?? string.Empty;
        }

        private static string ReadStream(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                var task = streamReader.ReadToEndAsync();
                task.Wait();
                return task.Result;
            }
        }
    }
}
