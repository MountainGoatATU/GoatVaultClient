using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultApplication.Shamir;

public class EnableShamirUseCase
{
    private readonly ISessionContext _session;
    private readonly IUserRepository _users;
    private readonly ICryptoService _crypto;
    private readonly IServerAuthService _serverAuth;
    public EnableShamirUseCase(
        ISessionContext session,
        IUserRepository users,
        ICryptoService cryptoService,
        IServerAuthService serverAuth
        )
    {
        _session = session;
        _users = users;
        _crypto = cryptoService;
        _serverAuth = serverAuth;
    }
    public async Task ExecuteAsync(string currentPassword)
    {
        if (_session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await _users.GetByIdAsync(_session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.ShamirEnabled == true)
            throw new InvalidOperationException("Shamir's Secret Sharing is already enabled for this user.");

        // 1. Verify current password
        var currentAuthVerifier = _crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt);
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // 2. Update server
        var updateRequest = new DisableShamirRequest
        {
            ShamirEnabled = true
        };

        var serverUser = await _serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.ShamirEnabled = serverUser.ShamirEnabled;
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await _users.SaveAsync(user);
    }
}
