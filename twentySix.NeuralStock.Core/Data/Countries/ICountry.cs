namespace twentySix.NeuralStock.Core.Data.Countries
{
    public interface ICountry
    {
        int Id { get; }

        string Name { get; }

        double GetFees(double contractValue);
    }
}