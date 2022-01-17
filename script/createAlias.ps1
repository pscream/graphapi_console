. .\common.ps1

function New-MailBoxAlias {
    param (
        [Parameter(Mandatory)]
        [string] $userName,
        [Parameter(Mandatory)]
        [string] $aliasName,
        [Parameter(Mandatory)]
        [string] $userDomain,
        [Parameter(Mandatory)]
        [Management.Automation.PsCredential] $userCreds
    )

    $userPrincipalName = "${userName}@${userDomain}";

    $aliasFullName = "${aliasName}@${userDomain}";

    Connect-Exchange 

    Set-Mailbox -Identity $userPrincipalName -EmailAddresses @{add = $aliasFullName}

    Disconnect-Exchange 

    Connect-Exchange $userCreds

    New-MailboxFolder -Name $aliasName -Parent $userPrincipalName
    New-InboxRule -Name $aliasName -HeaderContainsWords $aliasFullName -MoveToFolder ":\$aliasName"

    Disconnect-Exchange 

}