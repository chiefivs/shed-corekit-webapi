using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shed.CoreKit.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Shed.CoreKit.WebApi
{
    /// <summary>
    /// App builder extensions
    /// </summary>
    public static class ApplicationBuilderExt
    {
        /// <summary>
        /// Use a service implementation as a web api endpoint
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="appBuilder"></param>
        /// <param name="root">Not required. Root path for service endoint</param>
        /// <returns></returns>
        public static IApplicationBuilder UseWebApiEndpoint<TService>(this IApplicationBuilder appBuilder, string root = null)
        {
            var descriptors = MethodDescriptor.GetAll(typeof(TService), root);

            foreach(var descriptor in descriptors)
            {
                appBuilder.Use(async (context, next) =>
                {
                    if(!descriptor.IsRouteMatch(context.Request.Path.Value))
                    {
                        await next();
                        return;
                    }

                    var descriptorHttpMethod = descriptor.HttpMethod.ToLower();
                    var requestHttpMethod = context.Request.Method.ToLower();
                    if (descriptorHttpMethod != requestHttpMethod)
                    {
                        await next();
                        return;
                    }

                    var contentType = ContentHelper.GetContentType(context.Request.ContentType);
                    if(string.IsNullOrEmpty(contentType))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        var info = new ExceptionInfo(new ArgumentException($"Http request must contain a 'Content-Type' header '{ContentTypes.Xml}' or '{ContentTypes.Json}'"));
                        await context.Response.WriteAsync(ContentHelper.SerializeBody(info, ContentTypes.Json));
                        return;
                    }

                    try
                    {
                        var instance = (TService)appBuilder.ApplicationServices.GetService(typeof(TService));
                        var instanceMtd = instance.GetType().GetMethods().First(m => {
                            if (m.Name != descriptor.MethodName)
                                return false;

                            var methodParams = m.GetParameters();
                            var interfaceParams = descriptor.MethodInfo.GetParameters();
                            if (methodParams.Length != interfaceParams.Length)
                                return false;

                            for(int n = 0; n < methodParams.Length; n++)
                            {
                                if (methodParams[n].ParameterType != interfaceParams[n].ParameterType)
                                    return false;
                            }

                            return true;
                        });

                        var mtdParams = descriptor.ExtractParamsFromRequest(context.Request);
                        var result = instanceMtd.Invoke(instance, mtdParams);
                        context.Response.ContentType = contentType;

                        context.Response.StatusCode = StatusCodes.Status200OK;

                        if(instanceMtd.ReturnType.Name != "Void")
                        {
                            var text = ContentHelper.SerializeBody(result, contentType);
                            await context.Response.WriteAsync(text);
                        }
                    }
                    catch(Exception ex)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        var info = new ExceptionInfo(ex);
                        await context.Response.WriteAsync(info.ToJsonString());
                    }
                });

            }

            return appBuilder;
        }

        /// <summary>
        /// Redirect all calls of this endpoint
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="rootPath">Root path</param>
        /// <param name="endpoint">Endpoint configuration for redirect</param>
        /// <returns></returns>
        public static IApplicationBuilder UseWebApiRedirect(this IApplicationBuilder appBuilder, string rootPath, WebApiEndpoint endpoint)
        {
            rootPath = rootPath.Trim();
            var descriptors = MethodDescriptor.GetAll(endpoint.Type, null);

            foreach(var descriptor in descriptors)
            {
                appBuilder.Use(async (context, next) =>
                {
                    await next();
                    if (context.Response.StatusCode != (int)HttpStatusCode.NotFound)
                        return;

                    var fullPath = context.Request.Path.Value.Trim('/');
                    if (!fullPath.StartsWith(rootPath))
                        return;

                    var redirectPath = fullPath.Substring(rootPath.Length);
                    if (!descriptor.IsRouteMatch(redirectPath))
                        return;

                    var redirectUrlBuilder = new UriBuilder(endpoint.BaseUri);

                    var pathSegments = new List<string>();
                    pathSegments.AddRange(endpoint.BaseUri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
                    pathSegments.AddRange(redirectPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
                    redirectUrlBuilder.Path = string.Join('/', pathSegments);
                    redirectUrlBuilder.Query = context.Request.QueryString.Value;

                    context.Response.StatusCode = (int)HttpStatusCode.RedirectKeepVerb;
                    context.Response.Headers.Add("Location", redirectUrlBuilder.Uri.ToString());
                });
            }

            return appBuilder;
        }

        /// <summary>
        /// Use correlation tokent when handling the request
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCorrelationToken(this IApplicationBuilder appBuilder)
        {
            appBuilder.Use(async (context, next) =>
            {
                var factory = appBuilder.ApplicationServices.GetService(typeof(CorrelationTokenFactory)) as CorrelationTokenFactory;
                if (factory == null)
                    throw new Exception("Please, add correlation token service to the ConfigureServices by using 'services.AddCorrelationToken(<name>)'.");
                
                context.Response.Headers.Add(factory.CorrelationTokenHeaderName, factory.GetCorrelationToken());

                await next();
            });

            return appBuilder;
        }
    }
}
