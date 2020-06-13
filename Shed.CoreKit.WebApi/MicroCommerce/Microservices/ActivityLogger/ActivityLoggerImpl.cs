using MicroCommerce;
using MicroCommerce.Models;
using System.Collections.Generic;
using System.Linq;

namespace ActivityLogger
{
    public class ActivityLoggerImpl : IActivityLogger
    {
        private IShoppingCart _shoppingCart;

        private static long timestamp;
        private static List<LogEvent> _log = new List<LogEvent>();

        public ActivityLoggerImpl(IShoppingCart shoppingCart)
        {
            _shoppingCart = shoppingCart;
        }

        public IEnumerable<LogEvent> Get(long timestamp)
        {
            return _log.Where(i => i.Timestamp > timestamp);
        }

        public void ReceiveEvents()
        {
            var cartEvents = _shoppingCart.GetCartEvents(timestamp);

            if(cartEvents.Count() > 0)
            {
                timestamp = cartEvents.Max(c => c.Timestamp);
                _log.AddRange(cartEvents.Select(e => new LogEvent
                {
                    Description = $"{GetEventDesc(e.Type)}: '{e.Order.Product.Name} ({e.Order.Quantity})'"
                }));
            }
        }

        private string GetEventDesc(CartEventTypeEnum type)
        {
            switch (type)
            {
                case CartEventTypeEnum.OrderAdded: return "order added";
                case CartEventTypeEnum.OrderChanged: return "order changed";
                case CartEventTypeEnum.OrderRemoved: return "order removed";
                default: return "unknown operation";
            }
        }
    }
}
