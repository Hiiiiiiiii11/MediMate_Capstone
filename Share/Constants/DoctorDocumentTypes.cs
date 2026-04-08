namespace Share.Constants
{
    public static class DoctorDocumentTypes
    {
        public const string PracticeLicense = "PRACTICE_LICENSE";
        public const string SpecialistCertificate = "SPECIALIST_CERTIFICATE";
        public const string Cme = "CME";
        public const string Other = "OTHER";

        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            PracticeLicense,
            SpecialistCertificate,
            Cme,
            Other
        };
    }
}
