using System.Management.Automation;

using Microsoft.Extensions.Logging;


namespace AddMailAliasService.Operations
{
    internal class AliasOperation : OperationBase
    {

        private readonly string _tenantDomain = string.Empty;

        public AliasOperation(ILogger<AliasOperation> logger, string adminUsername, string adminPassword, string tenantDomain)
            : base(logger, adminUsername, adminPassword)
        {
            _tenantDomain = tenantDomain;
        }

        protected override PowerShell ImportModules(PowerShell ps)
        {

            if (ps == null)
                ps = PowerShell.Create();

            ps.AddScript("Import-Module ExchangeOnlineManagement");
            ps = InvokeAndClear(ps);

            ps.AddScript("Write-Information $(Get-Module -Verbose | out-string)");
            ps = InvokeAndClear(ps);

            return ps;

        }

        public PowerShell ConnectExchangeAsAdmin(PowerShell ps)
        {
            return ConnectExchange(ps, _adminUsername, _adminPassword);
        }

        public PowerShell ConnectExchange(PowerShell ps, string username, string password)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps.AddScript(string.Format("$username = \"{0}\"", username));
            ps.AddScript(string.Format("$password = ConvertTo-SecureString \"{0}\" -AsPlainText -Force", password));
            ps.AddScript("$psCred = New-Object System.Management.Automation.PSCredential -ArgumentList ($username, $password)");
            ps.AddScript("Connect-ExchangeOnline -Credential $psCred");
            ps = InvokeAndClear(ps);

            return ps;
        }

        public PowerShell DisconnectExchange(PowerShell ps)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps.AddScript("Disconnect-ExchangeOnline -Confirm:$false -InformationAction Ignore -ErrorAction SilentlyContinue");
            ps = InvokeAndClear(ps);

            return ps;
        }

        public PowerShell CreateMailAlias(PowerShell ps, string username, string password, string aliasName)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps = PrepareEnvironment(ps);

            ps = ImportModules(ps);

            ps = DisconnectExchange(ps);

            ps = ConnectExchangeAsAdmin(ps);

            var aliasFullName = $"{aliasName}@{_tenantDomain}";

            ps.AddScript(string.Format("Set-Mailbox -Identity \"{0}\" -EmailAddresses @{{add = \"{1}\"}}", username, aliasFullName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            if (ps.HadErrors)
                return ps;

            ps = ConnectExchange(ps, username, password);

            ps.AddScript(string.Format("New-MailboxFolder -Name {0} -Parent {1}", aliasName, username));
            ps.AddScript(string.Format("New-InboxRule -Name {0} -HeaderContainsWords {1} -MoveToFolder \":\\{0}\"", aliasName, aliasFullName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            return ps;
        }

        public PowerShell RenameMailAlias(PowerShell ps, string username, string password, string currentAliasName, string newAliasName)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps = PrepareEnvironment(ps);

            ps = ImportModules(ps);

            ps = DisconnectExchange(ps);

            ps = ConnectExchangeAsAdmin(ps);

            var currentAliasFullName = $"{currentAliasName}@{_tenantDomain}";
            var newAliasFullName = $"{newAliasName}@{_tenantDomain}";

            ps.AddScript(string.Format("Set-Mailbox -Identity \"{0}\" -EmailAddresses @{{add = \"{1}\"}}", username, newAliasFullName));
            ps.AddScript(string.Format("Set-Mailbox -Identity \"{0}\" -EmailAddresses @{{remove = \"{1}\"}}", username, currentAliasFullName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            if (ps.HadErrors)
                return ps;

            ps = ConnectExchange(ps, username, password);

            ps.AddScript(string.Format("Remove-InboxRule {0} -Force -Confirm:$false", currentAliasName));
            ps.AddScript(string.Format("New-MailboxFolder -Name {0} -Parent {1}", newAliasName, username));
            ps.AddScript(string.Format("New-InboxRule -Name {0} -HeaderContainsWords {1} -MoveToFolder \":\\{0}\"", newAliasName, newAliasFullName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            return ps;
        }

        public PowerShell RemoveMailAlias(PowerShell ps, string username, string password, string aliasName)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps = PrepareEnvironment(ps);

            ps = ImportModules(ps);

            ps = DisconnectExchange(ps);

            ps = ConnectExchangeAsAdmin(ps);

            var aliasFullName = $"{aliasName}@{_tenantDomain}";

            ps.AddScript(string.Format("Set-Mailbox -Identity \"{0}\" -EmailAddresses @{{remove = \"{1}\"}}", username, aliasFullName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            if (ps.HadErrors)
                return ps;

            ps = ConnectExchange(ps, username, password);

            ps.AddScript(string.Format("Remove-InboxRule {0} -Force -Confirm:$false", aliasName));
            ps = InvokeAndClear(ps);

            ps = DisconnectExchange(ps);

            return ps;
        }

    }
}
