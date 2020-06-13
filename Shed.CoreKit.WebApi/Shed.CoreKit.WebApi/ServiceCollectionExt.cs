using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shed.CoreKit.WebApi.Proxy;
using System;
using System.Net.Http;

namespace Shed.CoreKit.WebApi
{
    /// <summary>
    /// Web api services extension
    /// </summary>
    public static class ServiceCollectionExt
    {
        /// <summary>
        /// Register a web api client (proxy) for interface 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IServiceCollection AddWebApiEndpoints(this IServiceCollection services, params WebApiEndpoint[] options)
        {
            var factory = new WebApiProxyFactory();
            foreach(var client in options)
            {
                services.AddTransient(client.Type, provider =>
                {
                    var proxy = factory.CreateDynamicProxy(client.Type) as WebApiProxy;
                    proxy.Endpoint = client.BaseUri;
                    proxy.Options = client.Options;
                    proxy.ServiceProvider = provider;
                    proxy.HttpContextAccessor = provider.GetService<IHttpContextAccessor>();
                    proxy.CorrelationTokenFactory = provider.GetService<CorrelationTokenFactory>();

                    return proxy;
                });
            }

            return services;
        }

        /// <summary>
        /// If you are going to use correlation tokens, you should register it before
        /// </summary>
        /// <param name="services"></param>
        /// <param name="correlationTokenName">Not required, by default "Correlation-Token"</param>
        /// <returns></returns>
        public static IServiceCollection AddCorrelationToken(this IServiceCollection services, string correlationTokenName = null)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationTokenFactory>(provider =>
            {
                var accessor = provider.GetService<IHttpContextAccessor>();
                return new CorrelationTokenFactory(accessor, correlationTokenName);
            });

            return services;
        }
    }

    /// <summary>
    /// Endpoint configuration
    /// </summary>
    public class WebApiEndpoint
    { 
        internal Type Type;

        internal Uri BaseUri;

        internal WebApiEndpointOptions Options;

        /// <summary>
        /// Endpoint parameters for interface
        /// </summary>
        /// <param name="type">Interface type</param>
        /// <param name="baseUri">Base uri for endpoint</param>
        /// <param name="options">Not required. By default Content-Type is 'application/json'</param>
        public WebApiEndpoint(Type type, Uri baseUri, WebApiEndpointOptions options = null)
        {
            BaseUri = baseUri;
            Type = type;
            Options = options ?? new WebApiEndpointOptions();
        }
    }

    /// <summary>
    /// Web api endpoint parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WebApiEndpoint<T>: WebApiEndpoint
    {
        /// <summary>
        /// Endpoint parameters for interface by type
        /// </summary>
        /// <param name="baseUri">Base uri for endpoint</param>
        /// <param name="options">Not required. By default Content-Type is 'application/json'</param>
        public WebApiEndpoint(Uri baseUri, WebApiEndpointOptions options = null) : base(typeof(T), baseUri, options)
        {

        }
    }

    /// <summary>
    /// Additional options for web api endpoint
    /// </summary>
    public class WebApiEndpointOptions
    {
        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; } = ContentTypes.Json;
    }
}
