using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Immerse.BfHClient.EditTests
{
    public class JsonDeserializationTests
    {
        private RegionHandler _regionHandler;

        [SetUp]
        public void Setup()
        {
            var json = Resources.Load<TextAsset>("province_data");
            //TODO
            //_regionHandler = new RegionHandler(json.text);
        }

        [Test]
        public void RegionHasCorrectAmountOfNeighbours()
        {
            var region = _regionHandler.GetRegionFromName("Bassas");
            Assert.AreEqual(5, region.Neighbours.Count);
        }

        [Test]
        public void RetrievesRegionWithCorrectName()
        {
            var region = _regionHandler.GetRegionFromName("Bassas");
            Assert.AreEqual("Bassas", region.Name);
        }

        [Test]
        public void RetrievesRegionWithCorrectFancyName()
        {
            var region = _regionHandler.GetRegionFromName("Monté");
            Assert.AreEqual("Monté", region.Name);
        }

        [Test]
        public void RetrievesRegionWithCorrectColor()
        {
            var color = new Color32(0, 95, 0, 255);
            Assert.AreEqual("Samoje", _regionHandler.GetRegionFromColor(color).Name);
        }

        [Test]
        public void AllRegionsHaveNeighbours()
        {
            var regions = _regionHandler.GetFieldValue<Dictionary<Color32, Region>>("_regions");
            Assert.IsNotEmpty(regions);
            foreach (var pair in regions)
                Assert.IsTrue(pair.Value.Neighbours.Count > 0);
        }
    }
}