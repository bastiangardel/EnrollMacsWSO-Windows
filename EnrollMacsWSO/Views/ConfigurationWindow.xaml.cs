using EnrollMacsWSO.Models;
using EnrollMacsWSO.Services;
using System.Windows;

namespace EnrollMacsWSO.Views
{
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            var cfg = ConfigManager.Instance.Load();
            PlatformIdBox.Text = cfg.PlatformId.ToString();
            OwnershipBox.Text = cfg.Ownership;
            MessageTypeBox.Text = cfg.MessageType.ToString();
            SambaPathBox.Text = cfg.SambaPath;
            SambaUsernameBox.Text = cfg.SambaUsername;
            LdapServerBox.Text = cfg.LdapServer;
            LdapBaseDNBox.Text = cfg.LdapBaseDN;
            SambaPasswordBox.Password = ConfigManager.Instance.LoadSambaPassword();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlatformIdBox.Text) ||
                string.IsNullOrWhiteSpace(OwnershipBox.Text) ||
                string.IsNullOrWhiteSpace(MessageTypeBox.Text) ||
                string.IsNullOrWhiteSpace(SambaPathBox.Text))
            {
                MessageBox.Show("Les champs Platform ID, Ownership, Message Type et Chemin SMB sont obligatoires.",
                    "Champs manquants", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cfg = new AppConfig
            {
                PlatformId = int.TryParse(PlatformIdBox.Text, out var pid) ? pid : 0,
                Ownership = OwnershipBox.Text.Trim(),
                MessageType = int.TryParse(MessageTypeBox.Text, out var mt) ? mt : 0,
                SambaPath = SambaPathBox.Text.Trim(),
                SambaUsername = SambaUsernameBox.Text.Trim(),
                LdapServer = LdapServerBox.Text.Trim(),
                LdapBaseDN = LdapBaseDNBox.Text.Trim(),
                IsConfigured = true,
                IsTestMode = ConfigManager.Instance.IsTestMode
            };

            ConfigManager.Instance.Save(cfg, SambaPasswordBox.Password);

            MessageBox.Show("Configuration enregistrée avec succès !",
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Effacer toute la configuration (registre + mot de passe) ?",
                "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                ConfigManager.Instance.ClearAll();
                PlatformIdBox.Text = "";
                OwnershipBox.Text = "";
                MessageTypeBox.Text = "";
                SambaPathBox.Text = "";
                SambaUsernameBox.Text = "";
                SambaPasswordBox.Password = "";
                LdapServerBox.Text = "";
                LdapBaseDNBox.Text = "";
                MessageBox.Show("Configuration effacée.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
