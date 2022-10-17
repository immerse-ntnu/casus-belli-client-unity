using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Immerse.BfHClient.EditTests
{
    public class JsonDeserializationTests
    {
        private RegionLookUp _regionLookUp;

        [SetUp]
        public void Setup()
        {
            var json = Resources.Load<TextAsset>("province_data");
            _regionLookUp = new RegionLookUp(json.text);
        }

        [Test]
        public void RegionHasCorrectAmountOfNeighbours()
        {
            var region = _regionLookUp.GetRegionFromName("Bassas");
            Assert.AreEqual(5, region.Neighbours.Count);
        }

        [Test]
        public void RetrievesRegionWithCorrectName()
        {
            var region = _regionLookUp.GetRegionFromName("Bassas");
            Assert.AreEqual("Bassas", region.Name);
        }

        [Test]
        public void RetrievesRegionWithCorrectFancyName()
        {
            var region = _regionLookUp.GetRegionFromName("Monté");
            Assert.AreEqual("Monté", region.Name);
        }

        [Test]
        public void RetrievesRegionWithCorrectColor()
        {
            var color = new Color32(0, 95, 0, 255);
            Assert.AreEqual("Samoje", _regionLookUp.GetRegionFromColor(color).Name);
        }

        [Test]
        public void AllRegionsHaveNeighbours()
        {
            var regions = _regionLookUp.GetFieldValue<Dictionary<Color32, Region>>("_regions");
            Assert.IsNotEmpty(regions);
            foreach (var pair in regions)
                Assert.IsTrue(pair.Value.Neighbours.Count > 0);
        }
    }
}