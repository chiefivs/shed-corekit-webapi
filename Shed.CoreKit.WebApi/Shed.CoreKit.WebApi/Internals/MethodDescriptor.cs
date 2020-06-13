using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Shed.CoreKit.WebApi
{

    internal class MethodDescriptor
    {
        public string[] Segments;
        public string HttpMethod;
        public MethodInfo MethodInfo;
        public string MethodName => MethodInfo.Name;

        private static readonly Type routeAttrType;

        public MethodDescriptor(Type type, string name, object[] args)
            : this(GetInterfaceSegments(type), GetMethodInfo(type, name, args), null)
        {
        }

        static MethodDescriptor()
        {
            routeAttrType = typeof(RouteAttribute);
        }

        private MethodDescriptor(string[] interfaceSegments, MethodInfo methodInfo, string root)
        {
            var attributes = methodInfo.GetCustomAttributes(false);
            HttpMethod = GetHttpMethodFromAttributes(attributes) ?? GetHttpMethodFromName(methodInfo);

            var routeAttr = attributes.FirstOrDefault(a => a.GetType() == routeAttrType) as RouteAttribute;

            var segments = new List<string>();
            if (!string.IsNullOrWhiteSpace(root))
                segments.Add(root);

            if (interfaceSegments != null)
                segments.AddRange(interfaceSegments);

            if (routeAttr != null)
                segments.AddRange(routeAttr.Segments);
            else
                segments.Add(methodInfo.Name.ToLower());

            Segments = segments.ToArray();
            MethodInfo = methodInfo;
        }

        public static IEnumerable<MethodDescriptor> GetAll(Type type, string root)
        {
            var rootSegments = GetInterfaceSegments(type);
            var descriptors = type.GetMethods()
                .Select(mtdInfo => new MethodDescriptor(rootSegments, mtdInfo, root));

            return descriptors;
        }

        public bool IsRouteMatch(string path)
        {
            var pathSegments = (path ?? "").Trim('/').Split('/');
            if (Segments.Length != pathSegments.Length)
                return false;

            for (int n = 0; n < Segments.Length; n++)
            {
                if (Segments[n] != pathSegments[n] && !Segments[n].StartsWith('{'))
                    return false;
            }

            return true;
        }

        public object[] ExtractParamsFromRequest(HttpRequest request)
        {
            var paramInfos = MethodInfo.GetParameters();
            var queryValues = GetQueryString(request);

            var paramValues = paramInfos.Select(pi =>
            {
                if (pi.GetCustomAttribute<FromBodyAttribute>() != null)
                {
                    return ContentHelper.DeserializeBodyFromRequest(pi.ParameterType, request);
                }

                if (IsPrimitive(pi.ParameterType))
                {
                    return ParseParamFromQuery(pi.ParameterType, queryValues.Get(pi.Name.ToLower()) ?? "");
                }
                else
                {
                    var instance = Activator.CreateInstance(pi.ParameterType);
                    var instanceProps = instance.GetType().GetProperties();
                    foreach (var prop in instanceProps)
                    {
                        var s = queryValues[prop.Name.ToLower()];
                        if (s == null)
                            continue;

                        prop.SetValue(instance, ParseParamFromQuery(prop.PropertyType, s));
                    }

                    return instance;
                }
            }).ToArray();

            return paramValues;
        }

        public string CreateQueryString(object[] pars)
        {
            var paramInfos = MethodInfo.GetParameters();
            var segmentsList = new List<string>(Segments);
            var queryParamsList = new List<string>();
            for(int n = 0; n < paramInfos.Length; n++)
            {
                var paramInfo = paramInfos[n];

                if (paramInfo.GetCustomAttribute<FromBodyAttribute>() != null)
                    continue;

                Action<string, object> putParam = (string name, object value) =>
                {
                    int index = segmentsList.FindIndex(segment => segment.ToLower() == "{" + name.ToLower() + "}");
                    if (index >= 0)
                        segmentsList[index] = ConvertToString(value);
                    else
                        queryParamsList.Add($"{name}={ConvertToString(value)}");
                };

                if (IsPrimitive(paramInfo.ParameterType))
                {
                    putParam(paramInfo.Name, pars[n]);
                }
                else
                {
                    var propInfos = paramInfo.ParameterType.GetProperties();
                    var parInst = pars[n];
                    foreach(var propInfo in propInfos)
                    {
                        putParam(propInfo.Name, propInfo.GetValue(parInst));
                    }
                }
            }

            var resultPartsList = new List<string>(new[] { string.Join("/", segmentsList) });
            if (queryParamsList.Any())
                resultPartsList.Add(string.Join("&", queryParamsList));

            return string.Join("?", resultPartsList);
        }

        public object GetBodyParam(object[] pars)
        {
            var index = new List<ParameterInfo>(MethodInfo.GetParameters())
                .FindIndex(pi => pi.GetCustomAttribute<FromBodyAttribute>() != null);

            return index >= 0 ? pars[index] : null;
        }

        public override string ToString()
        {
            return $"{HttpMethod} {string.Join("/", Segments)}";
        }

        private static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type.Namespace == null || type.Namespace.Equals("System");

        }

        private string ConvertToString(object obj)
        {
            return obj?.ToString() ?? "";
        }

        private static string[] GetInterfaceSegments(Type type)
        {
            var routeAttr = type.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Segments;
        }

        private static MethodInfo GetMethodInfo(Type type, string name, object[] args)
        {
            return type.GetMethods().First(m => {
                if (m.Name != name)
                    return false;

                var pars = m.GetParameters();
                if (pars.Length != args.Length)
                    return false;

                for(int n = 0; n < pars.Length; n++)
                {
                    if (args[n] != null && pars[n].ParameterType != args[n].GetType())
                        return false;
                }

                return true;
            });
        }

        private static string GetHttpMethodFromAttributes(object[] attributes)
        {
            if (attributes.Any(a => a.GetType() == typeof(HttpDeleteAttribute)))
                return HttpMethods.Delete;

            if (attributes.Any(a => a.GetType() == typeof(HttpGetAttribute)))
                return HttpMethods.Get;

            if (attributes.Any(a => a.GetType() == typeof(HttpPatchAttribute)))
                return HttpMethods.Patch;

            if (attributes.Any(a => a.GetType() == typeof(HttpPostAttribute)))
                return HttpMethods.Post;

            if (attributes.Any(a => a.GetType() == typeof(HttpPutAttribute)))
                return HttpMethods.Put;

            return null;
        }

        private static string GetHttpMethodFromName(MethodInfo mtd)
        {
            var name = mtd.Name.ToUpper();

            if (name.StartsWith(HttpMethods.Delete))
                return HttpMethods.Delete;

            if (name.StartsWith(HttpMethods.Get))
                return HttpMethods.Get;

            if (name.StartsWith(HttpMethods.Patch))
                return HttpMethods.Patch;

            if (name.StartsWith(HttpMethods.Post))
                return HttpMethods.Post;

            if (name.StartsWith(HttpMethods.Put))
                return HttpMethods.Put;

            return HttpMethods.Get;
        }

        private static object ParseParamFromQuery(Type dataType, string ValueToConvert)
        {
            TypeConverter obj = TypeDescriptor.GetConverter(dataType);
            object value = obj.ConvertFromString(null, CultureInfo.InvariantCulture, ValueToConvert ?? "");
            return value;
        }

        private NameValueCollection GetQueryString(HttpRequest request)
        {
            var queryValues = HttpUtility.ParseQueryString(request.QueryString.Value);

            var urlSegments = request.Path.HasValue
                ? request.Path.Value.TrimStart('/').Split('/')
                : new string[] { };

            for (int n = 0; n < Segments.Length; n++)
            {
                if (n > urlSegments.Length - 1)
                    break;

                if (!Segments[n].StartsWith('{'))
                    continue;

                queryValues.Add(Segments[n].Trim('{', '}'), urlSegments[n]);
            }

            return queryValues;
        }
    }
}
