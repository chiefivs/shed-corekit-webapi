using MicroCommerce.Models;
using System.Collections.Generic;

namespace MicroCommerce
{
    public interface IActivityLogger
    {
        IEnumerable<LogEvent> Get(long timestamp);
    }
}
