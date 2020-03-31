namespace twentySix.NeuralStock.CoreTests.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Models;

    [TestFixture]
    public class StockPortfolioTests
    {
        private StockPortfolio _stockPortfolio;
        private Stock _stock;

        [SetUp]
        public void SetUp()
        {
            this._stockPortfolio = new StockPortfolio();

            var historical = new HistoricalData(
                new SortedList<DateTime, Quote>(
                    Enumerable.Range(1, 20).ToDictionary(
                        x => DateTime.Today.AddDays(-(x - 1)),
                        x => new Quote
                        {
                            Date = DateTime.Today.AddDays(-(x - 1)),
                            Open = x * 10,
                            High = x * 14,
                            Close = x * 1.5,
                            Low = x * 8
                        })));

            this._stock = new Stock
            {
                Country = new Singapore(),
                Symbol = "C31",
                HistoricalData = historical
            };
        }

        [TearDown]
        public void TearDown()
        {
            this._stockPortfolio = null;
            this._stock = null;
        }

        [Test]
        public void GetCash_NoCash_ReturnsZero()
        {
            var result = this._stockPortfolio.GetCash(DateTime.Today);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetCash_OneCredit_ReturnsCash()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-1), 26d);

            var past = this._stockPortfolio.GetCash(DateTime.Today.AddDays(-10));
            var actual = this._stockPortfolio.GetCash(DateTime.Today);
            var future = this._stockPortfolio.GetCash(DateTime.Today.AddDays(10));

            Assert.AreEqual(0d, past);
            Assert.AreEqual(26d, actual);
            Assert.AreEqual(26d, future);
        }

        [Test]
        public void GetCash_OneCreditOneDebit_ReturnsCash()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 26d);
            this._stockPortfolio.Add(DateTime.Today, -12d);

            var past = this._stockPortfolio.GetCash(DateTime.Today.AddDays(-10));
            var past2 = this._stockPortfolio.GetCash(DateTime.Today.AddDays(-3));
            var actual = this._stockPortfolio.GetCash(DateTime.Today);
            var future = this._stockPortfolio.GetCash(DateTime.Today.AddDays(10));

            Assert.AreEqual(0d, past);
            Assert.AreEqual(26d, past2);
            Assert.AreEqual(14d, actual);
            Assert.AreEqual(14d, future);
        }

        [Test]
        public void AddCash_NotEnough_ExceptionRaised()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 26d);

            Assert.That(() => this._stockPortfolio.Add(DateTime.Today, -100d), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AddBuyTrade_EnoughCash_Added()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var trade = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(trade);

            Assert.IsTrue(this._stockPortfolio.GetCash(DateTime.Today) < 1000d);
            Assert.IsTrue(this._stockPortfolio.CashTransactions.Count == 2);
            Assert.IsTrue(this._stockPortfolio.Trades.Count == 1);
        }

        [Test]
        public void AddSellTrade_EnoughCash_Added()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var trade = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Sell,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(trade);

            Assert.IsTrue(this._stockPortfolio.GetCash(DateTime.Today) > 1000d);
            Assert.IsTrue(this._stockPortfolio.CashTransactions.Count == 2);
            Assert.IsTrue(this._stockPortfolio.Trades.Count == 1);
        }

        [Test]
        public void BuyAndSellTrade_EnoughCash_Executed()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var sell = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Sell,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(sell);

            Assert.IsTrue(this._stockPortfolio.GetCash(DateTime.Today) < 1000d);
            Assert.IsTrue(this._stockPortfolio.CashTransactions.Count == 3);
            Assert.IsTrue(this._stockPortfolio.Trades.Count == 2);
        }

        [Test]
        public void GetMaxPurchaseVolume_EnoughCash_ReturnsVolume()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var result = this._stockPortfolio.GetMaxPurchaseVolume(this._stock, DateTime.Today, 10d);

            Assert.IsTrue(result > 50);
        }

        [Test]
        public void GetMaxPurchaseVolume_NotEnoughCash_ReturnsZeroVolume()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 100d);

            var result = this._stockPortfolio.GetMaxPurchaseVolume(this._stock, DateTime.Today, 120d);

            Assert.IsTrue(result == 0);
        }

        [Test]
        public void GetHoldings_NoTrades_ReturnsEmpty()
        {
            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsFalse(result.Any());
        }

        [Test]
        public void GetHoldings_OneBuyTrade_ReturnsPositiveHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 1);
            Assert.IsTrue(result.ContainsKey(this._stock));
            Assert.AreEqual(10, result[this._stock]);
        }

        [Test]
        public void GetHoldings_TwoBuyTrades_ReturnsPositiveHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-2),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            buy = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 14,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 2);
            Assert.IsTrue(result.ContainsKey(this._stock));
            Assert.AreEqual(24, result[this._stock]);
        }

        [Test]
        public void GetHoldings_OneSellTrade_ReturnsNegativeHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-2),
                Stock = this._stock,
                Type = TransactionEnum.Sell,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 1);
            Assert.IsTrue(result.ContainsKey(this._stock));
            Assert.AreEqual(-10, result[this._stock]);
        }

        [Test]
        public void GetHoldings_BuySellSameVolume_ReturnsPositiveHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-2),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var sell = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Sell,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(sell);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 2);
            Assert.IsFalse(result.Any());
        }

        [Test]
        public void GetHoldings_BuySellPartial_ReturnsPositiveHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-2),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var sell = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Sell,
                NumberOfShares = 6,
                Price = 12
            };

            this._stockPortfolio.Add(sell);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 2);
            Assert.IsTrue(result.ContainsKey(this._stock));
            Assert.AreEqual(4, result[this._stock]);
        }

        [Test]
        public void GetHoldings_TwoTrades_ReturnsPositiveHolding()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 1000d);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-1),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 10,
                Price = 12
            };

            this._stockPortfolio.Add(buy);

            var stock2 = new Stock { Country = new Singapore(), Symbol = "S58" };
            buy = new Trade
            {
                Date = DateTime.Today.AddDays(-5),
                Stock = stock2,
                Type = TransactionEnum.Buy,
                NumberOfShares = 14,
                Price = 9
            };

            this._stockPortfolio.Add(buy);

            var result = this._stockPortfolio.GetHoldings(DateTime.Today);

            Assert.IsTrue(this._stockPortfolio.Trades.Count == 2);
            Assert.IsTrue(result.ContainsKey(this._stock));
            Assert.IsTrue(result.ContainsKey(stock2));
            Assert.AreEqual(10, result[this._stock]);
            Assert.AreEqual(14, result[stock2]);
        }

        [Test]
        public void GetValue_Nothing_ReturnsZero()
        {
            var result = this._stockPortfolio.GetValue(DateTime.Today);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetValue_OnlyCash_ReturnsCashValue()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-2), 10);

            var result = this._stockPortfolio.GetValue(DateTime.Today);

            Assert.AreEqual(10, result);
        }

        [Test]
        public void GetValue_CashWithStocks_ReturnsValue()
        {
            this._stockPortfolio.Add(DateTime.Today.AddDays(-5), 10000);

            var buy = new Trade
            {
                Date = DateTime.Today.AddDays(-2),
                Stock = this._stock,
                Type = TransactionEnum.Buy,
                NumberOfShares = 3000,
                Price = 1.3
            };

            this._stockPortfolio.Add(buy);

            var result = this._stockPortfolio.GetValue(DateTime.Today);

            Assert.AreEqual(10546.5d, result);
        }
    }
}