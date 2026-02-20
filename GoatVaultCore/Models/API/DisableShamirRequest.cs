using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultCore.Models.API;

public class DisableShamirRequest
{
    public required bool ShamirEnabled { get; set; } = false;
}
