namespace SWWerkplaats.Configurator.Domain
{
    public sealed class MachineProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double MaxXmm { get; set; }
        public double MaxYmm { get; set; }
        public string FileExtension { get; set; }
        public double SafeZmm { get; set; }
        public string Origin { get; set; }
    }
}
