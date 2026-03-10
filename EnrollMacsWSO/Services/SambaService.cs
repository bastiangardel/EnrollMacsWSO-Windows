using SMBLibrary;
using SMBLibrary.Client;
using System.IO;

namespace EnrollMacsWSO.Services
{
    public static class SambaService
    {
        /// <summary>
        /// Saves a file either to a local test folder (test mode)
        /// or to a real SMB share (production mode).
        /// Returns (success, message).
        /// </summary>
        public static Task<(bool Success, string Message)> SaveFileAsync(
            string filename, byte[] content)
        {
            return Task.Run<(bool, string)>(() =>
            {
                var cfg = ConfigManager.Instance.Load();

                // ── Test mode: write locally ──────────────────────────────
                if (cfg.IsTestMode)
                {
                    try
                    {
                        string localDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Downloads", "TestStorage");
                        Directory.CreateDirectory(localDir);
                        File.WriteAllBytes(Path.Combine(localDir, filename), content);
                        return (true, $"[TEST] Fichier enregistré dans {localDir}");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"[TEST] Erreur : {ex.Message}");
                    }
                }

                // ── Production: SMB upload ────────────────────────────────
                string sambaPassword = ConfigManager.Instance.LoadSambaPassword();

                if (string.IsNullOrWhiteSpace(cfg.SambaPath))
                    return (false, "Chemin SMB non configuré.");

                try
                {
                    // Parse smb://host/share/path/to/dir
                    // Accept both smb:// and \\host\share formats
                    string rawPath = cfg.SambaPath.Trim();

                    string host, shareName, remoteDirRelative;

                    if (rawPath.StartsWith("smb://", StringComparison.OrdinalIgnoreCase))
                    {
                        var uri = new Uri(rawPath);
                        host = uri.Host;
                        var parts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
                        shareName = parts[0];
                        remoteDirRelative = parts.Length > 1 ? parts[1].Replace('/', '\\') : "";
                    }
                    else
                    {
                        // UNC: \\host\share\path
                        var parts = rawPath.TrimStart('\\').Split('\\');
                        host = parts[0];
                        shareName = parts.Length > 1 ? parts[1] : "";
                        remoteDirRelative = parts.Length > 2
                            ? string.Join("\\", parts[2..])
                            : "";
                    }

                    if (string.IsNullOrWhiteSpace(shareName))
                        return (false, "Nom de partage manquant dans le chemin SMB.");

                    var client = new SMB2Client();
                    bool connected = client.Connect(host, SMBTransportType.DirectTCPTransport);
                    if (!connected)
                        return (false, $"Impossible de se connecter à {host}.");

                    var status = client.Login(string.Empty, cfg.SambaUsername, sambaPassword);
                    if (status != NTStatus.STATUS_SUCCESS)
                        return (false, $"Échec de l'authentification SMB : {status}");

                    var fileStore = client.TreeConnect(shareName, out status) as ISMBFileStore;
                    if (status != NTStatus.STATUS_SUCCESS || fileStore == null)
                        return (false, $"Impossible d'accéder au partage '{shareName}' : {status}");

                    string remoteFilePath = string.IsNullOrEmpty(remoteDirRelative)
                        ? filename
                        : $"{remoteDirRelative}\\{filename}";

                    // Create or overwrite file
                    status = fileStore.CreateFile(
                        out object? fileHandle,
                        out FileStatus _,
                        remoteFilePath,
                        AccessMask.GENERIC_WRITE | AccessMask.GENERIC_READ,
                        SMBLibrary.FileAttributes.Normal,
                        ShareAccess.None,
                        CreateDisposition.FILE_OVERWRITE_IF,
                        CreateOptions.FILE_NON_DIRECTORY_FILE,
                        null);

                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        fileStore.Disconnect();
                        client.Logoff();
                        client.Disconnect();
                        return (false, $"Impossible de créer le fichier distant : {status}");
                    }

                    // Write in chunks
                    int offset = 0;
                    int chunkSize = 65536;
                    while (offset < content.Length)
                    {
                        int count = Math.Min(chunkSize, content.Length - offset);
                        byte[] chunk = new byte[count];
                        Buffer.BlockCopy(content, offset, chunk, 0, count);

                        status = fileStore.WriteFile(out int _, fileHandle!, offset, chunk);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            fileStore.CloseFile(fileHandle!);
                            fileStore.Disconnect();
                            client.Logoff();
                            client.Disconnect();
                            return (false, $"Erreur d'écriture SMB : {status}");
                        }
                        offset += count;
                    }

                    fileStore.CloseFile(fileHandle!);
                    fileStore.Disconnect();
                    client.Logoff();
                    client.Disconnect();

                    return (true, $"Fichier '{filename}' enregistré sur {cfg.SambaPath}");
                }
                catch (Exception ex)
                {
                    return (false, $"Erreur SMB : {ex.Message}");
                }
            });
        }
    }
}
