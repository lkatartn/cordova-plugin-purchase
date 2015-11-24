using System;
using System.Collections;
using System.Text;
using WPCordovaClassLib.Cordova;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;

#if DEBUG
using MockIAPLib;
using Store = MockIAPLib;
#else
using Windows.ApplicationModel.Store;
#endif

public class InAppBillingPlugin : BaseCommand
{
    private void SetupMockIAP(string xml)
    {
            MockIAP.Init();

            MockIAP.RunInMockMode(true);
            MockIAP.SetListingInformation(1, "en-us", "A description", "1", "TestApp");

            MockIAP.PopulateIAPItemsFromXml(xml);
    }

    public void init(string skus) {
        DispatchCommandResult();
    }

    public void setTestMode(string options)
    {

        var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
        var file = await folder.GetFileAsync("productMock.xml");
        var contents = await Windows.Storage.FileIO.ReadTextAsync(file);

        SetupMockIAP(contents);
        DispatchCommandResult();

    }
    public void getAvailableProducts(string options)
    {
        ListingInformation li = await CurrentApp.LoadListingInformationAsync();
        //pls in read-only dictionary<string, ProductListing>
        var pls = li.ProductListings;
        //one string per one product
        List<string> productJsons;
        //making json string from ProductListing class
        foreach (ProductListing pl in pls.Values) {
            string pj = String.Format(
                @"{{ 
                    id:""{0}"",
                    title:""{1}"",
                    type:""{2}"",
                    description:""{3}"",
                    price:""{4}"",
                    currency:""{5}""
                    }}",
                    pl.ProductId,
                    pl.Name,
                    pl.ProductType,
                    pl.Description,
                    pl.FormattedPrice,
                    pl.CurrencyCode );
            productJsons.Add(pj);
        }
        string jsonResult = String.Join(",",productJsons.ToArray());
        jsonResult = "[" + jsonResult + "]";
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, jsonResult));
    }
    public void buy(string options)
    {
        //Getting productId from Json options
        string productId = JsonHelper.Deserialize<string[]>(options)[0];
        
        var receiptXml = await CurrentApp.RequestProductPurchaseAsync(productId, true);
        this.receipts.Add(receiptXml);
        var bytes = Encoding.UTF8.GetBytes(receiptXml);
        var base64 = Convert.ToBase64String(bytes);
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, "{base64: \""+base64+"\"}"));
    }
    public void consumePurchase(string options)
    {
        //Getting productId from Json options
        string productId = JsonHelper.Deserialize<string[]>(options)[0];
        try {}
        CurrentApp.ReportProductFulfillment(productId);
        DispatchCommandResult();
    }
    public void getProductDetails(string options)
    {
        DispatchCommandResult();
    }
    public void getPurchases(string options)
    {
        DispatchCommandResult();
    }
    private System.Collections.Generic.IReadOnlyDictionary<String, ProductListing> localListing;
    private List<string> receipts;
}
