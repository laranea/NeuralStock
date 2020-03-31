namespace twentySix.NeuralStock.Core.Services.Tests
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Services.Interfaces;

    [TestFixture]
    public class StatisticsServiceTests
    {
        private IStatisticsService _statisticsService;
        private double[] _testData;

        [SetUp]
        public void SetUp()
        {
            this._statisticsService = new StatisticsService();
            this._testData = Enumerable.Range(1, 26).Select(Convert.ToDouble).ToArray();
        }

        [TearDown]
        public void TearDown()
        {
            this._statisticsService = null;
            this._testData = null;
        }

        [Test]
        public void Mean_ReturnsMean()
        {
            var mean = this._statisticsService.Mean(this._testData);

            Assert.AreEqual(13.5d, mean);
        }

        [Test]
        public void StdDev_ReturnsStdDev()
        {
            var std = this._statisticsService.StandardDeviation(this._testData);

            Assert.IsTrue(std - 7.6485 < 0.001);
        }
    }
}