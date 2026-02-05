# GoatVault

**GoatVault** is a secure, cross-platform password manager built with **.NET MAUI**.

## Architecture

The project follows the **MVVM (Model-View-ViewModel)** architectural pattern, leveraging the **CommunityToolkit.Mvvm** for efficient state management and data binding.

*   **`GoatVaultClient`**: The UI layer containing Views, ViewModels, and platform-specific implementations.
*   **`GoatVaultCore`**: The domain layer containing models.
*   **`GoatVaultInfrastructure`**: The service layer handling data, encryption, and networking.

## Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
*   MAUI Workloads for your target platforms (android, ios, maccatalyst, windows)

### Installation
1.  Clone the repository:
    ```bash
    git clone https://github.com/MountainGoatATU/GoatVaultClient.git
    cd GoatVaultClient/GoatVaultClient-v4
    ```

2.  Restore dependencies:
    ```bash
    dotnet restore
    ```

3.  Build and Run:
    *   **Windows:** Select `GoatVaultClient` (Windows Machine) and run.
