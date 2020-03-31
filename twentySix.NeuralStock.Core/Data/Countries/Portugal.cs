namespace twentySix.NeuralStock.Core.Data.Countries
{
    using System.ComponentModel.Composition;

    [Export(typeof(ICountry))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Portugal : ICountry
    {
        public static int CountryId => 2;

        public int Id => CountryId;

        public string Name => "Portugal";

        public double GetFees(double contractValue)
        {
            var selo = 4d;
            var taxa = contractValue < 10000d ? 9d : 0.001d * contractValue;
            taxa *= 1d + (selo / 100d);
            return taxa;
        }
    }
}