using Shed.CoreKit.WebApi.Manager.ProxyGenerator;

namespace Shed.CoreKit.WebApi.Proxy
{
    internal class WebApiProxyFactory: DynamicProxyFactory<WebApiProxy>
    {
        public WebApiProxyFactory() : base(new DynamicInterfaceImplementor())
        {

        }

    }
}
