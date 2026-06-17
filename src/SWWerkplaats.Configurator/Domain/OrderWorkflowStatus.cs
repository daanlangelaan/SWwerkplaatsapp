namespace SWWerkplaats.Configurator.Domain
{
    public static class OrderWorkflowStatus
    {
        public const string Nieuw = "Nieuw";
        public const string TeControleren = "Te controleren";
        public const string Goedgekeurd = "Goedgekeurd";
        public const string InFreeswachtrij = "In freeswachtrij";
        public const string InProductie = "In productie";
        public const string Gereed = "Gereed";

        public static string[] All()
        {
            return new[]
            {
                Nieuw,
                TeControleren,
                Goedgekeurd,
                InFreeswachtrij,
                InProductie,
                Gereed
            };
        }
    }
}
