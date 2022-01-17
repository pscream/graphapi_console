### Step 2. Create an alias and setup a corresponding mailbox folder

__Note.__ Before performing this step, it might be needed to wait for some time until Exchange Online has completed all required operations over a newly-create user. Microsoft statement says _'the setup process can take up to 4 hours to complete'_

This step may repeat many times, whenever we need a new alias for a new email connector.

Because Exchange Online doesn't expose mailbox aliases through any of its API. Even if 'internetMessageHeaders' are requested, it still withhold the raw 'To' header content).  Normal message type _'Microsoft.Graph.Message'_ in its _'ToRecipients'_ collection keeps the primary address of the recipient instead of an alias used by a sender. It's a [limitation/bug/design](https://stackoverflow.com/questions/60292284/microsoft-graph-how-to-get-the-alias-from-an-email-message) widely discussed. The most probable explanation is in the following quote:

> Microsoft Exchange Server, Office 365 and Outlook.com (formerly Hotmail) do not preserve (or at least expose) the SMTP RCPT TO value (i.e. the envelope address) - which is distinct from the To:, Cc:, and Bcc: headers (note that the Bcc header isn't usually sent at all to the recipient anyway).
> Note that anti-spam software will often flag emails when the RCPT TO value doesn't appear in any of the To:, Cc:, or Bcc: headers as in the late-1990s/early-200s mass-mailed spam didn't have personalised To: headers.

The only reasonable workaround to distinguish messages sent for the same mailbox but for different aliases is to apply incoming mail rules that move messages to corresponding folders depending on alias occurrence in message headers.

For example, if on of the message header contains the alias with the 'user1-alias1@company.com' address, it's moved to the 'user1-alias1' mailbox folder.

The most obvious way to create mailbox folders, mailbox rules and mail aliases is to create them with the user credential the given mailbox belongs to. While first two operation can be performed by a normal user over a user's mailbox (and isn't possible for anyone else, unless the owner of the mailbox), the last operation can be performed by the admin user only, however. 

Actually, mail aliases can be added by a user who has Admin rights. 
See [Microsoft explanation](https://docs.microsoft.com/en-us/microsoft-365/admin/email/add-another-email-alias-for-a-user?view=o365-worldwide):

> This article is for Microsoft 365 administrators who have business subscriptions. It's not for home users.
> ...
> You must have Global Admin rights to add email aliases to a user.

Thus, on the one hand, it's impossible to add aliases to you own mailbox if you aren't an admin, and, on the other hand, the admin cannot create mailbox folders and rules. See [details](https://docs.microsoft.com/en-us/powershell/module/exchange/new-mailboxfolder?view=exchange-ps).

> Use the New-MailboxFolder cmdlet to create folders in your own mailbox. Administrators can't use this cmdlet to create folders in other mailboxes.

If you try to connect to Exchange Online with a normal user, you can run the following to add an alias and you get an error:

```powershell

PS > Set-Mailbox -Identity "user1@company.com" -EmailAddresses @{add="user1-alias1@company.com"}
A parameter cannot be found that matches parameter name 'EmailAddresses'.
    + CategoryInfo          : InvalidArgument: (:) [Set-Mailbox], ParameterBindingException
    + FullyQualifiedErrorId : NamedParameterNotFound,Set-Mailbox
    + PSComputerName        : outlook.office365.com

```

_Note._ You can't create aliases (aka _'proxyAddresses'_) with Graph API. It's a readonly field. Searching in the Internet gives [this conversation](https://stackoverflow.com/questions/41961856/updating-proxyaddresses-using-microsoft-graph-api/42035071#42035071) and here is the quote from it made by one of Microsoft developers:

> There is no way to set email address on User currently through Microsoft Graph API. We are currently investigating adding the needed support, but there is no ETA.
> Feb 4 '17 at 0:19

Likewise, you have to switch your connection to a mailbox owner to create a new folder. Otherwise, you get an error as well. Don't be confused with the error text _'The specified mailbox Identity doesn't exist'_. It magically become available when you connect as an owner of the mailbox and run the same command.

```powershell

PS > New-MailboxFolder -Name "user1-alias1" -Parent "user1@company.com"
The specified mailbox Identity: 'user1@company.com' does not exist.
    + CategoryInfo          : NotSpecified: (:) [New-MailboxFolder], ManagementObjectNotFoundException
    + FullyQualifiedErrorId : [] [FailureCategory=Cmdlet-ManagementObjectNotFoundException] D228C798,Microsoft.Exchange.Management.StoreTasks.NewMailboxFolder
    + PSComputerName        : outlook.office365.com

```

All in all, the algorithm is like the following:

1. Connect to Exchange Online as an admin
2. Create a new alias for a new email connector
3. Disconnect from the admin session
4. Connect to Exchange Online as a mailbox owner
5. Create a new mailbox folder with the same name as a new email connector
6. Create a rule to move messages received for the new alias to the newly-created mailbox folder 
7. Disconnect from the mailbox owner session

```powershell

# Connect to Exchange Online as an admin
Connect-ExchangeOnline -Credential $adminCred

# Create a new alias for a new email connector
Set-Mailbox -Identity "user1@company.com" -EmailAddresses @{add="user1-alias1@company.com"}

# Disconnect from the admin session
Disconnect-ExchangeOnline

# Connect to Exchange Online as a mailbox owner
Connect-ExchangeOnline -Credential $ownerCred

# Create a new mailbox folder with the same name as a new email connector
New-MailboxFolder -Name "user1-alias1" -Parent "user1@company.com"

# Create a rule to move messages received for the new alias to the newly created mailbox folder
New-InboxRule -Name "user1-alias1" -HeaderContainsWords "user1-alias1@company.com" -MoveToFolder ":\user1-alias1"

# Disconnect from the mailbox owner session
Disconnect-ExchangeOnline

```

If you don't have SSL infrastructure installed you might get an error when call 'Connect-ExchangeOnline':

```powershell

Connect-ExchangeOnline -Credential $adminCred
pwsh: symbol lookup error: /opt/microsoft/powershell/7/libmi.so: undefined symbol: SSL_library_init

```

In this case, you should run the next shippet and restart _'pwsh'_:

```powershell

Install-Module PSWSMan
Import-Module PSWSMan
Install-WSMan
WARNING: WSMan libs have been installed, please restart your PowerShell session to enable it in PowerShell

```

An example is shown below.

```powershell

Install-Module ExchangeOnlineManagement

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
Import-Module .\createAlias.ps1

$userName = "tenant1" 
$userDomain = Get-Secret -Name TenantDomain -AsPlainText
$aliasName = "tenant1-alias1" 

New-MailBoxAlias $userName $aliasName $userDomain $newUserCreds

```