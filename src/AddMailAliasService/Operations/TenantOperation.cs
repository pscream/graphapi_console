using System.Management.Automation;

using Microsoft.Extensions.Logging;

namespace AddMailAliasService.Operations
{
    internal class TenantOperation : OperationBase
    {
        public TenantOperation(ILogger<TenantOperation> logger, string adminUsername, string adminPassword)
            : base(logger, adminUsername, adminPassword)
        {

        }

        protected override PowerShell ImportModules(PowerShell ps)
        {

            if (ps == null)
                ps = PowerShell.Create();

            ps.AddScript("Import-Module Az.Accounts");
            ps.AddScript("Import-Module AzureAD.Standard.Preview");
            ps = InvokeAndClear(ps);

            ps.AddScript("Write-Information $(Get-Module -Verbose | out-string)");
            ps = InvokeAndClear(ps);

            return ps;

        }

        public PowerShell CreateADUser(PowerShell ps, string displayName, string username, string password, string azurePlan)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps = PrepareEnvironment(ps);

            ps = ImportModules(ps);

            ps = ConnectAzureADAsAdmin(ps);

            ps.AddScript("$PasswordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile");
            ps.AddScript(string.Format("$PasswordProfile.Password = \"{0}\"", password));
            ps.AddScript("$PasswordProfile.ForceChangePasswordNextLogin = $false");
            ps.AddScript(string.Format("New-AzureADUser -UserPrincipalName \"{0}\" -DisplayName \"{1}\" -PasswordProfile $passwordProfile -Mailnickname {2} -UsageLocation \"US\" -AccountEnabled:$true",
                username, displayName, username.Split('@')[0]));
            ps = InvokeAndClear(ps);

            ps = AssignUserLincense(ps, username, azurePlan);

            ps = DisconnectAzureAD(ps);

            return ps;
        }

        public PowerShell RemoveADUser(PowerShell ps, string username)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps = PrepareEnvironment(ps);

            ps = ImportModules(ps);

            ps = ConnectAzureADAsAdmin(ps);

            ps.AddScript(string.Format("Remove-AzureADUser -ObjectId {0}", username));
            ps = InvokeAndClear(ps);

            ps = DisconnectAzureAD(ps);

            return ps;
        }

        private PowerShell AssignUserLincense(PowerShell ps, string username, string azurePlan)
        {
            ps.AddScript("$license = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicense");
            ps.AddScript(string.Format("$license.SkuId = (Get-AzureADSubscribedSku | Where-Object -Property SkuPartNumber -Value \"{0}\" -EQ).SkuID", azurePlan));
            ps.AddScript("$licensesToAssign = New-Object -TypeName Microsoft.Open.AzureAD.Model.AssignedLicenses");
            ps.AddScript("$licensesToAssign.AddLicenses = $license");
            ps.AddScript(string.Format("Set-AzureADUserLicense -ObjectId \"{0}\" -AssignedLicenses $licensesToAssign", username));
            ps = InvokeAndClear(ps);

            return ps;
        }

    }
}
