using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, "init"));
    }

    public void setTestMode(string options)
    {

        var contents = 
            @"<?xml version=""1.0""?>
            <ProductListings>
                <ProductListing Key=""test1"" Purchased=""false"" Fulfilled=""false"">
                    <Name>Testproduct</Name>
                    <Description>A sample product listing</Description>
                    <ProductId>yearlysubscription</ProductId>
                    <ProductType>Consumable</ProductType>
                    <FormattedPrice>$9.99</FormattedPrice>
                    <CurrencyCode>USD</CurrencyCode>
                    <ImageUri></ImageUri>
                    <Keywords>test;product</Keywords>
                    <Tag>Additional text</Tag>
                </ProductListing>
            </ProductListings>";

        SetupMockIAP(contents);
        DispatchCommandResult();

    }
    public async void getAvailableProducts(string options)
    {
        ListingInformation li = await CurrentApp.LoadListingInformationAsync();
        //pls in read-only dictionary<string, ProductListing>
        var pls = li.ProductListings;
        //one string per one product
       List<string> productJsons = new List<string>();
        //making json string from ProductListing class
        foreach (ProductListing pl in pls.Values) {
            string pj = String.Format(
                @"{{ 
                    ""id"":""{0}"",
                    ""title"":""{1}"",
                    ""type"":""{2}"",
                    ""description"":""{3}"",
                    ""price"":""{4}""
                    }}",
                    pl.ProductId,
                    pl.Name,
                    pl.ProductType,
                    pl.Description,
                    pl.FormattedPrice);
            productJsons.Add(pj);
        }
        string jsonResult = String.Join(",",productJsons.ToArray());
        jsonResult = "[" + jsonResult + "]";
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, jsonResult));
    }


    public async void buy(string options)
    {
        //Getting productId from Json options
        string productId = JsonHelper.Deserialize<string[]>(options)[0];
        Deployment.Current.Dispatcher.BeginInvoke((Action)(async () => {

            try
            {
                String receiptXml = await CurrentApp.RequestProductPurchaseAsync(productId, true);
                var bytes = Encoding.UTF8.GetBytes(receiptXml);
                var base64 = Convert.ToBase64String(bytes);
                string jsonresult = String.Format("{{ \n \"base64\": \"{0}\" \n}}", base64);
                DispatchCommandResult(new PluginResult(PluginResult.Status.OK, jsonresult));

            }
            catch (Exception e)
            {
                DispatchCommandResult(new PluginResult(PluginResult.Status.ERROR, "bad error"));
            }
        
        
        }));
        
        
    }
    public void consumePurchase(string options)
    {
        //Getting productId from Json options
        string productId = JsonHelper.Deserialize<string[]>(options)[0];
        CurrentApp.ReportProductFulfillment(productId);
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, "consumed"));
    }
    public void getProductDetails(string options)
    {
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, "getProductdetails"));
    }
    public void getPurchases(string options)
    {
        LicenseInformation li = CurrentApp.LicenseInformation;
        var licenses = li.ProductLicenses;
        List<string> activeProducts = new List<string>();
        foreach(ProductLicense pl in licenses.Values) {
            if (pl.IsActive)
            {
                string plJson = String.Format(@"{{
                ""id"":""{0}"",
                ""active"":""true"",
                ""consumable"":""{1}"",
                ""expirationDate"":""{2}""
                }}",
                    pl.ProductId,
                    pl.IsConsumable,
                    pl.ExpirationDate);
                activeProducts.Add(plJson);
            }
        }
        string jsonResult = String.Join(",", activeProducts.ToArray());
        jsonResult = "[" + jsonResult + "]";
        DispatchCommandResult(new PluginResult(PluginResult.Status.OK, jsonResult));
    }
}
