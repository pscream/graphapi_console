. .\common.ps1

function New-AdUser {
    [OutputType([Management.Automation.PsCredential])]

    Param (
        [Parameter(Mandatory)]
        [string] $userName,
        [Parameter(Mandatory)]
        [string] $userDomain,
        [Parameter(Mandatory)]
        [string] $licencePlanName
    )

    $DebugPreference = "Continue"

    $userPrincipalName = "${userName}@${userDomain}";
    $userDisplayName = $userName.Substring(0,1).ToUpper() + $userName.Substring(1).ToLower()   

    Import-Module AzureAD

    # Connect to Azure AD
    Connect-Ad
    
    # Create a new user for a tenant
    $passwordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile
    $passwordProfile.Password = Get-UserPassword(16)
    $passwordProfile.ForceChangePasswordNextLogin = $false 
    $params = @{
        "UserPrincipalName"     = $userPrincipalName
        "DisplayName"           = $userDisplayName
        "PasswordProfile"       = $passwordProfile
        "Mailnickname"          = $userName
        "UsageLocation"         = "US"
        "AccountEnabled"        = $true
        "PasswordPolicies"      = "DisablePasswordExpiration"
    }

    Write-Debug "Principal User Name:$userPrincipalName, Password: $($passwordProfile.Password)"

    $createdUser = New-AzureADUser @params
    Write-Debug $createdUser

    # Add a license to access the mailbox
    $userId=$createdUser.ObjectId
    $license = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicense
    $license.SkuId = (Get-AzureADSubscribedSku | Where-Object -Property SkuPartNumber -Value $licencePlanName -EQ).SkuID
    $licensesToAssign = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicenses
    $licensesToAssign.AddLicenses = $license
    Set-AzureADUserLicense -ObjectId $userId -AssignedLicenses $licensesToAssign
    
    Disconnect-Ad

    $passwordSecure = ConvertTo-SecureString -AsPlainText -Force -String $passwordProfile.Password

    return New-Object -TypeName Management.Automation.PsCredential -ArgumentList ($userPrincipalName, $passwordSecure)

}