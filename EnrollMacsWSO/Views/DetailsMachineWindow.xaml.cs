using EnrollMacsWSO.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnrollMacsWSO.Views
{
    public partial class DetailsMachineWindow : Window
    {
        public DetailsMachineWindow(Machine machine)
        {
            InitializeComponent();
            PopulateInfo(machine);
        }

        private void PopulateInfo(Machine m)
        {
            var general = new[]
            {
                ("Nom d'utilisateur", m.EndUserName),
                ("SCIPER", m.Sciper),
                ("Numéro d'actif", m.AssetNumber),
                ("Numéro de série", m.SerialNumber),
                ("Nom convivial", m.FriendlyName),
                ("Email", string.IsNullOrEmpty(m.Email) ? "Non renseigné" : m.Email)
            };

            var selections = new[]
            {
                ("Type d'employé",       m.EmployeeType),
                ("Type d'appareil",      m.DeviceType),
                ("VPN Guest",            string.IsNullOrEmpty(m.VpnSelect) ? "Aucune sélection" : m.VpnSelect),
                ("FileMaker",            string.IsNullOrEmpty(m.Filemaker) ? "Aucune sélection" : m.Filemaker),
                ("Tableau Desktop",      m.TableauDesktopBool ? "Oui" : "Non"),
                ("Tableau Prep",         m.TableauPrepBool    ? "Oui" : "Non"),
                ("MindManager",          m.MindmanagerBool    ? "Oui" : "Non"),
                ("Lina (installée)",     m.LinaExceptionBool  ? "Non" : "Oui"),
                ("Exception Acrobat Pro",m.AcrobatReaderExceptionBool ? "Oui" : "Non")
            };

            GeneralInfo.ItemsSource   = BuildRows(general);
            SelectionInfo.ItemsSource = BuildRows(selections);

            GeneralInfo.ItemTemplate   = RowTemplate();
            SelectionInfo.ItemTemplate = RowTemplate();
        }

        private static List<(string Label, string Value)> BuildRows(
            IEnumerable<(string, string)> items) => items.ToList();

        private static DataTemplate RowTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(245, 245, 245)));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            factory.SetValue(Border.MarginProperty, new Thickness(0, 2, 0, 2));
            factory.SetValue(Border.PaddingProperty, new Thickness(8, 5, 8, 5));

            var grid = new FrameworkElementFactory(typeof(Grid));

            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(160));
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            grid.AppendChild(col1);

            var label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            label.SetValue(TextBlock.FontSizeProperty, 13.0);
            label.SetBinding(TextBlock.TextProperty,
                new System.Windows.Data.Binding("Item1"));
            grid.AppendChild(label);

            var value = new FrameworkElementFactory(typeof(TextBlock));
            value.SetValue(Grid.ColumnProperty, 1);
            value.SetValue(TextBlock.FontSizeProperty, 13.0);
            value.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            value.SetBinding(TextBlock.TextProperty,
                new System.Windows.Data.Binding("Item2"));
            grid.AppendChild(value);

            factory.AppendChild(grid);
            return new DataTemplate { VisualTree = factory };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
