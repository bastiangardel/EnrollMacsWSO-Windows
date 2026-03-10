using EnrollMacsWSO.Models;
using EnrollMacsWSO.Services;
using System.Windows;
using System.Windows.Controls;

namespace EnrollMacsWSO.Views
{
    public partial class AddMachineWindow : Window
    {
        public Machine? Result { get; private set; }

        public AddMachineWindow()
        {
            InitializeComponent();
            UpdateAddButton();
        }

        // ── Auto friendly name ────────────────────────────────────────────────

        private void AssetNumberBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FriendlyNameBox.Text = $"SCX-{AssetNumberBox.Text.Trim()}";
            UpdateAddButton();
        }

        // Handler partagé pour tous les champs texte obligatoires
        private void RequiredField_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddButton();
        }

        private void EmployeeType_Changed(object sender, RoutedEventArgs e)
        {
            // Disable AcrobatException for "Personnel"
            if (AcrobatException != null)
            {
                bool isPersonnel = EmpPersonnel.IsChecked == true;
                AcrobatException.IsEnabled = !isPersonnel;
                if (isPersonnel) AcrobatException.IsChecked = false;
            }
            UpdateAddButton();
        }

        private void DeviceType_Changed(object sender, RoutedEventArgs e) => UpdateAddButton();

        // ── LDAP email ────────────────────────────────────────────────────────

        private async void LoadEmail_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            if (string.IsNullOrEmpty(username)) return;

            LoadEmailBtn.IsEnabled = false;
            LoadEmailBtn.Content = "…";
            LdapMessage.Visibility = Visibility.Collapsed;

            var result = await LdapService.FetchEmailAsync(username);

            switch (result.Type)
            {
                case LdapResultType.Found:
                    EmailBox.Text = result.Email;
                    LdapMessage.Visibility = Visibility.Collapsed;
                    break;
                case LdapResultType.NoMail:
                    EmailBox.Text = "";
                    LdapMessage.Text = "Pas d'email disponible, merci d'en définir un.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
                case LdapResultType.NotFound:
                    EmailBox.Text = "";
                    LdapMessage.Text = "Le compte n'existe pas dans l'AD.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
                case LdapResultType.Error:
                    EmailBox.Text = "";
                    LdapMessage.Text = "Erreur lors de la recherche LDAP.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
            }

            LoadEmailBtn.Content = "Load email";
            LoadEmailBtn.IsEnabled = true;
            UpdateAddButton();
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void UpdateAddButton()
        {
            bool valid =
                !string.IsNullOrWhiteSpace(UsernameBox?.Text) &&
                !string.IsNullOrWhiteSpace(SciperBox?.Text) &&
                !string.IsNullOrWhiteSpace(AssetNumberBox?.Text) &&
                !string.IsNullOrWhiteSpace(SerialNumberBox?.Text) &&
                !string.IsNullOrWhiteSpace(EmailBox?.Text) &&
                (EmpPersonnel?.IsChecked == true || EmpHote?.IsChecked == true || EmpHorsEPFL?.IsChecked == true) &&
                (DevLaptop?.IsChecked == true || DevWorkstation?.IsChecked == true || DevMobile?.IsChecked == true);

            if (AddBtn != null) AddBtn.IsEnabled = valid;
        }

        // ── Add ───────────────────────────────────────────────────────────────

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var cfg = ConfigManager.Instance.Load();

            string locationGroupId = "628";
            if (DevWorkstation.IsChecked == true) locationGroupId = "629";
            else if (DevMobile.IsChecked == true) locationGroupId = "627";

            string vpn = VpnSSC.IsChecked == true ? "SSC" : VpnAGA.IsChecked == true ? "AGA" : "";
            string fm = FmTTO.IsChecked == true ? "TTO-AJ" :
                        FmOHSPR.IsChecked == true ? "OHSPR-DSE" :
                        FmAutres.IsChecked == true ? "Autres" : "";
            string emp = EmpPersonnel.IsChecked == true ? "Personnel" :
                         EmpHote.IsChecked == true ? "Hôte" : "Hors-EPFL";
            string dev = DevLaptop.IsChecked == true ? "Laptop" :
                         DevWorkstation.IsChecked == true ? "Workstation" : "Mobile";

            Result = new Machine
            {
                EndUserName = UsernameBox.Text.Trim(),
                Sciper = SciperBox.Text.Trim(),
                AssetNumber = AssetNumberBox.Text.Trim(),
                SerialNumber = SerialNumberBox.Text.Trim(),
                FriendlyName = FriendlyNameBox.Text,
                Email = EmailBox.Text.Trim(),
                LocationGroupId = locationGroupId,
                MessageType = cfg.MessageType,
                PlatformId = cfg.PlatformId,
                Ownership = cfg.Ownership,
                EmployeeType = emp,
                DeviceType = dev,
                VpnSelect = vpn,
                Filemaker = fm,
                TableauDesktopBool = TableauDesktop.IsChecked == true,
                TableauPrepBool = TableauPrep.IsChecked == true,
                MindmanagerBool = MindManager.IsChecked == true,
                LinaExceptionBool = LinaException.IsChecked == true,
                AcrobatReaderExceptionBool = AcrobatException.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
