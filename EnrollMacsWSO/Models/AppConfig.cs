namespace EnrollMacsWSO.Models
{
    public class AppConfig
    {
        public int PlatformId { get; set; } = 12;
        public string Ownership { get; set; } = "C";
        public int MessageType { get; set; } = 0;
        public string SambaPath { get; set; } = "";
        public string SambaUsername { get; set; } = "";
        public bool IsTestMode { get; set; } = false;
        public bool IsConfigured { get; set; } = false;
    }
}
