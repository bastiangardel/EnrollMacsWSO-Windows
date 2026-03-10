using EnrollMacsWSO.Models;
using EnrollMacsWSO.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnrollMacsWSO.Views
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Machine> _machines = new();
        private string _sortKey = "friendlyName";
        private bool _sortAscending = true;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
            MachineListView.ItemsSource = _machines;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTestModeUI();

            // Brancher le clic sur les en-têtes de colonnes via AddHandler
            MachineListView.AddHandler(
                GridViewColumnHeader.ClickEvent,
                new RoutedEventHandler(GridHeader_Click));

            // First-run: show config if not yet configured
            var cfg = ConfigManager.Instance.Load();
            if (!cfg.IsConfigured)
                ShowConfigDialog();
        }

        // ── Test Mode ────────────────────────────────────────────────────────

        private void TestModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            bool isTest = TestModeToggle.IsChecked == true;
            ConfigManager.Instance.SetTestMode(isTest);
            RefreshTestModeUI();
        }

        private void RefreshTestModeUI()
        {
            bool isTest = ConfigManager.Instance.IsTestMode;
            TestModeToggle.IsChecked = isTest;
            TestModeBadge.Visibility = isTest ? Visibility.Visible : Visibility.Collapsed;
            TitleText.Text = isTest
                ? "TEST - Affectation des machines dans WSO"
                : "Affectation des machines dans WSO";
            Title = TitleText.Text;
        }

        // ── Sorting ───────────────────────────────────────────────────────────

        private void GridHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header) return;

            string? key = null;
            if      (header.Column == ColFriendlyName)  key = "friendlyName";
            else if (header.Column == ColEndUserName)   key = "endUserName";
            else if (header.Column == ColAssetNumber)   key = "assetNumber";
            else if (header.Column == ColLocationGroup) key = "locationGroupId";
            else if (header.Column == ColSerial)        key = "serialNumber";

            if (key != null) SortMachines(key);
        }

        private void SortMachines(string key)
        {
            if (_sortKey == key)
                _sortAscending = !_sortAscending;
            else
            {
                _sortKey = key;
                _sortAscending = true;
            }

            var sorted = key switch
            {
                "friendlyName" => _sortAscending
                    ? _machines.OrderBy(m => m.FriendlyName, StringComparer.OrdinalIgnoreCase)
                    : _machines.OrderByDescending(m => m.FriendlyName, StringComparer.OrdinalIgnoreCase),
                "endUserName" => _sortAscending
                    ? _machines.OrderBy(m => m.EndUserName, StringComparer.OrdinalIgnoreCase)
                    : _machines.OrderByDescending(m => m.EndUserName, StringComparer.OrdinalIgnoreCase),
                "assetNumber" => _sortAscending
                    ? _machines.OrderBy(m => m.AssetNumber, StringComparer.OrdinalIgnoreCase)
                    : _machines.OrderByDescending(m => m.AssetNumber, StringComparer.OrdinalIgnoreCase),
                "locationGroupId" => _sortAscending
                    ? _machines.OrderBy(m => m.LocationGroupId)
                    : _machines.OrderByDescending(m => m.LocationGroupId),
                "serialNumber" => _sortAscending
                    ? _machines.OrderBy(m => m.SerialNumber, StringComparer.OrdinalIgnoreCase)
                    : _machines.OrderByDescending(m => m.SerialNumber, StringComparer.OrdinalIgnoreCase),
                _ => (IOrderedEnumerable<Machine>)_machines.OrderBy(m => m.FriendlyName)
            };

            var list = sorted.ToList();
            _machines.Clear();
            foreach (var m in list) _machines.Add(m);
        }

        // ── Buttons ───────────────────────────────────────────────────────────

        private void AddMachine_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddMachineWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                _machines.Add(dlg.Result);
                SortMachines(_sortKey);
                ShowStatus($"Machine '{dlg.Result.FriendlyName}' ajoutée avec succès !");
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CsvImportWindow { Owner = this };
            if (dlg.ShowDialog() == true && dlg.ImportedMachines != null)
            {
                foreach (var m in dlg.ImportedMachines)
                    _machines.Add(m);
                SortMachines(_sortKey);
                ShowStatus($"{dlg.ImportedMachines.Count} machine(s) importée(s) avec succès !");
            }
        }

        private void DetailsMachine_Click(object sender, RoutedEventArgs e)
        {
            if (MachineListView.SelectedItems.Count == 1
                && MachineListView.SelectedItem is Machine m)
                ShowDetails(m);
        }

        private void MachineListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MachineListView.SelectedItems.Count == 1
                && MachineListView.SelectedItem is Machine m)
                ShowDetails(m);
        }

        private void ShowDetails(Machine m)
        {
            var dlg = new DetailsMachineWindow(m) { Owner = this };
            dlg.ShowDialog();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var toRemove = MachineListView.SelectedItems.Cast<Machine>().ToList();
            foreach (var m in toRemove) _machines.Remove(m);
            ShowStatus($"{toRemove.Count} machine(s) supprimée(s).");
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Supprimer toutes les machines de la liste ?",
                "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _machines.Clear();
                ShowStatus("Toutes les machines ont été supprimées.");
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (_machines.Count == 0) { ShowStatus("Aucune machine à envoyer."); return; }

            // Windows credential prompt via UAC / login dialog
            bool authenticated = AuthenticateUser();
            if (!authenticated)
            {
                ShowStatus("Authentification annulée ou échouée.");
                return;
            }

            SetProcessing(true);
            ProgressPanel.Visibility = Visibility.Visible;
            SendProgressBar.Value = 0;

            var machineList = _machines.ToList();
            int total = machineList.Count;
            int successCount = 0;
            var failed = new List<Machine>();
            var messages = new List<string>();

            for (int i = 0; i < total; i++)
            {
                var machine = machineList[i];
                byte[] jsonBytes = Encoding.UTF8.GetBytes(machine.ToJson());
                string filename = $"scx-{machine.AssetNumber}.json";

                var (success, msg) = await SambaService.SaveFileAsync(filename, jsonBytes);
                if (success) successCount++;
                else { failed.Add(machine); messages.Add(msg); }

                double pct = (double)(i + 1) / total * 100;
                SendProgressBar.Value = pct;
                ProgressLabel.Text = $"Progression : {(int)pct}%";
            }

            // Remove successfully sent machines
            foreach (var m in machineList.Except(failed))
                _machines.Remove(m);

            SetProcessing(false);
            ProgressPanel.Visibility = Visibility.Collapsed;

            string summary = $"{successCount} fichier(s) envoyé(s) sur {total}.";
            if (messages.Count > 0) summary += "\n" + string.Join("\n", messages.Take(3));
            ShowStatus(summary);
        }

        private void EditConfig_Click(object sender, RoutedEventArgs e) => ShowConfigDialog();

        private void CloseApp_Click(object sender, RoutedEventArgs e) => Close();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowConfigDialog()
        {
            var dlg = new ConfigurationWindow { Owner = this };
            dlg.ShowDialog();
            RefreshTestModeUI();
        }

        private bool AuthenticateUser()
        {
            // On Windows, we use a simple password dialog as credential confirmation.
            // For stronger auth, Windows Hello / Windows Security can be invoked via
            // Windows.Security.Credentials.UI.UserConsentVerifier (requires WinRT reference).
            var dlg = new CredentialPromptWindow { Owner = this };
            return dlg.ShowDialog() == true;
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            AddMachineBtn.IsEnabled = !processing;
            ImportCsvBtn.IsEnabled = !processing;
            DetailsBtn.IsEnabled = !processing && MachineListView.SelectedItems.Count == 1;
            DeleteSelectedBtn.IsEnabled = !processing && MachineListView.SelectedItems.Count > 0;
            DeleteAllBtn.IsEnabled = !processing && _machines.Count > 0;
            SendBtn.IsEnabled = !processing && _machines.Count > 0;
            EditConfigBtn.IsEnabled = !processing;
        }

        private System.Threading.CancellationTokenSource? _statusCts;
        private async void ShowStatus(string message, double seconds = 6)
        {
            _statusCts?.Cancel();
            _statusCts = new System.Threading.CancellationTokenSource();
            var token = _statusCts.Token;

            StatusText.Text = message;
            StatusBorder.Visibility = Visibility.Visible;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), token);
                StatusBorder.Visibility = Visibility.Collapsed;
            }
            catch (TaskCanceledException) { /* superseded by a newer message */ }
        }
    }
}
