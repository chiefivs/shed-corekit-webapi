using System.Collections.Generic;

namespace MicroCommerce.Models
{
    public class Cart
    {
        public IEnumerable<Order> Orders { get; set; }
    }
}
