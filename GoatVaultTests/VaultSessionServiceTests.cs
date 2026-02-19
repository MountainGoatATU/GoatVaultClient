namespace GoatVaultTests;

public class VaultSessionServiceAdditionalTests
{
    // TODO: Broken tests
    /*
    [Fact]
    public void AddEntry_AddsVaultEntryAndRaisesEvent()
    {
        // Arrange
        var service = new VaultSessionService
        {
            DecryptedVault = new VaultData { Entries = [] }
        };
        var entry = new VaultEntry
        {
            Site = "example2.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.AddEntry(entry);

        // Assert
        Assert.Single(service.VaultEntries);
        Assert.Contains(entry, service.VaultEntries);
        Assert.True(eventRaised, "VaultEntriesChanged event should be raised");
    }

    [Fact]
    public void RemoveEntry_RemovesVaultEntryAndRaisesEvent()
    {
        // Arrange
        var entry = new VaultEntry
        {
            Site = "example.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        var service = new VaultSessionService
        {
            DecryptedVault = new VaultData { Entries = [entry] }
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.RemoveEntry(entry);

        // Assert
        Assert.Empty(service.VaultEntries);
        Assert.True(eventRaised, "VaultEntriesChanged event should be raised");
    }

    [Fact]
    public void UpdateEntry_UpdatesExistingEntryAndRaisesEvent()
    {
        // Arrange
        var oldEntry = new VaultEntry
        {
            Site = "example.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };

        var newEntry = new VaultEntry
        {
            Site = "example2.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        var service = new VaultSessionService
        {
            DecryptedVault = new VaultData { Entries = [oldEntry] }
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.UpdateEntry(oldEntry, newEntry);

        // Assert
        Assert.Single(service.VaultEntries);
        Assert.Equal("example2.com", service.VaultEntries[0].Site);
        Assert.True(eventRaised, "VaultEntriesChanged event should be raised");
    }

    [Fact]
    public void ChangeMasterPassword_UpdatesPasswordAndRaisesEvent()
    {
        // Arrange
        var service = new VaultSessionService();
        const string newPassword = "NewSuperSecret!";
        bool eventRaised = false;
        service.MasterPasswordChanged += () => eventRaised = true;

        // Act
        service.ChangeMasterPassword(newPassword);

        // Assert
        Assert.Equal(newPassword, service.MasterPassword);
        Assert.True(eventRaised, "MasterPasswordChanged event should be raised");
    }

    [Fact]
    public void VaultEntries_ReturnsEmptyListWhenDecryptedVaultIsNull()
    {
        // Arrange
        var service = new VaultSessionService { DecryptedVault = null };

        // Act
        var entries = service.VaultEntries;

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void AddEntry_DoesNothingWhenDecryptedVaultIsNull()
    {
        // Arrange
        var service = new VaultSessionService { DecryptedVault = null };
        var entry = new VaultEntry
        {
            Site = "example.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.AddEntry(entry);

        // Assert
        Assert.Empty(service.VaultEntries);
        Assert.False(eventRaised);
    }

    [Fact]
    public void RemoveEntry_DoesNothingWhenDecryptedVaultIsNull()
    {
        // Arrange
        var service = new VaultSessionService { DecryptedVault = null };
        var entry = new VaultEntry
        {
            Site = "example.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.RemoveEntry(entry);

        // Assert
        Assert.Empty(service.VaultEntries);
        Assert.False(eventRaised);
    }

    [Fact]
    public void UpdateEntry_DoesNothingWhenEntryNotFound()
    {
        // Arrange
        var service = new VaultSessionService
        {
            DecryptedVault = new VaultData 
            { 
                Entries = [new VaultEntry 
                {
                    Site = "example2.com",
                    UserName = "testuser",
                    Password = "SuperSecure123!",
                    Description = "Test entry",
                    Category = "Work",
                    HasMfa = true,
                    MfaSecret = "ABCDEF123456",
                    CurrentTotpCode = "123456",
                    TotpTimeRemaining = 30,
                    BreachCount = 0 }
                ]
            }
        };

        var entryToUpdate = new VaultEntry
        {
            Site = "example2.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        var newEntry = new VaultEntry
        {
            Site = "example.com",
            UserName = "testuser",
            Password = "SuperSecure123!",
            Description = "Test entry",
            Category = "Work",
            HasMfa = true,
            MfaSecret = "ABCDEF123456",
            CurrentTotpCode = "123456",
            TotpTimeRemaining = 30,
            BreachCount = 0
        };
        bool eventRaised = false;
        service.VaultEntriesChanged += () => eventRaised = true;

        // Act
        service.UpdateEntry(entryToUpdate, newEntry);

        // Assert
        Assert.DoesNotContain(newEntry, service.VaultEntries);
        Assert.True(eventRaised, "VaultEntriesChanged should still be invoked even if entry not found");
    }
    */
}
