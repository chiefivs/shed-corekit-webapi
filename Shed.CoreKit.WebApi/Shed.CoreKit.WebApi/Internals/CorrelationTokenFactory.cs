using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Shed.CoreKit.WebApi
{
    internal class CorrelationTokenFactory
    {
        public readonly string CorrelationTokenHeaderName;
        IHttpContextAccessor HttpContextAccessor;

        public CorrelationTokenFactory(IHttpContextAccessor accessor, string name)
        {
            HttpContextAccessor = accessor;
            CorrelationTokenHeaderName = name ?? "Correlation-Token";
        }

        public string GetCorrelationToken()
        {
            var token = HttpContextAccessor?.HttpContext?.Request?.Headers
                .GetCommaSeparatedValues(CorrelationTokenHeaderName)
                .FirstOrDefault();

            if(token == null)
                token = HttpContextAccessor?.HttpContext?.Response?.Headers
                    .GetCommaSeparatedValues(CorrelationTokenHeaderName)
                    .FirstOrDefault();

            return string.IsNullOrEmpty(token) ? Guid.NewGuid().ToString() : token;
        }
    }
}
