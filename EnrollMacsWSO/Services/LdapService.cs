using Novell.Directory.Ldap;

namespace EnrollMacsWSO.Services
{
    public enum LdapResultType { Found, NoMail, NotFound, Error }

    public record LdapResult(LdapResultType Type, string Email = "");

    public static class LdapService
    {
        public static Task<LdapResult> FetchEmailAsync(string username)
        {
            return Task.Run(() =>
            {
                var config = ConfigManager.Instance.Load();

                if (string.IsNullOrWhiteSpace(config.LdapServer) || string.IsNullOrWhiteSpace(username))
                    return new LdapResult(LdapResultType.Error);

                try
                {
                    // Parse host and port from ldap(s)://host:port
                    var uri = new Uri(config.LdapServer);
                    string host = uri.Host;
                    int port = uri.IsDefaultPort
                        ? (uri.Scheme == "ldaps" ? 636 : 389)
                        : uri.Port;
                    bool useSsl = uri.Scheme == "ldaps";

                    using var conn = new LdapConnection();
                    conn.SecureSocketLayer = useSsl;
                    conn.Connect(host, port);
                    conn.Bind("", ""); // anonymous bind

                    string filter = $"(uid={EscapeLdap(username)})";
                    string baseDn = string.IsNullOrWhiteSpace(config.LdapBaseDN)
                        ? "o=epfl,c=ch"
                        : config.LdapBaseDN;

                    var searchResults = conn.Search(
                        baseDn,
                        LdapConnection.ScopeSub,
                        filter,
                        new[] { "mail" },
                        false);

                    if (!searchResults.HasMore())
                        return new LdapResult(LdapResultType.NotFound);

                    var entry = searchResults.Next();
                    var mailAttr = entry.GetAttribute("mail");

                    if (mailAttr == null || string.IsNullOrWhiteSpace(mailAttr.StringValue))
                        return new LdapResult(LdapResultType.NoMail);

                    return new LdapResult(LdapResultType.Found, mailAttr.StringValue);
                }
                catch
                {
                    return new LdapResult(LdapResultType.Error);
                }
            });
        }

        private static string EscapeLdap(string input)
        {
            return input
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }
    }
}
