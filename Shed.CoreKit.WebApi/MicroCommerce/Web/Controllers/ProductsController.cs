using Microsoft.AspNetCore.Mvc;

namespace MicroCommerce.Web.Controllers
{
    public class ProductsController: Controller
    {
        private IProductCatalog _catalog;
        private IShoppingCart _cart;

        public ProductsController(IProductCatalog catalog, IShoppingCart cart)
        {
            _catalog = catalog;
            _cart = cart;
        }

        public object GetProducts()
        {
            var res = _catalog.Get();
            return res;
        }

        public object GetCart()
        {
            var res = _cart.Get();
            return res;
        }
    }
}
