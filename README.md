# graphapi_console

We need to store the sensitive data (aka secrets) for PowerShell scripts somewhere
By default it's the 'c:\Users\&lt;username&gt;\AppData\Local\Microsoft\PowerShell\secretmanagement ' folder
```
# First install modules
Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
Install-Module Microsoft.PowerShell.SecretManagement -Scope CurrentUser -SkipPublisherCheck -Confirm:$false
Install-Module Microsoft.PowerShell.SecretStore -Scope CurrentUser -SkipPublisherCheck -Confirm:$false

# Register a secret vault
Register-SecretVault -Name "AzureMailTrial" -ModuleName Microsoft.PowerShell.SecretStore
# We don't need a password for local development, but if you run the following command for the first time you might be asked for a new password
Set-SecretStoreConfiguration -Authentication None -Interaction None 

# Set secrets
Set-Secret -Name AdminUPN -Secret "***"
Set-Secret -Name AdminPWD -Secret "***"

# Check secrets
Get-SecretInfo
$adminUpn = Get-Secret -Name AdminUPN -AsPlainText
$adminPwd = Get-Secret -Name AdminPWD -AsPlainText

```

If we are going to use Microsoft Graph API wrappers from the C# app we have to create [a secret store].(https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows)
The following commands add UserSecretsId element to the csproj file and create secrets in 'c:\Users\&lt;username&gt;\AppData\Roaming\Microsoft\UserSecrets '. 
Of course, before you run those commands you have to get secrets from Azure Portal when you register your app there in Azure AD.

```

cd .\src\CreateUser\ 
dotnet user-secrets init
dotnet user-secrets set "ClientId" "***"
dotnet user-secrets set "TenantId" "***"
dotnet user-secrets set "ClientSecret" "***"
dotnet user-secrets set "MailboxName" "***"

```

Possible candidates to implement this feature are listed in the next table

| Name  | Windows | Linux | Used features | Remark |
|-------|-----|---|---|---|
| [MSOnline](https://docs.microsoft.com/en-us/powershell/azure/active-directory/install-msonlinev1?view=azureadps-1.0) | Powershell | n/a  | New-MsolUser | Deprecated |
| [AzureAD](https://docs.microsoft.com/en-us/powershell/azure/active-directory/overview?view=azureadps-2.0) + [ExchangeOnlineManagement](https://docs.microsoft.com/en-us/powershell/exchange/exchange-online-powershell-v2?view=exchange-ps) | Powershell | n/a | New-AzureADUser, Set-AzureADUserLicense, Set-Mailbox, New-MailboxFolder, New-InboxRule | AzureAD works on Windows only  |
| [Az](https://docs.microsoft.com/en-us/powershell/azure/what-is-azure-powershell?view=azps-7.1.0): Az.Accounts, Az.Resources + [ExchangeOnlineManagement](https://docs.microsoft.com/en-us/powershell/exchange/exchange-online-powershell-v2?view=exchange-ps)| Powershell | Powershell | New-AzADUser |  No way to assign [the license](https://stackoverflow.com/questions/54423267/how-to-add-a-license-to-an-user-on-az-powershell) |
| [Microsoft Graph API](https://docs.microsoft.com/en-us/graph/sdks/create-requests?tabs=CS) | C# | C# |   |  No way to add an alias |

One can play with Microsoft Graph API (https://developer.microsoft.com/en-us/graph/graph-explorer)

Take a look at the general idea of [the design](doc/mail-processing.md). 

Below is how to get the environment ready to use this repository and some important command to note.

Install .Net Runtime if not installed
```

Short way (if all prerequisites are installed)
$ sudo apt-get install -y dotnet-runtime-6.0

Long way (with all prerequisites)
$ wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ rm packages-microsoft-prod.deb

$ sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-runtime-6.0

```

Install PowerShell on Ubuntu (https://docs.microsoft.com/en-us/powershell/scripting/install/install-ubuntu?view=powershell-7.2)

```
# Make sure you have .Net Runtime
$ dotnet --list-runtimes

# Initially update the list of packages (normally you don't have any 'https://*.microsoft.com' repos)
$ sudo apt-get update

# Install pre-requisite packages.
$ sudo apt-get install -y wget apt-transport-https software-properties-common

# Download the Microsoft repository GPG keys (add -q if you want it to be silent)
$ wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb

# Register the Microsoft repository GPG keys
$ sudo dpkg -i packages-microsoft-prod.deb

# Re-update the list of packages after we added packages.microsoft.com (now you should have 'https://*.microsoft.com' repos)
$ sudo apt-get update

# Install PowerShell
$ sudo apt-get install -y powershell

# Run PowerShell and make sure it works
$ pwsh
PowerShell 7.2.1
Copyright (c) Microsoft Corporation.

https://aka.ms/powershell
Type 'help' to get help.

PS >
```