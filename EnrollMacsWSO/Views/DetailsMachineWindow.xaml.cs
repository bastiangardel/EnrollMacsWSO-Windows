using EnrollMacsWSO.Models;
using System.Windows;

namespace EnrollMacsWSO.Views
{
    public class InfoRow
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public partial class DetailsMachineWindow : Window
    {
        public DetailsMachineWindow(Machine machine)
        {
            InitializeComponent();
            PopulateInfo(machine);
        }

        private void PopulateInfo(Machine m)
        {
            GeneralInfo.ItemsSource = new[]
            {
                new InfoRow { Label = "Nom d'utilisateur", Value = m.EndUserName },
                new InfoRow { Label = "SCIPER",            Value = m.Sciper },
                new InfoRow { Label = "Numéro d'actif",    Value = m.AssetNumber },
                new InfoRow { Label = "Numéro de série",   Value = m.SerialNumber },
                new InfoRow { Label = "Nom convivial",     Value = m.FriendlyName },
                new InfoRow { Label = "Email",             Value = string.IsNullOrEmpty(m.Email) ? "Non renseigné" : m.Email }
            };

            SelectionInfo.ItemsSource = new[]
            {
                new InfoRow { Label = "Type d'employé",        Value = m.EmployeeType },
                new InfoRow { Label = "Type d'appareil",       Value = m.DeviceType },
                new InfoRow { Label = "VPN Guest",             Value = string.IsNullOrEmpty(m.VpnSelect) ? "Aucune sélection" : m.VpnSelect },
                new InfoRow { Label = "FileMaker",             Value = string.IsNullOrEmpty(m.Filemaker) ? "Aucune sélection" : m.Filemaker },
                new InfoRow { Label = "Tableau Desktop",       Value = m.TableauDesktopBool         ? "Oui" : "Non" },
                new InfoRow { Label = "Tableau Prep",          Value = m.TableauPrepBool            ? "Oui" : "Non" },
                new InfoRow { Label = "MindManager",           Value = m.MindmanagerBool            ? "Oui" : "Non" },
                new InfoRow { Label = "Lina (installée)",      Value = m.LinaExceptionBool          ? "Non" : "Oui" },
                new InfoRow { Label = "Exception Acrobat Pro", Value = m.AcrobatReaderExceptionBool ? "Oui" : "Non" }
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
