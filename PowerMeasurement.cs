namespace Supla
{
    public class Measurement
    {
        public int Number { get; set; }
        public Device Device { get; set; }
        public DateTime Date { get; set; }

        public double Phase1FAE { get; set; }
        public double Phase1RAE { get; set; }
        public double Phase1FRE { get; set; }
        public double Phase1RRE { get; set; }

        public double Phase2FAE { get; set; }
        public double Phase2RAE { get; set; }
        public double Phase2FRE { get; set; }
        public double Phase2RRE { get; set; }

        public double Phase3FAE { get; set; }
        public double Phase3RAE { get; set; }
        public double Phase3FRE { get; set; }
        public double Phase3RRE { get; set; }

        public double SumVFAE { get; set; }
        public double SumVRAE { get; set; }
        public double SumFAE { get; set; }
        public double SumRAE { get; set; }
        public double SumFRE { get; set; }
        public double SumRRE { get; set; }
    }
}
