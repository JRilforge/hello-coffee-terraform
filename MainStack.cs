// https://github.com/hashicorp/terraform-cdk-action

using HashiCorp.Cdktf.Providers.Azurerm.Provider;

namespace MyTerraformStack;

using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Azurerm;
using HashiCorp.Cdktf.Providers.Azurerm.CosmosdbAccount;
using HashiCorp.Cdktf.Providers.Azurerm.AppService;
using HashiCorp.Cdktf.Providers.Azurerm.AppServicePlan;

public class MainStack : TerraformStack
{
    public MainStack(Construct scope, string id) : base(scope, id)
    {
        var azurermProvider = new AzurermProvider(this, "AzureRM", new AzurermProviderConfig
        {
            Features = new AzurermProviderFeatures()
        });

        // Cosmos DB
        var cosmosDbAccount = new CosmosdbAccount(this, "CosmosDbAccount", new CosmosdbAccountConfig
        {
            Name = "mycosmosdbaccount",
            Location = "West Europe",
            ResourceGroupName = "myResourceGroup",
            OfferType = "Standard",
            Kind = "GlobalDocumentDB",
            ConsistencyPolicy = new CosmosdbAccountConsistencyPolicy
            {
                ConsistencyLevel = "Session"
            },
            GeoLocation = new[]
            {
                new CosmosdbAccountGeoLocation
                {
                    Location = "West Europe",
                    FailoverPriority = 0
                }
            }
        });

        // App Service Plan for Web App
        var webAppServicePlan = new AppServicePlan(this, "WebAppServicePlan", new AppServicePlanConfig
        {
            Name = "webAppServicePlan",
            Location = "West Europe",
            ResourceGroupName = "myResourceGroup",
            Sku = new AppServicePlanSku
            {
                Tier = "Standard",
                Size = "S1"
            }
        });

        // Web App
        var webApp = new AppService(this, "WebApp", new AppServiceConfig
        {
            Name = "myWebApp",
            Location = "West Europe",
            ResourceGroupName = "myResourceGroup",
            AppServicePlanId = webAppServicePlan.Id,
            SiteConfig = new AppServiceSiteConfig
            {
                DotnetFrameworkVersion = "v6.0"
            }
        });

        // App Service Plan for API App
        var apiAppServicePlan = new AppServicePlan(this, "ApiAppServicePlan", new AppServicePlanConfig
        {
            Name = "apiAppServicePlan",
            Location = "West Europe",
            ResourceGroupName = "myResourceGroup",
            Sku = new AppServicePlanSku
            {
                Tier = "Standard",
                Size = "S1"
            }
        });

        // API App
        var apiApp = new AppService(this, "ApiApp", new AppServiceConfig
        {
            Name = "myApiApp",
            Location = "West Europe",
            ResourceGroupName = "myResourceGroup",
            AppServicePlanId = apiAppServicePlan.Id,
            SiteConfig = new AppServiceSiteConfig
            {
                DotnetFrameworkVersion = "v6.0"
            }
        });

        // Output the Web App URL
        new TerraformOutput(this, "WebAppUrl", new TerraformOutputConfig
        {
            Value = webApp.DefaultSiteHostname
        });

        // Output the API App URL
        new TerraformOutput(this, "ApiAppUrl", new TerraformOutputConfig
        {
            Value = apiApp.DefaultSiteHostname
        });
    }
}
