using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    // Pins ElevationMap: per-tile height with level 0 as the implicit default.
    public class ElevationMapTests
    {
        [Test]
        public void UnsetTile_IsLevelZero()
        {
            var map = new ElevationMap();
            Assert.That(map.GetLevel(new GridCoord(2, 2)), Is.EqualTo(0));
        }

        [Test]
        public void SetLevel_IsReadBack()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(1, 1), 2);
            Assert.That(map.GetLevel(new GridCoord(1, 1)), Is.EqualTo(2));
        }

        [Test]
        public void SetLevel_Overwrites()
        {
            var map = new ElevationMap();
            var coord = new GridCoord(1, 1);
            map.SetLevel(coord, 2);
            map.SetLevel(coord, 3);
            Assert.That(map.GetLevel(coord), Is.EqualTo(3));
        }

        [Test]
        public void SetLevelZero_ResetsToDefault()
        {
            var map = new ElevationMap();
            var coord = new GridCoord(1, 1);
            map.SetLevel(coord, 2);
            map.SetLevel(coord, 0);
            Assert.That(map.GetLevel(coord), Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(1, 1), 2);
            map.SetLevel(new GridCoord(2, 2), 1);
            map.Clear();
            Assert.That(map.GetLevel(new GridCoord(1, 1)), Is.EqualTo(0));
            Assert.That(map.GetLevel(new GridCoord(2, 2)), Is.EqualTo(0));
        }

        [Test]
        public void DistinctCoords_AreIndependent()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(1, 1), 2);
            Assert.That(map.GetLevel(new GridCoord(1, 2)), Is.EqualTo(0));
            Assert.That(map.GetLevel(new GridCoord(2, 1)), Is.EqualTo(0));
        }

        // === IsUniformUnder: the no-straddling rule for multi-tile footprints ===

        [Test]
        public void IsUniformUnder_FlatGround_True()
        {
            var map = new ElevationMap();
            Assert.That(map.IsUniformUnder(new GridFootprint(2, 2), new GridCoord(2, 2)), Is.True);
        }

        [Test]
        public void IsUniformUnder_OneCellRaised_StraddlesEdge_False()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(3, 3), 1); // one corner of the 2x2 is raised
            Assert.That(map.IsUniformUnder(new GridFootprint(2, 2), new GridCoord(2, 2)), Is.False);
        }

        [Test]
        public void IsUniformUnder_WholeFootprintRaised_True()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(2, 2), 1);
            map.SetLevel(new GridCoord(3, 2), 1);
            map.SetLevel(new GridCoord(2, 3), 1);
            map.SetLevel(new GridCoord(3, 3), 1);
            Assert.That(map.IsUniformUnder(new GridFootprint(2, 2), new GridCoord(2, 2)), Is.True);
        }

        [Test]
        public void IsUniformUnder_SingleTile_AlwaysTrue()
        {
            var map = new ElevationMap();
            map.SetLevel(new GridCoord(1, 1), 3);
            Assert.That(map.IsUniformUnder(GridFootprint.Single, new GridCoord(1, 1)), Is.True);
        }
    }
}
