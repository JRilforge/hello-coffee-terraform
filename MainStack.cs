// https://github.com/hashicorp/terraform-cdk-action
// AZURE_CREDENTIALS - https://success.skyhighsecurity.com/Skyhigh_CASB/Skyhigh_CASB_Sanctioned_Apps/Skyhigh_CASB_for_Office_365/Service_Principal_with_a_Secret_Key_and_Azure_API_Integration#:~:text=An%20Azure%20Service%20Principal%20is,and%20use%20it%20to%20authenticate.
// create resource group - https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal

using HashiCorp.Cdktf.Providers.Azurerm.Provider;

namespace MyTerraformStack;

using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Azurerm;
using HashiCorp.Cdktf.Providers.Azurerm.CosmosdbAccount;
using HashiCorp.Cdktf.Providers.Azurerm.AppService;
using HashiCorp.Cdktf.Providers.Azurerm.AppServicePlan;
using HashiCorp.Cdktf.Providers.Azurerm.CosmosdbSqlDatabase;
using HashiCorp.Cdktf.Providers.Azurerm.ResourceGroup;
using HashiCorp.Cdktf.Providers.Azurerm.AppServiceSlot;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

public class MainStack : TerraformStack
{
    public MainStack(Construct scope, string id) : base(scope, id)
    {
        var azurermProvider = new AzurermProvider(this, "AzureRM", new AzurermProviderConfig
        {
            Features = new AzurermProviderFeatures()
        });

        var playwrightUserPassword = new TerraformVariable(this, "playwrightUserPassword", new TerraformVariableConfig
        {
            Type = "string",
            Description = "Playwright Test User Password",
            Sensitive = true,
        });

        var resourceGroup = new ResourceGroup(this, "resourceGroup", new ResourceGroupConfig
        {
            Location = "West Europe",
            Name = "helloCoffeeResourceGroup"
        });

        // Cosmos DB
        var cosmosDbAccount = new CosmosdbAccount(this, "CosmosDbAccount", new CosmosdbAccountConfig
        {
            Name = "hellocoffeedb",
            Location = "Central US",
            ResourceGroupName = resourceGroup.Name,
            OfferType = "Standard",
            Kind = "GlobalDocumentDB",

            ConsistencyPolicy = new CosmosdbAccountConsistencyPolicy
            {
                ConsistencyLevel = "BoundedStaleness",
                MaxIntervalInSeconds = 300,
                MaxStalenessPrefix = 100000
            },
            GeoLocation = new[]
            {
                new CosmosdbAccountGeoLocation
                {
                    Location = "Central US",
                    FailoverPriority = 0
                }
            }
        });

        var cosmosDbSqlDatabase = new CosmosdbSqlDatabase(this, "cosmosDbSqlDatabase", new CosmosdbSqlDatabaseConfig
        {
            Name = "HelloCoffeeDb",
            ResourceGroupName = resourceGroup.Name,
            AccountName = cosmosDbAccount.Name
        });

        // App Service Plan for Web App
        var webAppServicePlan = new AppServicePlan(this, "WebAppServicePlan", new AppServicePlanConfig
        {
            Name = "webAppServicePlan",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Sku = new AppServicePlanSku
            {
                Tier = "Standard",
                Size = "S1"
            }
        });

        // Web App
        var webApp = new AppService(this, "WebApp", new AppServiceConfig
        {
            Name = "HelloCoffeeWebApp",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = webAppServicePlan.Id,
            DependsOn = new[] { cosmosDbSqlDatabase },
            SiteConfig = new AppServiceSiteConfig
            {
                DotnetFrameworkVersion = "v8.0"
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name },
                { "PLAYWRIGHT_USER_PASSWORD", playwrightUserPassword.StringValue }
            }
        });

        // App Service Plan for API App
        var apiAppServicePlan = new AppServicePlan(this, "ApiAppServicePlan", new AppServicePlanConfig
        {
            Name = "apiAppServicePlan",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Sku = new AppServicePlanSku
            {
                Tier = "Standard",
                Size = "S1"
            }
        });

        // API App
        var apiApp = new AppService(this, "ApiApp", new AppServiceConfig
        {
            Name = "HelloCoffeeWebApi",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = apiAppServicePlan.Id,
            DependsOn = new[] { cosmosDbSqlDatabase },
            SiteConfig = new AppServiceSiteConfig
            {
                DotnetFrameworkVersion = "v8.0"
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name }
            }
        });

        // Add deployment slot for web api
        var webAppDeploymentSlot = new AppServiceSlot(this, "webAppDeploymentSlot", new AppServiceSlotConfig
        {
            Name = "production",
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = webAppServicePlan.Id,
            AppServiceName = webApp.Name,
            Location = resourceGroup.Location,
            SiteConfig = new AppServiceSlotSiteConfig
            {
                DotnetFrameworkVersion = "v8.0"
            }
        });

        // Add deployment slot for web app
        var webApiDeploymentSlot = new AppServiceSlot(this, "webAppDeploymentSlot", new AppServiceSlotConfig
        {
            Name = "production",
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = apiAppServicePlan.Id,
            AppServiceName = apiApp.Name,
            Location = resourceGroup.Location,
            SiteConfig = new AppServiceSlotSiteConfig
            {
                DotnetFrameworkVersion = "v8.0"
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
