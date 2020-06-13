using System;
using System.Collections.Generic;

namespace Shed.CoreKit.WebApi.Exceptions
{
    /// <summary>
    /// Wrapper for exceptions
    /// </summary>
    public class ExceptionInfo
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public ExceptionInfo InnerExcepton { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Data { get; set; }

        public ExceptionInfo()
        {

        }

        public ExceptionInfo(Exception ex)
        {
            Type = ex.GetType().FullName;
            Message = ex.Message;
            Source = ex.Source;
            StackTrace = ex.StackTrace;
            if(ex.InnerException != null)
            {
                InnerExcepton = new ExceptionInfo(ex.InnerException);
            }

            if(ex.Data != null)
            {
                var data = new List<KeyValuePair<string, string>>();
                var keys = ex.Data.Keys;
                foreach (var key in keys)
                {
                    try
                    {
                        data.Add(KeyValuePair.Create(key.ToString(), ex.Data[key].ToString()));
                    }
                    catch
                    {

                    }
                }

            }
        }

        public string ToJsonString()
        {
            return ContentHelper.SerializeBody(this, ContentTypes.Json);
        }
    }
}
