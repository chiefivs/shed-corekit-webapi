using MicroCommerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroCommerce.ShoppingCart
{
    public class ShoppingCartImpl : IShoppingCart
    {
        private static List<Order> _orders = new List<Order>();
        private static List<CartEvent> _events = new List<CartEvent>();
        private IProductCatalog _catalog;

        public ShoppingCartImpl(IProductCatalog catalog)
        {
            _catalog = catalog;
        }

        public Cart AddOrder(Guid productId, int qty)
        {
            var order = _orders.FirstOrDefault(i => i.Product.Id == productId);
            if(order != null)
            {
                order.Quantity += qty;
                CreateEvent(CartEventTypeEnum.OrderChanged, order);
            }
            else
            {
                var product = _catalog.Get(productId);
                if (product != null)
                {
                    order = new Order
                    {
                        Id = Guid.NewGuid(),
                        Product = product,
                        Quantity = qty
                    };

                    _orders.Add(order);
                    CreateEvent(CartEventTypeEnum.OrderAdded, order);
                }
            }

            return Get();
        }

        public Cart DeleteOrder(Guid orderId)
        {
            var order = _orders.FirstOrDefault(i => i.Id == orderId);
            if(order != null)
            {
                _orders.Remove(order);
                CreateEvent(CartEventTypeEnum.OrderRemoved, order);
            }

            return Get();
        }

        public Cart Get()
        {
            return new Cart
            {
                Orders = _orders
            };
        }

        public IEnumerable<CartEvent> GetCartEvents(long timestamp)
        {
            return _events.Where(e => e.Timestamp > timestamp);
        }

        private void CreateEvent(CartEventTypeEnum type, Order order)
        {
            _events.Add(new CartEvent
            {
                Timestamp = DateTime.Now.Ticks,
                Time = DateTime.Now,
                Order = order.Clone(),
                Type = type
            });
        }
    }
}
