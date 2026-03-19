using EnrollMacsWSO.Models;
using EnrollMacsWSO.Services;
using System.Windows;
using System.Windows.Controls;

namespace EnrollMacsWSO.Views
{
    public partial class DetailsMachineWindow : Window
    {
        // Machine mise à jour, disponible après DialogResult = true
        public Machine? Result { get; private set; }

        // On conserve les valeurs immuables de la machine d'origine
        private readonly Machine _original;

        public DetailsMachineWindow(Machine machine)
        {
            _original = machine;
            InitializeComponent();
            Loaded += (_, _) => Prefill(machine);
        }

        // ── Pré-remplissage ───────────────────────────────────────────────────

        private void Prefill(Machine m)
        {
            // Champs texte
            UsernameBox.Text    = m.EndUserName;
            SciperBox.Text      = m.Sciper;
            AssetNumberBox.Text = m.AssetNumber;
            SerialNumberBox.Text= m.SerialNumber;
            FriendlyNameBox.Text= m.FriendlyName;
            EmailBox.Text       = m.Email;

            // Employee Type
            EmpPersonnel.IsChecked = m.EmployeeType == "Personnel";
            EmpHote.IsChecked      = m.EmployeeType == "Hôte";
            EmpHorsEPFL.IsChecked  = m.EmployeeType == "Hors-EPFL";

            // Device Type
            DevLaptop.IsChecked      = m.DeviceType == "Laptop";
            DevWorkstation.IsChecked = m.DeviceType == "Workstation";
            DevMobile.IsChecked      = m.DeviceType == "Mobile";

            // VPN
            VpnSSC.IsChecked  = m.VpnSelect == "SSC";
            VpnAGA.IsChecked  = m.VpnSelect == "AGA";
            VpnNone.IsChecked = m.VpnSelect != "SSC" && m.VpnSelect != "AGA";

            // FileMaker
            FmTTO.IsChecked    = m.Filemaker == "TTO-AJ";
            FmOHSPR.IsChecked  = m.Filemaker == "OHSPR-DSE";
            FmAutres.IsChecked = m.Filemaker == "Autres";
            FmNone.IsChecked   = m.Filemaker != "TTO-AJ" && m.Filemaker != "OHSPR-DSE" && m.Filemaker != "Autres";

            // Tableau
            TableauDesktop.IsChecked = m.TableauDesktopBool;
            TableauPrep.IsChecked    = m.TableauPrepBool;

            // Options
            MindManager.IsChecked    = m.MindmanagerBool;
            LinaException.IsChecked  = m.LinaExceptionBool;
            AcrobatException.IsChecked = m.AcrobatReaderExceptionBool;

            // État initial AcrobatException
            AcrobatException.IsEnabled = m.EmployeeType != "Personnel";

            UpdateSaveButton();
        }

        // ── Handlers identiques à AddMachineWindow ────────────────────────────

        private void AssetNumberBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FriendlyNameBox.Text = $"SCX-{AssetNumberBox.Text.Trim()}";
            UpdateSaveButton();
        }

        private void RequiredField_TextChanged(object sender, TextChangedEventArgs e)
            => UpdateSaveButton();

        private void EmployeeType_Changed(object sender, RoutedEventArgs e)
        {
            if (AcrobatException != null)
            {
                bool isPersonnel = EmpPersonnel.IsChecked == true;
                AcrobatException.IsEnabled = !isPersonnel;
                if (isPersonnel) AcrobatException.IsChecked = false;
            }
            UpdateSaveButton();
        }

        private void DeviceType_Changed(object sender, RoutedEventArgs e)
            => UpdateSaveButton();

        // ── LDAP email (identique à AddMachineWindow) ─────────────────────────

        private async void LoadEmail_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            if (string.IsNullOrEmpty(username)) return;

            LoadEmailBtn.IsEnabled = false;
            LoadEmailBtn.Content   = "…";
            LdapMessage.Visibility = Visibility.Collapsed;

            var result = await LdapService.FetchEmailAsync(username);

            switch (result.Type)
            {
                case LdapResultType.Found:
                    EmailBox.Text = result.Email;
                    LdapMessage.Visibility = Visibility.Collapsed;
                    break;
                case LdapResultType.NoMail:
                    EmailBox.Text  = "";
                    LdapMessage.Text = "Pas d'email disponible, merci d'en définir un.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
                case LdapResultType.NotFound:
                    EmailBox.Text  = "";
                    LdapMessage.Text = "Le compte n'existe pas dans l'AD.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
                case LdapResultType.Error:
                    EmailBox.Text  = "";
                    LdapMessage.Text = "Erreur lors de la recherche LDAP.";
                    LdapMessage.Visibility = Visibility.Visible;
                    break;
            }

            LoadEmailBtn.Content   = "Load email";
            LoadEmailBtn.IsEnabled = true;
            UpdateSaveButton();
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void UpdateSaveButton()
        {
            bool valid =
                !string.IsNullOrWhiteSpace(UsernameBox?.Text)    &&
                !string.IsNullOrWhiteSpace(SciperBox?.Text)      &&
                !string.IsNullOrWhiteSpace(AssetNumberBox?.Text) &&
                !string.IsNullOrWhiteSpace(SerialNumberBox?.Text)&&
                !string.IsNullOrWhiteSpace(EmailBox?.Text)       &&
                (EmpPersonnel?.IsChecked == true || EmpHote?.IsChecked == true || EmpHorsEPFL?.IsChecked == true) &&
                (DevLaptop?.IsChecked == true || DevWorkstation?.IsChecked == true || DevMobile?.IsChecked == true);

            if (SaveBtn != null) SaveBtn.IsEnabled = valid;
        }

        // ── Enregistrer ───────────────────────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string locationGroupId = "628";
            if (DevWorkstation.IsChecked == true) locationGroupId = "629";
            else if (DevMobile.IsChecked == true) locationGroupId = "627";

            string vpn = VpnSSC.IsChecked == true ? "SSC" : VpnAGA.IsChecked == true ? "AGA" : "";
            string fm  = FmTTO.IsChecked == true ? "TTO-AJ" :
                         FmOHSPR.IsChecked == true ? "OHSPR-DSE" :
                         FmAutres.IsChecked == true ? "Autres" : "";
            string emp = EmpPersonnel.IsChecked == true ? "Personnel" :
                         EmpHote.IsChecked == true ? "Hôte" : "Hors-EPFL";
            string dev = DevLaptop.IsChecked == true ? "Laptop" :
                         DevWorkstation.IsChecked == true ? "Workstation" : "Mobile";

            // Conserve MessageType, PlatformId et Ownership de la machine d'origine
            Result = new Machine
            {
                EndUserName  = UsernameBox.Text.Trim(),
                Sciper       = SciperBox.Text.Trim(),
                AssetNumber  = AssetNumberBox.Text.Trim(),
                SerialNumber = SerialNumberBox.Text.Trim(),
                FriendlyName = FriendlyNameBox.Text,
                Email        = EmailBox.Text.Trim(),
                LocationGroupId = locationGroupId,
                MessageType  = _original.MessageType,
                PlatformId   = _original.PlatformId,
                Ownership    = _original.Ownership,
                EmployeeType = emp,
                DeviceType   = dev,
                VpnSelect    = vpn,
                Filemaker    = fm,
                TableauDesktopBool         = TableauDesktop.IsChecked == true,
                TableauPrepBool            = TableauPrep.IsChecked    == true,
                MindmanagerBool            = MindManager.IsChecked    == true,
                LinaExceptionBool          = LinaException.IsChecked  == true,
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
