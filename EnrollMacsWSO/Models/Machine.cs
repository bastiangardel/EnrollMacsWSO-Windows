using Newtonsoft.Json;

namespace EnrollMacsWSO.Models
{
    public class Machine
    {
        [JsonIgnore]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonProperty("EndUserName")]
        public string EndUserName { get; set; } = "";

        [JsonProperty("AssetNumber")]
        public string AssetNumber { get; set; } = "";

        [JsonProperty("LocationGroupId")]
        public string LocationGroupId { get; set; } = "";

        [JsonProperty("MessageType")]
        public int MessageType { get; set; }

        [JsonProperty("SerialNumber")]
        public string SerialNumber { get; set; } = "";

        [JsonProperty("PlatformId")]
        public int PlatformId { get; set; }

        [JsonProperty("FriendlyName")]
        public string FriendlyName { get; set; } = "";

        [JsonProperty("Ownership")]
        public string Ownership { get; set; } = "";

        [JsonProperty("employeetypemacssc")]
        public string EmployeeType { get; set; } = "";

        [JsonProperty("vpnguestmacssc")]
        public string VpnSelect { get; set; } = "";

        [JsonProperty("tableauDesktopmacssc")]
        public int TableauDesktop { get; set; }

        [JsonProperty("tableauPrepmacssc")]
        public int TableauPrep { get; set; }

        [JsonProperty("filemakermacssc")]
        public string Filemaker { get; set; } = "";

        [JsonProperty("mindmanagermacssc")]
        public int Mindmanager { get; set; }

        [JsonProperty("linaexceptionssc")]
        public int LinaException { get; set; }

        [JsonProperty("acrobatreaderexceptionssc")]
        public int AcrobatReaderException { get; set; }

        [JsonProperty("devicetypemacssc")]
        public string DeviceType { get; set; } = "";

        [JsonProperty("SCIPER")]
        public string Sciper { get; set; } = "";

        [JsonProperty("MailAddress")]
        public string Email { get; set; } = "";

        [JsonIgnore]
        public bool TableauDesktopBool
        {
            get => TableauDesktop != 0;
            set => TableauDesktop = value ? 1 : 0;
        }

        [JsonIgnore]
        public bool TableauPrepBool
        {
            get => TableauPrep != 0;
            set => TableauPrep = value ? 1 : 0;
        }

        [JsonIgnore]
        public bool MindmanagerBool
        {
            get => Mindmanager != 0;
            set => Mindmanager = value ? 1 : 0;
        }

        [JsonIgnore]
        public bool LinaExceptionBool
        {
            get => LinaException != 0;
            set => LinaException = value ? 1 : 0;
        }

        [JsonIgnore]
        public bool AcrobatReaderExceptionBool
        {
            get => AcrobatReaderException != 0;
            set => AcrobatReaderException = value ? 1 : 0;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
