namespace GoatVaultCore.Models.Api;

public class DisableMfaRequest
{
    public required bool MfaEnabled { get; set; }

    public required string? MfaSecret { get; set; }
}
