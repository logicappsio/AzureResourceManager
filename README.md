# Azure Resource Manager Connector
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploying ##
Click the "Deploy to Azure" button above.  You can create new resources or reference existing ones (resource group, gateway, service plan, etc.)  **Site Name and Gateway must be unique URL hostnames.**  The deployment script will deploy the following:
 * Resource Group (optional)
 * Service Plan (if you don't reference exisiting one)
 * Gateway (if you don't reference existing one)
 * API App
 * API App Host (this is the site behind the api app that this github code deploys to)

Before you can start using the Connector, you need to set up a Service Principal with permissions to do whatever it is you want to do in Azure. All calls will be made on-behalf-of this Service Principal that you set up.
    [David Ebbo has written a great blog post on how to set this up](http://blog.davidebbo.com/2014/12/azure-service-principal.html). Please follow all the instructions there and get your Tenant ID, Client ID and Secret. 