using System.Collections.ObjectModel;
using System.Management.Automation;

using Microsoft.Extensions.Logging;

namespace AddMailAliasService.Operations
{

    internal abstract class OperationBase
    {

        private readonly ILogger<OperationBase> _logger;

        protected string _adminUsername = string.Empty;
        protected string _adminPassword = string.Empty;

        public OperationBase(ILogger<OperationBase> logger, string adminUsername, string adminPassword)
        {
            _logger = logger;
            _adminUsername = adminUsername;
            _adminPassword = adminPassword;
        }

        protected abstract PowerShell ImportModules(PowerShell ps);

        public virtual PowerShell PrepareEnvironment(PowerShell ps)
        {
            if (ps == null)
                ps = PowerShell.Create();

            //ps.AddScript("Write-Information $(Get-Host | out-string)");
            //ps.AddScript("Set-ExecutionPolicy -Scope Process -ExecutionPolicy Unrestricted -Force");
            //ps.AddScript("Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted -Force");
            //ps.AddScript("Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy Unrestricted -Force");
            ps.AddScript("Write-Information $(Get-ExecutionPolicy -List | out-string)");
            ps.AddScript("Write-Information $Env:PROCESSOR_ARCHITECTURE");
            ps = InvokeAndClear(ps);

            return ps;
        }

        public PowerShell ConnectAzureADAsAdmin(PowerShell ps)
        {
            if(ps == null)
                ps = PowerShell.Create();

            ps.AddScript(string.Format("$username = \"{0}\"", _adminUsername));
            ps.AddScript(string.Format("$password = ConvertTo-SecureString \"{0}\" -AsPlainText -Force", _adminPassword));
            ps.AddScript("$psCred = New-Object System.Management.Automation.PSCredential -ArgumentList ($username, $password)");
            ps.AddScript("Connect-AzAccount -Credential $psCred");
            ps.AddScript("$token = Get-AzAccessToken -ResourceTypeName AadGraph");
            ps.AddScript(string.Format("Connect-AzureAD -AadAccessToken  $token.Token -AccountId \"{0}\"", _adminUsername));
            ps = InvokeAndClear(ps);

            return ps;
        }

        public PowerShell DisconnectAzureAD(PowerShell ps)
        {
            if (ps == null)
                ps = PowerShell.Create();

            ps.AddScript("Disconnect-AzureAD");
            ps = InvokeAndClear(ps);

            return ps;
        }


        protected PowerShell InvokeAndClear(PowerShell ps)
        {
            var psObjects = ps.Invoke();
            PrintPowershellOutput(ps, psObjects);
            ps.Streams.ClearStreams();
            ps.Commands.Clear();
            return ps;
        }

        protected void PrintPowershellOutput(PowerShell ps, Collection<PSObject> pSObjects)
        {
            foreach (var record in ps.Streams.Information)
                _logger.LogInformation(record.ToString());

            foreach (var record in ps.Streams.Verbose)
                _logger.LogInformation(record.ToString());

            if (ps.HadErrors)
            {
                var errors = ps.Streams.Error;
                foreach (var error in errors)
                {
                    _logger.LogError(error.Exception.Message);
                }
            }

            foreach (var result in pSObjects)
                _logger.LogInformation(result.ToString());
        }

    }

}
