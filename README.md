[![codecov](https://codecov.io/github/mountaingoatatu/goatvaultclient/graph/badge.svg?token=5ESATTO6OQ)](https://codecov.io/github/mountaingoatatu/goatvaultclient)
[![CodeScene Average Code Health](https://codescene.io/projects/76220/status-badges/average-code-health?component-name=Goatvaultclient)](https://codescene.io/projects/76220/architecture/biomarkers?component=Goatvaultclient)

# GoatVault

**GoatVault** is a secure, cross-platform password manager built with **.NET MAUI**.

## Features

*   Zero-knowledge storage of vault and master password
*   Encrypted vault with Argon2Id and AES
*   Cross-platform syncing on Windows and Android

## Architecture

The project follows the **MVVM (Model-View-ViewModel)** architectural pattern, leveraging the **CommunityToolkit.Mvvm** for efficient state management and data binding.

*   **`GoatVaultApplication`**: The application layer containing use cases and session context.
*   **`GoatVaultClient`**: The UI layer containing Views, ViewModels, and platform-specific implementations.
*   **`GoatVaultCore`**: The domain layer containing models, objects, and abstractions.
*   **`GoatVaultInfrastructure`**: The service layer handling data, encryption, and networking.

## Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
*   MAUI Workloads for your target platforms (android, ios, maccatalyst, windows)

### Installation
1.  Clone the repository:
    ```bash
    git clone https://github.com/MountainGoatATU/GoatVaultClient.git
    cd GoatVaultClient
    ```

2.  Restore dependencies:
    ```bash
    dotnet restore
    ```

3.  Create a file named `appsettings.json` in the `GoatVaultClient/GoatVaultClient` directory.

4.  Copy `appsettings.example.json` to `appsettings.json` and update the values:
    ```json
    {
      "API_BASE_URL": "https://api.example.com"
    }
    ```

5.  Build and Run:
    *   **Windows:** Select `GoatVaultClient` (Windows Machine) and run.
