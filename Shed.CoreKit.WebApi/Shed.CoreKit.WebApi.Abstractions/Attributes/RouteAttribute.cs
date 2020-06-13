using System;

namespace Shed.CoreKit.WebApi
{
    public class RouteAttribute: Attribute
    {
        public string[] Segments;

        public RouteAttribute(string pattern)
        {
            Segments = pattern.Trim('/').Split('/');
        }
    }
}
