namespace GoatVaultCore.Models.Api;

public class EnableMfaRequest
{
    public required bool MfaEnabled { get; set; } = true;
    public required string MfaSecret { get; set; }
}
