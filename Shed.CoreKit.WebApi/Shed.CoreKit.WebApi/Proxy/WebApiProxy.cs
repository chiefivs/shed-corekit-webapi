using Microsoft.AspNetCore.Http;
using Shed.CoreKit.WebApi.Exceptions;
using Shed.CoreKit.WebApi.Manager.ProxyGenerator;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Shed.CoreKit.WebApi.Proxy
{
    /// <summary>
    /// Proxy type for web api clients
    /// </summary>
    public class WebApiProxy : DynamicProxy
    {
        internal Uri Endpoint;
        internal IServiceProvider ServiceProvider;
        internal IHttpContextAccessor HttpContextAccessor;
        internal CorrelationTokenFactory CorrelationTokenFactory;
        internal WebApiEndpointOptions Options;

        protected override bool TryInvokeMember(Type interfaceType, string name, object[] args, out object result)
        {
            var descriptor = new MethodDescriptor(interfaceType, name, args);
            var query = descriptor.CreateQueryString(args);
            var contentType = GetContentType();
            var body = ContentHelper.SerializeBody(descriptor.GetBodyParam(args), contentType);

            try
            {
                var task = ExecuteRequestAsync(query, descriptor.HttpMethod, contentType, body);
                task.Wait();
                var message = task.Result;

                if (message.IsSuccessStatusCode)
                {
                    result = ContentHelper.DeserializeBodyFromResponse(descriptor.MethodInfo.ReturnType, message);
                    return true;
                }
                else
                {
                    try
                    {
                        var info = (ExceptionInfo)ContentHelper.DeserializeBodyFromResponse(typeof(ExceptionInfo), message);
                        throw new WebApiException(info);
                    }
                    catch
                    {
                        var textTask = message.Content.ReadAsStringAsync();
                        textTask.Wait();
                        throw new WebApiException(new Exception(textTask.Result));
                    }
                }
            }
            catch(Exception ex)
            {
                throw new WebApiException(ex);
            }
        }

        #region Not implemented
        protected override bool TryGetMember(Type interfaceType, string name, out object result)
        {
            throw new NotImplementedException();
        }

        protected override bool TrySetEvent(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }
        #endregion

        private async Task<HttpResponseMessage> ExecuteRequestAsync(string url, string method, string contentType, string body)
        {
            using (var httpClient = CreateHttpClient())
            {
                httpClient.BaseAddress = Endpoint;
                using (var request = new HttpRequestMessage(new HttpMethod(method), url))
                {
                    if (CorrelationTokenFactory != null)
                    {
                        request.Headers.Add(
                            CorrelationTokenFactory.CorrelationTokenHeaderName,
                            CorrelationTokenFactory.GetCorrelationToken());
                    }

                    request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(contentType));
                    request.Content = new StringContent(body ?? "", Encoding.UTF8, contentType);

                    return await httpClient.SendAsync(request);
                }
            }
        }

        private string GetContentType()
        {
            return HttpContextAccessor?.HttpContext?.Request?.ContentType ?? Options.ContentType;
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = ServiceProvider.GetService(typeof(HttpClient)) as HttpClient ?? new HttpClient();

            return httpClient;
        }
    }
}
