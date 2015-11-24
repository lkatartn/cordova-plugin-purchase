(function () {
    "use strict";

	//   {
    //    purchaseDateString: "2012-08-30T23:08:52Z",
    //    expirationDateString: "2012-08-30T23:08:52Z",
    //    receipt: 'receipt is UTF-8 xml encoded in base64'
    //   }
    store.setProductData = function(product, data) {

        store.log.debug("wp8 -> product data for " + product.id);
        store.log.debug(data);
        //
        product.transaction = {
            type: 'windows-store', 
            purchaseDate : data.purchaseDateString,
            expirationDate : data.expirationDateString,
            receipt: data.receipt
        };
    };

})();