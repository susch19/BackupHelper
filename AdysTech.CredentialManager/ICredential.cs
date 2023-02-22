using System;
using System.Collections.Generic;
using System.Net;

namespace AdysTech.CredentialManager
{
    public interface ICredential
    {
        CredentialType Type { get; set; }
        string TargetName { get; set; }
        string Comment { get; set; }
        DateTime LastWritten { get; set; }
        string CredentialBlob { get; set; }
        Persistance Persistance { get; set; }
        IDictionary<string, Object> Attributes { get; set; }
        string UserName { get; set; }

        NetworkCredential ToNetworkCredential();
        bool SaveCredential(bool AllowBlankPassword = false);

        bool RemoveCredential();
    }
}
