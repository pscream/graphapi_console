using System;
using System.Collections.Generic;
using System.Text;

namespace AddMailAliasService.Messages
{
    public class ProcessAlias
    {

        public string TenantDisplayName { get; set; }

        public string TenantPrincipalName { get; set; }

        public string TenantPassword { get; set; }

        public string NewAliasName { get; set; }

        public string CurrentAliasName { get; set; }

        public ActionTypes Type { get; set; }
    }
}
