using System;

namespace Shed.CoreKit.WebApi.Exceptions
{
    /// <summary>
    /// Special exception type for web api interactions
    /// </summary>
    public class WebApiException: Exception
    {
        public ExceptionInfo Info { get; set; }

        public WebApiException(ExceptionInfo info)
        {
            Info = info;
        }

        public WebApiException(Exception ex)
        {
            Info = new ExceptionInfo(ex);
        }
    }
}
