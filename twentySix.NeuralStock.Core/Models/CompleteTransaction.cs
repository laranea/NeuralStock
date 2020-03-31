namespace twentySix.NeuralStock.Core.Models
{
    public class CompleteTransaction
    {
        public CompleteTransaction(Trade buyTrade, Trade sellTrade)
        {
            this.BuyTrade = buyTrade;
            this.SellTrade = sellTrade;
        }

        public Trade BuyTrade { get; }

        public Trade SellTrade { get; }

        public double PL => -(SellTrade.TotalValue + BuyTrade.TotalValue);
    }
}