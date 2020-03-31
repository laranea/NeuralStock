namespace twentySix.NeuralStock.Core.Models
{
    using System;

    public enum TransactionEnum
    {
        Buy, Sell
    }

    public class Trade
    {
        public DateTime Date { get; set; }

        public TransactionEnum Type { get; set; }

        public Stock Stock { get; set; }

        public int NumberOfShares { get; set; }

        public double Price { get; set; }

        public double Fees => this.Stock.Country.GetFees(this.Price * this.NumberOfShares);

        public double TotalValue => ((this.Type == TransactionEnum.Sell ? -1d : 1d) * this.NumberOfShares * this.Price) + this.Fees;
    }
}