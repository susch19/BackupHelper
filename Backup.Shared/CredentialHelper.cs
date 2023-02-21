using AdysTech.CredentialManager;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Backup.Shared;
public static class CredentialHelper
{
    public static byte[] GetCredentialsFor(string name, string defaultPW, string caption, ref bool credManagerSave)
    {

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var target = "backuprestore:" + name;
            var cred = CredentialManager.GetCredentials(target);
            if (cred is null)
            {
                cred = CredentialManager.PromptForCredentials(target, ref credManagerSave, $"Please enter the ecnryption passwort for \"{name}\"", caption, name);
            }
            if (cred is not null)
            {
                if (credManagerSave)
                    CredentialManager.SaveCredentials(target, cred);
                return Encoding.UTF8.GetBytes(cred.Password);
            }
        }
        return Encoding.UTF8.GetBytes(defaultPW);
    }
}
