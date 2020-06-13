using MicroCommerce.Models;
using Shed.CoreKit.WebApi;
using System;
using System.Collections.Generic;

namespace MicroCommerce
{
    public interface IProductCatalog
    {
        IEnumerable<Product> Get();

        [Route("get/{productId}")]
        public Product Get(Guid productId);
    }
}
