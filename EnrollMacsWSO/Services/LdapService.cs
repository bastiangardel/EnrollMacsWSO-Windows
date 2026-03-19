using System.DirectoryServices;

namespace EnrollMacsWSO.Services
{
    public enum LdapResultType { Found, NoMail, NotFound, Error }

    public record LdapResult(LdapResultType Type, string Email = "");

    public static class LdapService
    {
        /// <summary>
        /// Équivalent C# de :
        ///   ([adsisearcher]"(&(objectClass=user)(sAMAccountName=USERNAME))").FindOne().Properties.mail
        /// Utilise le DC du domaine joint — aucune configuration LDAP requise.
        /// </summary>
        public static Task<LdapResult> FetchEmailAsync(string username)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(username))
                    return new LdapResult(LdapResultType.Error);

                try
                {
                    string filter = $"(&(objectClass=user)(sAMAccountName={EscapeLdap(username)}))";

                    using var searcher = new DirectorySearcher
                    {
                        Filter     = filter,
                        PageSize   = 1,
                    };
                    searcher.PropertiesToLoad.Add("mail");
                    searcher.PropertiesToLoad.Add("sAMAccountName");

                    SearchResult? result = searcher.FindOne();

                    // Compte introuvable dans l'AD
                    if (result == null)
                        return new LdapResult(LdapResultType.NotFound);

                    // Compte trouvé — cherche l'attribut mail
                    if (result.Properties["mail"].Count > 0)
                    {
                        string mail = result.Properties["mail"][0]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(mail))
                            return new LdapResult(LdapResultType.Found, mail);
                    }

                    // Compte trouvé mais pas de mail
                    return new LdapResult(LdapResultType.NoMail);
                }
                catch
                {
                    return new LdapResult(LdapResultType.Error);
                }
            });
        }

        // Échappe les caractères spéciaux LDAP (RFC 4515)
        private static string EscapeLdap(string input) =>
            input
                .Replace("\\", "\\5c")
                .Replace("*",  "\\2a")
                .Replace("(",  "\\28")
                .Replace(")",  "\\29")
                .Replace("\0", "\\00");
    }
}
