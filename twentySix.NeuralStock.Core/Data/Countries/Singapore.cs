namespace twentySix.NeuralStock.Core.Data.Countries
{
    using System.ComponentModel.Composition;

    [Export(typeof(ICountry))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Singapore : ICountry
    {
        public static int CountryId => 1;

        public int Id => CountryId;

        public string Name => "Singapore";

        public double GetFees(double contractValue)
        {
            var minCommission = 25d;
            var commissionPercent = 0d;
            if (contractValue < 50000d)
            {
                commissionPercent = 0.275d;
            }
            else if (contractValue >= 50000d && contractValue < 100000d)
            {
                commissionPercent = 0.22d;
            }
            else if (contractValue >= 100000d)
            {
                commissionPercent = 0.20d;
            }

            var clearingFee = 0.0325d;
            var tradingAccessFee = 0.0075d;
            var gst = 7d;
            var fees = contractValue * (commissionPercent + clearingFee + tradingAccessFee) / 100d;
            if (fees < minCommission)
            {
                fees = minCommission;
            }

            fees *= 1d + (gst / 100d);
            return fees;
        }
    }
}