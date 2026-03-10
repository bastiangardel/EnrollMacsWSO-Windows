# Enroll Macs WSO — Windows Edition

Port Windows de l'application macOS Swift/SwiftUI, réécrit en **C# / WPF (.NET 8)**.

---

## Prérequis

| Outil | Version minimale |
|---|---|
| Visual Studio 2022 | 17.8+ |
| .NET SDK | 8.0 |
| OS | Windows 10 / 11 (x64) |

---

## Structure du projet

```
EnrollMacsWSO/
├── Models/
│   ├── Machine.cs          # Modèle de données machine (JSON-sérialisable)
│   └── AppConfig.cs        # Modèle de configuration
├── Services/
│   ├── ConfigManager.cs    # Lecture/écriture registre + DPAPI pour le mot de passe SMB
│   ├── SambaService.cs     # Upload SMB via SMBLibrary (ou écriture locale en test mode)
│   ├── LdapService.cs      # Recherche email LDAP (Novell.Directory.Ldap)
│   └── CsvService.cs       # Import/export CSV + logique de correspondance
├── Views/
│   ├── MainWindow           # Liste des machines, tri, actions
│   ├── AddMachineWindow     # Formulaire ajout d'une machine
│   ├── CsvImportWindow      # Import CSV (name/ocs/inventory)
│   ├── DetailsMachineWindow # Vue détails read-only
│   ├── ConfigurationWindow  # Configuration (Platform ID, SMB, LDAP…)
│   └── CredentialPromptWindow # Vérification mot de passe Windows avant envoi
├── Helpers/
│   ├── RelayCommand.cs      # ICommand MVVM
│   └── ObservableObject.cs  # INotifyPropertyChanged base
├── App.xaml / App.xaml.cs
└── app.manifest             # DPI awareness
```

---

## Stockage de la configuration

### Paramètres non-sensibles → **Registre Windows**
Clé : `HKEY_CURRENT_USER\SOFTWARE\EPFL\EnrollMacsWSO`

| Valeur | Description |
|---|---|
| PlatformId | INT |
| Ownership | STRING |
| MessageType | INT |
| SambaPath | STRING (UNC ou smb://) |
| SambaUsername | STRING |
| LdapServer | STRING |
| LdapBaseDN | STRING |
| IsTestMode | DWORD (0/1) |
| IsConfigured | DWORD (0/1) |

### Mot de passe SMB → **Windows DPAPI** (chiffrement lié à la session utilisateur)
La valeur `SambaPasswordEncrypted` dans le registre contient le mot de passe chiffré
via `ProtectedData.Protect(…, DataProtectionScope.CurrentUser)`.
Il ne peut être déchiffré que par **le même utilisateur Windows sur la même machine**.

---

## Dépendances NuGet

| Package | Usage |
|---|---|
| `SMBLibrary` | Client SMB2/3 natif (pas besoin de mappage réseau) |
| `Novell.Directory.Ldap.NETStandard` | Client LDAP/LDAPS anonyme |
| `CsvHelper` | Parsing et export CSV |
| `Newtonsoft.Json` | Sérialisation JSON des machines |
| `System.Security.Cryptography.ProtectedData` | DPAPI pour le mot de passe |

---

## Build et exécution

```bash
# Restaurer les packages
dotnet restore

# Compiler
dotnet build -c Release

# Lancer
dotnet run
```

Ou ouvrir `EnrollMacsWSO.sln` dans **Visual Studio 2022** et appuyer sur F5.

---

## Mode Test

Activez le **Mode Test** via le toggle en haut à droite de la fenêtre principale.
En mode test, les fichiers JSON sont écrits dans :
```
%USERPROFILE%\Downloads\TestStorage\
```
au lieu d'être envoyés sur le partage SMB.

---

## Authentification avant envoi

Avant d'envoyer les machines sur SMB, l'application demande le **mot de passe Windows**
de l'utilisateur courant (via `LogonUser` Win32 API), équivalent à l'authentification
biométrique Touch ID de la version macOS.

---

## Correspondances macOS → Windows

| macOS (Swift) | Windows (C#) |
|---|---|
| `Keychain` | Windows DPAPI (`ProtectedData`) + Registre |
| `Core Data` | Registre Windows (`HKCU`) |
| `@AppStorage` | Registre Windows |
| `LAContext` (Touch ID) | `LogonUser` Win32 API |
| `SMBClient` (swift-smb) | `SMBLibrary` |
| `ldapsearch` (process) | `Novell.Directory.Ldap` |
| `NSSavePanel` | `SaveFileDialog` (WPF) |
| `NSOpenPanel` | `OpenFileDialog` (WPF) |
