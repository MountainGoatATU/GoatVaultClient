using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultApplication.Shamir;

public class ValidateUserEmailUseCase (IServerAuthService serverAuth)
{
    public async Task Execute(Email email, CancellationToken ct = default)
    { 
        try
        {
            // Check if the user exists on the server
            var authInitRequest = new AuthInitRequest { Email = email.Value };
            var authInitResponse = await serverAuth.InitAsync(authInitRequest, ct);
            // If the server returns a response, the email is valid.
            if (authInitResponse.ShamirEnabled == false)
            {
                throw new InvalidOperationException("Shamir secret is not enabled on your account");
            }
        }
        catch 
        {
            throw;
        }
    }     
}
