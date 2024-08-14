// https://github.com/hashicorp/terraform-cdk-action
// AZURE_CREDENTIALS - https://success.skyhighsecurity.com/Skyhigh_CASB/Skyhigh_CASB_Sanctioned_Apps/Skyhigh_CASB_for_Office_365/Service_Principal_with_a_Secret_Key_and_Azure_API_Integration#:~:text=An%20Azure%20Service%20Principal%20is,and%20use%20it%20to%20authenticate.
// create resource group - https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/manage-resource-groups-portal

using HashiCorp.Cdktf.Providers.Azurerm.LinuxWebAppSlot;
using HashiCorp.Cdktf.Providers.Azurerm.Provider;
using HashiCorp.Cdktf.Providers.Azurerm.ServicePlan;

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
using HashiCorp.Cdktf.Providers.Azurerm.LinuxWebApp;

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

        const string resourceGroupId = "/subscriptions/50f420bb-8ac6-4659-a9d0-ad43633bd961/resourceGroups/helloCoffeeResourceGroup";

        var resourceGroup = new ResourceGroup(this, "resourceGroup", new ResourceGroupConfig
        {
            Location = "West Europe",
            Name = "helloCoffeeResourceGroup"
        }).WithId(resourceGroupId);

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
        }).WithId($"{resourceGroupId}/providers/Microsoft.DocumentDB/databaseAccounts/hellocoffeedb");

        var cosmosDbSqlDatabase = new CosmosdbSqlDatabase(this, "cosmosDbSqlDatabase", new CosmosdbSqlDatabaseConfig
        {
            Name = "HelloCoffeeDb",
            ResourceGroupName = resourceGroup.Name,
            AccountName = cosmosDbAccount.Name
        }).WithId($"{resourceGroupId}/providers/Microsoft.DocumentDB/databaseAccounts/hellocoffeedb/sqlDatabases/HelloCoffeeDb");

        // Service Plan for API App
        var apiAppServicePlan = new ServicePlan(this, "ApiAppServicePlan", new ServicePlanConfig
        {
            Name = "apiAppServicePlan",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            OsType = "Linux",
            SkuName = "S1"
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/serverFarms/apiAppServicePlan");

        // API App
        var apiApp = new LinuxWebApp(this, "ApiApp", new LinuxWebAppConfig
        {
            Name = "HelloCoffeeWebApi",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            ServicePlanId = apiAppServicePlan.Id,
            DependsOn = new[] { cosmosDbSqlDatabase },
            SiteConfig = new LinuxWebAppSiteConfig
            {
                ApplicationStack = new LinuxWebAppSiteConfigApplicationStack()
                {
                    DotnetVersion = "8.0"
                }
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name }
            }
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/sites/HelloCoffeeWebApi");

        // Add deployment slot for web app
        
        var webApiDeploymentSlot = new LinuxWebAppSlot(this, "webApiDeploymentSlot", new LinuxWebAppSlotConfig
        {
            Name = "staging",
            AppServiceId = apiApp.Id,
            SiteConfig = new LinuxWebAppSlotSiteConfig
            {
                ApplicationStack = new LinuxWebAppSlotSiteConfigApplicationStack()
                {
                    DotnetVersion = "8.0"
                }
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name }
            }
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/sites/HelloCoffeeWebApi/slots/staging");

        // Service Plan for Web App
        var webAppServicePlan = new ServicePlan(this, "WebAppServicePlan", new ServicePlanConfig
        {
            Name = "webAppServicePlan",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            OsType = "Linux",
            SkuName = "S1"
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/serverFarms/webAppServicePlan");

        // Web App
        var webApp = new LinuxWebApp(this, "WebApp", new LinuxWebAppConfig
        {
            Name = "HelloCoffeeWebApp",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            ServicePlanId = apiAppServicePlan.Id,
            DependsOn = new[] { cosmosDbSqlDatabase },
            SiteConfig = new LinuxWebAppSiteConfig
            {
                ApplicationStack = new LinuxWebAppSiteConfigApplicationStack()
                {
                    DotnetVersion = "8.0"
                }
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name },
                { "PLAYWRIGHT_USER_PASSWORD", playwrightUserPassword.StringValue },
                { "HELLO_COFFEE_API_HOST", apiApp.DefaultHostname }
            }
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/sites/HelloCoffeeWebApp");

        // Add deployment slot for web api
        var webAppDeploymentSlot = new LinuxWebAppSlot(this, "webAppDeploymentSlot", new LinuxWebAppSlotConfig
        {
            Name = "staging",
            AppServiceId = webApp.Id,
            SiteConfig = new LinuxWebAppSlotSiteConfig
            {
                ApplicationStack = new LinuxWebAppSlotSiteConfigApplicationStack()
                {
                    DotnetVersion = "8.0"
                }
            },
            AppSettings = new Dictionary<string, string>
            {
                { "COSMOS_ENDPOINT", cosmosDbAccount.Endpoint },
                { "COSMOS_KEY", cosmosDbAccount.PrimaryKey },
                { "COSMOS_DB", cosmosDbSqlDatabase.Name },
                { "PLAYWRIGHT_USER_PASSWORD", playwrightUserPassword.StringValue },
                { "HELLO_COFFEE_API_HOST", apiApp.DefaultHostname }
            }
        }).WithId($"{resourceGroupId}/providers/Microsoft.Web/sites/HelloCoffeeWebApp/slots/staging");


        // Output the Web App URL
        new TerraformOutput(this, "WebAppUrl", new TerraformOutputConfig
        {
            Value = webApp.DefaultHostname
        });

        // Output the API App URL
        new TerraformOutput(this, "ApiAppUrl", new TerraformOutputConfig
        {
            Value = apiApp.DefaultHostname
        });
    }
}

public static class TerraformResourceExtendedMethods {
    public static T WithId<T>(this T source, string resourceId) where T : TerraformResource {
        source.ImportFrom(resourceId);
        return source;
    }
}
