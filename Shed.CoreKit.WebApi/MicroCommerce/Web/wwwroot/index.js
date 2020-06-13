function request(url, method, data) {
    return $.ajax({
        cache: false,
        dataType: 'json',
        url: url,
        data: data ? JSON.stringify(data) : null,
        method: method,
        contentType: 'application/json'
    });
}

function IndexModel() {
    this.products = ko.observableArray([]);
    this.shoppingCart = ko.observableArray(null);
    this.logs = ko.observableArray([]);
    var _this = this;

    this.getproducts = function () {
        request('/api/products/get', 'GET')
            .done(function (products) {
                _this.products(products);
                console.log("get products: ", products);
            }).fail(function (err) {
                console.log("get products error: ", err);
            });
    };

    this.getcart = function () {
        request('/api/orders/get', 'GET')
            .done(function (cart) {
                _this.shoppingCart(cart);
                console.log("get cart: ", cart);
            }).fail(function (err) {
                console.log("get cart error: ", err);
            });
    };

    this.addorder = function (id, qty) {
        request(`/api/orders/addorder/${id}/${qty}`, 'PUT')
            .done(function (cart) {
                _this.shoppingCart(cart);
                console.log("add order: ", cart);
            }).fail(function (err) {
                console.log("add order error: ", err);
            });
    };

    this.delorder = function (id) {
        request(`/api/orders/deleteorder?orderId=${id}`, 'DELETE')
            .done(function (cart) {
                _this.shoppingCart(cart);
                console.log("del order: ", cart);
            }).fail(function (err) {
                console.log("del order error: ", err);
            });
    };

    this.timestamp = Number(0);
    this.updateLogsInProgress = false;
    this.updatelogs = function () {
        if (_this.updateLogsInProgress)
            return;

        _this.updateLogsInProgress = true;
        request(`/api/logs/get?timestamp=${_this.timestamp}`, 'GET')
            .done(function (logs) {
                if (!logs.length) {
                    return;
                }

                ko.utils.arrayForEach(logs, function (item) {
                    _this.logs.push(item);
                    _this.timestamp = Math.max(_this.timestamp, Number(item.timestamp));
                });
                console.log("update logs: ", logs, _this.timestamp);
            }).fail(function (err) {
                console.log("update logs error: ", err);
            }).always(function () { _this.updateLogsInProgress = false; });
    };

    this.getproducts();
    this.getcart();
    this.updatelogs();
    setInterval(() => _this.updatelogs(), 1000);
}
