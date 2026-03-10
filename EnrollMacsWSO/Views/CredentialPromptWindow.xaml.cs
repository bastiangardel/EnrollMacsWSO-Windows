using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace EnrollMacsWSO.Views
{
    public partial class CredentialPromptWindow : Window
    {
        public CredentialPromptWindow() => InitializeComponent();

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Confirm_Click(sender, e);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Veuillez entrer votre mot de passe.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            bool valid = ValidateWindowsCredential(username, password);
            if (valid)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "Mot de passe incorrect. Veuillez réessayer.";
                ErrorText.Visibility = Visibility.Visible;
                PasswordBox.Password = "";
                PasswordBox.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ── Windows LogonUser P/Invoke ────────────────────────────────────────

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(
            string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(IntPtr handle);

        private static bool ValidateWindowsCredential(string username, string password)
        {
            try
            {
                // Split DOMAIN\user or user@domain
                string domain = ".";
                string user = username;
                if (username.Contains('\\'))
                {
                    var parts = username.Split('\\', 2);
                    domain = parts[0];
                    user = parts[1];
                }
                else if (username.Contains('@'))
                {
                    domain = username.Split('@')[1];
                    user = username.Split('@')[0];
                }

                const int LOGON32_LOGON_NETWORK = 3;
                const int LOGON32_PROVIDER_DEFAULT = 0;

                bool result = LogonUser(user, domain, password,
                    LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out IntPtr token);

                if (result) CloseHandle(token);
                return result;
            }
            catch
            {
                return false;
            }
        }
    }
}
