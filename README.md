# Hello Coffee Terraform Project

This guide will help you set up a Terraform C# .NET 8 CDK project called `hello-coffee-terraform`, create a GitHub Actions workflow to deploy the Terraform CDK script to Azure, and update the Terraform CDK resources.

## Prerequisites

- .NET 8 SDK
- Terraform CLI (preferably v1.9.4 or up)
- [CDK for Terraform (CDKTF)](https://developer.hashicorp.com/terraform/cdktf)
- Azure CLI
- GitHub CLI

## Setting Up the Project

1. **Create a new directory for your project:**

    ```bash
    mkdir hello-coffee-terraform
    cd hello-coffee-terraform
    ```

2. **Initialize a new CDK for Terraform project:**

    ```bash
    cdktf init --template="csharp"
    ```

3. **Install necessary packages:**

    ```bash
    dotnet add package HashiCorp.Cdktf
    dotnet add package HashiCorp.Cdktf.Providers.Azurerm
    ```

4. **Create your main application file:**

    Create a file named `Main.cs` and add the following code:

    ```csharp
    using HashiCorp.Cdktf;
    using Constructs;

    namespace HelloCoffeeTerraform
    {
        class Program
        {
            static void Main(string[] args)
            {
                var app = new App();
                new MyStack(app, "hello-coffee-terraform");
                app.Synth();
            }
        }

        public class MyStack : TerraformStack
        {
            public MyStack(Construct scope, string id) : base(scope, id)
            {
                // Define your resources here
            }
        }
    }
    ```

## Creating a GitHub Actions Workflow

1. **Create a `.github/workflows` directory:**

    ```bash
    mkdir -p .github/workflows
    ```

2. **Create a workflow file named `deploy.yml`:**

    ```yaml
    name: Deploy to Azure

    on:
      push:
        branches:
          - main

    jobs:
      deploy:
        runs-on: ubuntu-latest

        steps:
        - name: Checkout code
          uses: actions/checkout@v2

        - name: Set up .NET
          uses: actions/setup-dotnet@v1
          with:
            dotnet-version: '8.0.x'

        - name: Install CDKTF
          run: npm install -g cdktf-cli

        - name: Install dependencies
          run: dotnet restore

        - name: Authenticate to Azure
          uses: azure/login@v1
          with:
            creds: ${{ secrets.AZURE_CREDENTIALS }}

        - name: Deploy with CDKTF
          run: |
            cdktf get
            cdktf deploy --auto-approve
    ```
The last action is the standard approach, I did it slightly differently.   

Note: For `AZURE_CREDENTIALS` can find out how to create it [here](https://success.skyhighsecurity.com/Skyhigh_CASB/Skyhigh_CASB_Sanctioned_Apps/Skyhigh_CASB_for_Office_365/Service_Principal_with_a_Secret_Key_and_Azure_API_Integration#:~:text=An%20Azure%20Service%20Principal%20is,and%20use%20it%20to%20authenticate.).

```json
{
   "clientId": "<Found in your Azure App page on the Azure Portal>",
   "clientSecret": "<Found in your Azure App page client credentials section>",
   "subscriptionId": "<Found: https://portal.azure.com/#view/Microsoft_Azure_Billing/SubscriptionsBladeV2>",
   "tenantId": "<Found in your Azure App page on the Azure Portal>"
}
```

![Azure credentials parts.png](images%2FAzure%20credentials%20parts.png)

Then added to:

![Azure credentials in github secrets.png](images%2FAzure%20credentials%20in%20github%20secrets.png)

## Updating Terraform CDK Resources

To update a Terraform CDK resource, you can use the `TerraformResource.ImportFrom` method. Here's an example:

```csharp
using HashiCorp.Cdktf;
using Constructs;

namespace HelloCoffeeTerraform
{
    public class MyStack : TerraformStack
    {
        public MyStack(Construct scope, string id) : base(scope, id)
        {
            // Example of importing an existing resource
            const string existingResourceGroupId = "/subscriptions/<subscriptionId>/resourceGroups/helloCoffeeResourceGroup";

            var resourceGroup = new ResourceGroup(this, "resourceGroup", new ResourceGroupConfig
            {
                Location = "West Europe",
                Name = "helloCoffeeResourceGroup"
            });
            resourceGroup.ImportFrom(existingResourceGroupId);
        }
    }
}
```
Result:

![updating terraform now easy.png](images%2Fupdating%20terraform%20now%20easy.png)
