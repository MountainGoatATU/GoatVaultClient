using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultCore.Models.API;

public class EnableShamirRequest
{
    public required bool ShamirEnabled { get; set; } = true;
}
