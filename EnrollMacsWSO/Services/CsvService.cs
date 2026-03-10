using CsvHelper;
using CsvHelper.Configuration;
using EnrollMacsWSO.Models;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace EnrollMacsWSO.Services
{
    public static class CsvService
    {
        // ── Parse helpers ─────────────────────────────────────────────────────

        public static List<Dictionary<string, string>> ParseCsv(string filePath)
        {
            var result = new List<Dictionary<string, string>>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
                PrepareHeaderForMatch = args =>
                    args.Header.Trim()
                        .Replace("\uFEFF", "")
                        .ToLowerInvariant()
            };

            using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();

            while (csv.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in headers)
                    row[h] = csv.GetField(h) ?? "";
                result.Add(row);
            }
            return result;
        }

        public static void ExportCsv(List<Dictionary<string, string>> data, string filePath)
        {
            if (data.Count == 0) return;
            var headers = data[0].Keys.ToList();

            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine(string.Join(",", headers));
            foreach (var row in data)
                writer.WriteLine(string.Join(",", headers.Select(h => EscapeCsvField(row.TryGetValue(h, out var v) ? v : ""))));
        }

        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }

        // ── Main processing (mirrors processCSVData in Swift) ─────────────────

        public static List<Machine> ProcessCsvData(
            List<Dictionary<string, string>> nameData,
            List<Dictionary<string, string>> ocsData,
            List<Dictionary<string, string>> inventoryData,
            string missingOutputPath,
            string doublonsOutputPath)
        {
            var machines = new List<Machine>();
            var missingResults = new List<Dictionary<string, string>>();
            var doublonsResults = new List<Dictionary<string, string>>();

            var config = ConfigManager.Instance.Load();
            string locationGroupId = "628";
            int platformId = config.PlatformId;
            int messageType = config.MessageType;
            string ownership = config.Ownership;

            // Step 1: match ocs rows against name rows
            var nameToComputerMatches = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var results = new List<Dictionary<string, string>>();

            foreach (var ocsRow in ocsData)
            {
                if (!ocsRow.TryGetValue("computername", out var computerName) || string.IsNullOrEmpty(computerName)) continue;
                if (!ocsRow.TryGetValue("serialnumber", out var serialNumber) || string.IsNullOrEmpty(serialNumber)) continue;
                if (!ocsRow.TryGetValue("username", out var userName)) continue;

                foreach (var nameRow in nameData)
                {
                    if (!nameRow.TryGetValue("name", out var name) || string.IsNullOrEmpty(name)) continue;

                    // Build regex: checks if computerName contains all letters of `name` in order (case-insensitive)
                    string pattern = string.Join(".*",
                        name.Select(c => Regex.Escape(c.ToString())));

                    if (Regex.IsMatch(computerName, pattern, RegexOptions.IgnoreCase))
                    {
                        if (!nameToComputerMatches.ContainsKey(name))
                            nameToComputerMatches[name] = new List<string>();
                        nameToComputerMatches[name].Add(computerName);

                        results.Add(new Dictionary<string, string>
                        {
                            ["computername"] = computerName,
                            ["username"] = userName,
                            ["serialnumber"] = serialNumber
                        });
                    }
                }
            }

            // Detect duplicates and missing
            foreach (var kvp in nameToComputerMatches)
            {
                if (kvp.Value.Count > 1)
                    foreach (var dup in kvp.Value)
                        doublonsResults.Add(new Dictionary<string, string> { ["computername"] = dup, ["name"] = kvp.Key });
            }

            foreach (var nameRow in nameData)
            {
                if (!nameRow.TryGetValue("name", out var name) || string.IsNullOrEmpty(name)) continue;
                if (!nameToComputerMatches.ContainsKey(name))
                    missingResults.Add(new Dictionary<string, string> { ["name"] = name });
            }

            if (missingResults.Count > 0) ExportCsv(missingResults, missingOutputPath);
            if (doublonsResults.Count > 0) ExportCsv(doublonsResults, doublonsOutputPath);

            // Step 2: match results against inventory
            foreach (var result in results)
            {
                string sourceSerial = result["serialnumber"];
                string sourceComputerName = result["computername"];
                string sourceUserName = result["username"];
                string serialLast6 = sourceSerial.Length >= 6 ? sourceSerial[^6..] : sourceSerial;

                var matchingInventory = inventoryData.Where(row =>
                    row.TryGetValue("serialnumber", out var inv) &&
                    inv.Length >= 6 && inv[^6..] == serialLast6).ToList();

                foreach (var invRow in matchingInventory)
                {
                    if (!invRow.TryGetValue("inventorynumber", out var inventoryNumber)) continue;

                    machines.Add(new Machine
                    {
                        EndUserName = sourceUserName,
                        AssetNumber = inventoryNumber,
                        LocationGroupId = locationGroupId,
                        MessageType = messageType,
                        SerialNumber = sourceSerial,
                        PlatformId = platformId,
                        FriendlyName = sourceComputerName,
                        Ownership = ownership
                    });
                }
            }

            return machines;
        }
    }
}
