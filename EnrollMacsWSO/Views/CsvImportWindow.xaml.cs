using EnrollMacsWSO.Models;
using EnrollMacsWSO.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace EnrollMacsWSO.Views
{
    public partial class CsvImportWindow : Window
    {
        public List<Machine>? ImportedMachines { get; private set; }

        private string? _namePath, _ocsPath, _inventoryPath, _missingPath, _doublonsPath;

        public CsvImportWindow() => InitializeComponent();

        // ── File pickers ──────────────────────────────────────────────────────

        private void PickName_Click(object sender, RoutedEventArgs e) =>
            Pick(ref _namePath, NameCheck, UpdateImportButton);

        private void PickOcs_Click(object sender, RoutedEventArgs e) =>
            Pick(ref _ocsPath, OcsCheck, UpdateImportButton);

        private void PickInventory_Click(object sender, RoutedEventArgs e) =>
            Pick(ref _inventoryPath, InventoryCheck, UpdateImportButton);

        private void PickMissing_Click(object sender, RoutedEventArgs e) =>
            SavePick(ref _missingPath, "missing.csv", MissingCheck);

        private void PickDoublons_Click(object sender, RoutedEventArgs e) =>
            SavePick(ref _doublonsPath, "doublons.csv", DoublonsCheck);

        private void Pick(ref string? field, TextBlock check, Action callback)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == true)
            {
                field = dlg.FileName;
                check.Visibility = Visibility.Visible;
                callback();
            }
        }

        private void SavePick(ref string? field, string defaultName, TextBlock check)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Fichiers CSV (*.csv)|*.csv",
                FileName = defaultName
            };
            if (dlg.ShowDialog() == true)
            {
                field = dlg.FileName;
                check.Visibility = Visibility.Visible;
                UpdateImportButton();
            }
        }

        private void UpdateImportButton()
        {
            ImportBtn.IsEnabled =
                _namePath != null &&
                _ocsPath != null &&
                _inventoryPath != null &&
                _missingPath != null &&
                _doublonsPath != null;
        }

        // ── Import ────────────────────────────────────────────────────────────

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            try
            {
                var nameData = CsvService.ParseCsv(_namePath!);
                var ocsData = CsvService.ParseCsv(_ocsPath!);
                var inventoryData = CsvService.ParseCsv(_inventoryPath!);

                ImportedMachines = CsvService.ProcessCsvData(
                    nameData, ocsData, inventoryData,
                    _missingPath!, _doublonsPath!);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Erreur lors du traitement : {ex.Message}";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
