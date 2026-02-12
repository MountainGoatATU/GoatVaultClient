using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultCore.Services.Secrets
{
    public interface IMnemonicEncoder
    {
        string Encode(byte[] data);
        byte[] Decode(string mnemonic);
    }
}
