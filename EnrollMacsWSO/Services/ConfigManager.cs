using EnrollMacsWSO.Models;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace EnrollMacsWSO.Services
{
    /// <summary>
    /// Manages application configuration:
    /// - Non-sensitive settings stored in Windows Registry (HKCU)
    /// - Samba password encrypted via Windows DPAPI (per-user)
    /// </summary>
    public class ConfigManager
    {
        private const string RegistryKeyPath = @"SOFTWARE\EPFL\EnrollMacsWSO";
        private const string PasswordValueName = "SambaPasswordEncrypted";

        public static ConfigManager Instance { get; } = new ConfigManager();
        private ConfigManager() { }

        // ── Registry helpers ─────────────────────────────────────────────────

        private RegistryKey OpenOrCreateKey(bool writable = false)
        {
            var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable);
            if (key == null && writable)
                key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, true);
            return key!;
        }

        private string RegGet(string name, string defaultValue = "")
        {
            try
            {
                using var key = OpenOrCreateKey();
                return key?.GetValue(name)?.ToString() ?? defaultValue;
            }
            catch { return defaultValue; }
        }

        private int RegGetInt(string name, int defaultValue = 0)
        {
            try
            {
                using var key = OpenOrCreateKey();
                var val = key?.GetValue(name);
                if (val is int i) return i;
                if (int.TryParse(val?.ToString(), out int parsed)) return parsed;
                return defaultValue;
            }
            catch { return defaultValue; }
        }

        private bool RegGetBool(string name, bool defaultValue = false)
        {
            try
            {
                using var key = OpenOrCreateKey();
                var val = key?.GetValue(name);
                if (val is int i) return i != 0;
                return defaultValue;
            }
            catch { return defaultValue; }
        }

        private void RegSet(string name, object value)
        {
            using var key = OpenOrCreateKey(true);
            key.SetValue(name, value);
        }

        // ── DPAPI password ────────────────────────────────────────────────────

        public void SaveSambaPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                RegSet(PasswordValueName, "");
                return;
            }
            byte[] plain = Encoding.UTF8.GetBytes(password);
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            RegSet(PasswordValueName, Convert.ToBase64String(encrypted));
        }

        public string LoadSambaPassword()
        {
            string b64 = RegGet(PasswordValueName);
            if (string.IsNullOrEmpty(b64)) return "";
            try
            {
                byte[] encrypted = Convert.FromBase64String(b64);
                byte[] plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch { return ""; }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public AppConfig Load()
        {
            return new AppConfig
            {
                PlatformId = RegGetInt("PlatformId", 12),
                Ownership = RegGet("Ownership", "C"),
                MessageType = RegGetInt("MessageType", 0),
                SambaPath = RegGet("SambaPath"),
                SambaUsername = RegGet("SambaUsername"),
                LdapServer = RegGet("LdapServer"),
                LdapBaseDN = RegGet("LdapBaseDN", "o=epfl,c=ch"),
                IsTestMode = RegGetBool("IsTestMode", false),
                IsConfigured = RegGetBool("IsConfigured", false)
            };
        }

        public void Save(AppConfig config, string sambaPassword)
        {
            RegSet("PlatformId", config.PlatformId);
            RegSet("Ownership", config.Ownership);
            RegSet("MessageType", config.MessageType);
            RegSet("SambaPath", config.SambaPath);
            RegSet("SambaUsername", config.SambaUsername);
            RegSet("LdapServer", config.LdapServer);
            RegSet("LdapBaseDN", config.LdapBaseDN);
            RegSet("IsTestMode", config.IsTestMode ? 1 : 0);
            RegSet("IsConfigured", config.IsConfigured ? 1 : 0);
            SaveSambaPassword(sambaPassword);
        }

        public void SetTestMode(bool value)
        {
            RegSet("IsTestMode", value ? 1 : 0);
        }

        public bool IsTestMode => RegGetBool("IsTestMode", false);

        public void ClearAll()
        {
            try { Registry.CurrentUser.DeleteSubKey(RegistryKeyPath, false); }
            catch { /* already absent */ }
        }
    }
}
