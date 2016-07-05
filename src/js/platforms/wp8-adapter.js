(function() {
"use strict";

var initialized = false;
var skus = [];

store.when("refreshed", function() {
    if (!initialized) init();
});

store.when("re-refreshed", function() {
    store.ready(true);
});

function init() {
    if (initialized) return;
    initialized = true;

    for (var i = 0; i < store.products.length; ++i)
        skus.push(store.products[i].id);

    store.inappbilling.init(iabReady,
        function(err) {
            initialized = false;
            store.error({
                code: store.ERR_SETUP,
                message: 'Init failed - ' + err
            });
        },
        {
            showLog: store.verbosity >= store.DEBUG ? true : false
        },
        skus);
}

function iabReady() {
    store.log.debug("plugin -> ready");
    store.inappbilling.getAvailableProducts(iabLoaded, function(err) {
        store.error({
            code: store.ERR_LOAD,
            message: 'Loading product info failed - ' + err
        });
    });
}

function iabLoaded(validProducts) {
    store.log.debug("plugin -> loaded - " + JSON.stringify(validProducts));
    var p, i;
    for (i = 0; i < validProducts.length; i++) {

        if (validProducts[i].id)
            p = store.products.byId[validProducts[i].id];
        else
            p = null;
        store.log.debug(" loaded valid product " + p);
        if (p) {
            p.set({
                title: validProducts[i].title || validProducts[i].name,
                price: validProducts[i].price || validProducts[i].formattedPrice || "",
                description: validProducts[i].description,
                currency: validProducts[i].price_currency_code ? validProducts[i].price_currency_code : "",
                state: store.VALID
            });
            p.trigger("loaded");
        }
    }
    for (i = 0; i < skus.length; ++i) {
        p = store.products.byId[skus[i]];
        if (p && !p.valid) {
            p.set("state", store.INVALID);
            p.trigger("loaded");
        }
    }
   iabGetPurchases();
}
function iabGetPurchases() {
    store.inappbilling.getPurchases(
        function(purchases) { // success
            // example purchases data:
            //
            // [
            //   {
            //     "id":"ProductId",
            //     "active":"true",
            //     "consumable":"true/false",
            //     "expirationDate":"isoString"
            //   },
            //   { ... }
            // ]
            store.log.debug("get purchases: " + JSON.stringify(purchases));
            if (purchases && purchases.length) {
                for (var i = 0; i < purchases.length; i++ ) {
                    var purchase = purchases[i];
                    var p = store.get(purchase.id);
                    if (!p) {
                        store.log.warn("plugin -> user owns a non-registered product");
                        continue;
                    }
                    p.set('state', store.APPROVED);
                }
            }
            store.ready(true);
        },
        function() { // error
            // TODO
        }
    );
}


store.when("requested", function(product) {
    store.ready(function() {
        if (!product) {
            store.error({
                code: store.ERR_INVALID_PRODUCT_ID,
                message: "Trying to order an unknown product"
            });
            return;
        }
        if (!product.valid) {
            product.trigger("error", [new store.Error({
                code: store.ERR_PURCHASE,
                message: "`purchase()` called with an invalid product"
            }), product]);
            return;
        }

        // Initiate the purchase
        product.set("state", store.INITIATED);

        var method = 'buy';

        store.inappbilling[method](function(data) {
            // Success callback.
            // xml example
            //      https://msdn.microsoft.com/en-us/library/windows/apps/mt219692.aspx
            // example data:
            // {
            //     base64: XML encoded in base64
            // }
            var xmlString = window.atob(data.base64);
            var parser = new DOMParser();
            var doc = parser.parseFromString(xmlString, "text/xml");
            var productData = {};

            var productReceipt = doc.documentElement.getElementsByTagName("ProductReceipt")[0];
            productData.purhaseDateString = productReceipt.attributes.PurchaseDate.value || "";
            productData.expirationDateString = (productReceipt.attributes.ExpirationDate && productReceipt.attributes.ExpirationDate.value) || "";
            productData.receipt = data.base64;

            store.setProductData(product, productData);
            product.set("state", store.APPROVED);
        },
        function(err, code) {
            store.log.info("plugin -> " + method + " error " + code);
            if (code === store.ERR_PAYMENT_CANCELLED) {
                // This isn't an error,
                // just trigger the cancelled event.
                product.transaction = null;
                product.trigger("cancelled");
            }
            else {
                store.error({
                    code: code || store.ERR_PURCHASE,
                    message: "Purchase failed: " + err
                });
            }
        }, product.id);
    });
});

/// #### finish a purchase
/// When a consumable product enters the store.FINISHED state,
/// `consume()` the product.
store.when("product", "finished", function(product) {
    store.log.debug("plugin -> consumable finished");
    if (product.type === store.CONSUMABLE) {
        store.inappbilling.consumePurchase(
            function() { // success
                store.log.debug("plugin -> consumable consumed");
                product.set('state', store.VALID);
            },
            function(err, code) { // error
                // can't finish.
                store.error({
                    code: code || store.ERR_UNKNOWN,
                    message: err
                });
            },
            product.id);
    }
    else {
        product.set('state', store.OWNED);
    }
});

})();
