### Step 1. Create a new Azure AD user for a new tenant 

This step must be performed whenever a new tenant has signed up.

When a new AD user is created, a new mailbox for the user is not created. The mailbox is created only when the appropriate license is assigned to the newly created user. So, make sure you have such licenses in your Azure plan. See [details](https://docs.microsoft.com/en-us/microsoft-365/enterprise/assign-licenses-to-user-accounts-with-microsoft-365-powershell?view=o365-worldwide).

_Note._ Azure might need some time to setup a mailbox for a new user. As for the Microsoft statement _'the setup process can take up to 4 hours to complete'_.

All in all, the algorithm is like the following:

1. Connect to Azure AD as an admin - you need permissions to create users
2. Create a user (make sure you pass the user location (such as 'US'))
3. Assign the license to the newly-created user
4. Disconnect from Azure AD

```powershell

# Connect to Azure AD as an admin
Connect-AzureAD -Credential $adminCred

# Create a user
$passwordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile
$passwordProfile.Password = <password>
New-AzureADUser -UserPrincipalName "user1@company.com" -DisplayName "User1" -PasswordProfile $passwordProfile -Mailnickname "user1" -UsageLocation "US" -AccountEnabled:$true

# Assign the license
$license = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicense
$license.SkuId = (Get-AzureADSubscribedSku | Where-Object -Property SkuPartNumber -Value "O365_BUSINESS_ESSENTIALS" -EQ).SkuID
$licensesToAssign = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicenses
$licensesToAssign.AddLicenses = $license
Set-AzureADUserLicense -ObjectId <userGuid> -AssignedLicenses $licensesToAssign

# Disconnect from Azure AD
Disconnect-AzureAD

```

There is no way to set [a user license](https://stackoverflow.com/questions/54423267/how-to-add-a-license-to-an-user-on-az-powershell) with _Az_ module of PowerShell. That's why we have to use older but more comprehensive _AzureAD_ module of PowerShell.

If you want to install the AzureAD module on Linux, you can face some difficulties. You can import in [Compatibility feature](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_windows_powershell_compatibility?view=powershell-7.2). However, this module officially isn't compatible with linux.

```powershell

PS > Import-Module AzureAD
Import-Module: Could not load file or assembly 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. The system cannot find the file specified.

# This key eliminates this error, but cmdlets might be still not visible
PS > Import-Module AzureAD -UseWindowsPowershell

```

An example is shown below.

```powershell

Install-Module AzureAD

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
Import-Module .\createAdUser.ps1

$userName = "tenant1" 
$userDomain = Get-Secret -Name TenantDomain -AsPlainText
$licensePlanName = "O365_BUSINESS_ESSENTIALS"

$newUserCreds = New-AdUser $userName $userDomain $licensePlanName

```

If you want to delete a user/mailbox with all its content you can run the snippet:

```powershell

# Connect to Azure AD as an admin
Connect-AzureAD -Credential $adminCred

# Remove the user. You can pass either a user principal name or a user's GUID
Remove-AzureADUser -ObjectId "user1@company.com"

```