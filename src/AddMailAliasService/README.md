### Dockerized service to create/remove Azure AD users and aliases. It is triggered by Azure Service Bus messages.

Before you start the application in a docker container you have to set environmental variables. It's convenient to create a file with environmental variable values and then pass this file as a parameter when _docker_ spin up a container with the [_--env-file_ parameter](https://docs.docker.com/engine/reference/run/#env-environment-variables).
Note the doubled underscore '__' instead of a colon ':'. There mustn't be spaces between values and either '>' or '>>' characters. 
```

echo AdminUPN=***> env.local.txt
echo AdminPWD=***>> env.local.txt
echo TenantDomain=***>> env.local.txt
echo ServiceBus__ConnectionString=Endpoint=sb://**********/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=**********>> env.local.txt
echo <other variable> >> env.local.txt

docker build -t add-mail-alias-service .
docker run -it --env-file env.local.txt --name add-mail-alias-service add-mail-alias-service

# To remove the container
docker container rm add-mail-alias-service

```

Due to debug reasons you might want to start an application in a normal way. In this case it's more convenient to store sensitive data in local secrets.

```

cd .\src\AddMailAliasService\ 
dotnet user-secrets init
dotnet user-secrets set "AdminUPN" "***"
dotnet user-secrets set "AdminPWD" "***"
dotnet user-secrets set "TenantDomain" "***"
dotnet user-secrets set "ServiceBus:ConnectionString" "***"

```

We need somehow send messages to _Azure Service Bus_. [_ServiceBusExplorer_](https://github.com/paolosalvatori/ServiceBusExplorer) (version 5.0.4). Just download it, extract from the package and run.

Here are examples of JSON message payload.

In order to create a new tenant user:

```json

{
    "Type" : 100,
    "TenantDisplayName" : "Tenant1",
    "TenantPrincipalName" : "tenant1@app.company.com",
    "TenantPassword" : "********"   
}

```

In order to remove the tenant user:

```json

{
    "Type" : 150,
    "TenantPrincipalName" : "tenant1@app.company.com"
}

```

In order to create a new email alias for the tenant user:

```json

{
    "Type" : 200,
    "NewAliasName" : "tenant1-alias1",
    "TenantPrincipalName" : "tenant1@app.company.com",
    "TenantPassword" : "********"   
}

```

In order to rename the alias for the tenant user:

```json

{
    "Type" : 210,
    "CurrentAliasName" : "tenant1-alias1",
	"NewAliasName" : "tenant1-alias2",
    "TenantPrincipalName" : "tenant1@app.company.com",
    "TenantPassword" : "********"   
}

```

In order to remove the tenant user:

```json

{
    "Type" : 250,
    "CurrentAliasName" : "tenant1-alias1",
    "TenantPrincipalName" : "tenant1@app.company.com",
    "TenantPassword" : "********"   
}

```




Don't be precautious about using the 'AzureAD.Standard.Preview' module. It's a part of _Azure Cloud Console_ environment. 
If you run it from PowerShell console you might need to set the execution policy. Otherwise, you can get an error _'AzureAD.Standard.Preview is not digitally signed. You cannot run this script on the current system'_.

```powershell

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Unrestricted -Force

```

Although the documentation of _AzureAD.Standard.Preview_ implies an authentication with a username and a password, due to unclear reasons it works fine under _Windows_ only. Under linux it returns an error _'Connect-AzureAD: password_required_for_managed_user: Password is required for managed user'_ even if a password is correctly passed. in this case you have to connect in the way below:

```powershell

# Use Az.Accounts and connect with the normal credentials.
Connect-AzAccount -Credential $psCred

# Get a token for Azure AD
$token = Get-AzAccessToken -ResourceTypeName AadGraph

# Use the retrieved token
Connect-AzureAD -AadAccessToken  $token.Token -AccountId <***>

```

__Note.__ When you connect to Exchange Online PowerShell downloads online scripts with cmdlets that are not available otherwise. It takes surprisingly long when run in a container under _Docker Desktop_. Don't be surprised if it takes up to 2 mins. 