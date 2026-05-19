using System.Collections.Generic;
using NUnit.Framework;
using TheCorruptedVirtues.CombatSlice.Core;

namespace TheCorruptedVirtues.Tests
{
    public class GridOccupancyTests
    {
        [Test]
        public void AddRemove_TogglesOccupancy()
        {
            var occ = new GridOccupancy();
            var c = new GridCoord(2, 3);

            Assert.That(occ.IsOccupied(c), Is.False);
            occ.Add(c);
            Assert.That(occ.IsOccupied(c), Is.True);
            occ.Remove(c);
            Assert.That(occ.IsOccupied(c), Is.False);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var occ = new GridOccupancy();
            occ.Add(new GridCoord(0, 0));
            occ.Add(new GridCoord(1, 1));

            occ.Clear();

            Assert.That(occ.IsOccupied(new GridCoord(0, 0)), Is.False);
            Assert.That(occ.IsOccupied(new GridCoord(1, 1)), Is.False);
        }
    }

    public class GridMathTests
    {
        [Test]
        public void ManhattanDistance_IsAbsoluteSum()
        {
            Assert.That(GridMath.ManhattanDistance(new GridCoord(0, 0), new GridCoord(3, 4)), Is.EqualTo(7));
            Assert.That(GridMath.ManhattanDistance(new GridCoord(-2, 1), new GridCoord(1, -1)), Is.EqualTo(5));
        }

        [Test]
        public void StepCount_IsPathLengthMinusOne()
        {
            var path = new List<GridCoord> { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(2, 0) };
            Assert.That(GridMath.StepCount(path), Is.EqualTo(2));
            Assert.That(GridMath.StepCount(new List<GridCoord>()), Is.EqualTo(0));
            Assert.That(GridMath.StepCount(null), Is.EqualTo(0));
        }
    }

    public class GridPathfinderBfsTests
    {
        private static readonly GridBounds Corridor = new GridBounds(3, 1);

        [Test]
        public void StartEqualsGoal_ReturnsSingleCell()
        {
            var path = GridPathfinderBfs.FindPath(new GridCoord(0, 0), new GridCoord(0, 0), null, Corridor);
            Assert.That(path.Count, Is.EqualTo(1));
            Assert.That(path[0], Is.EqualTo(new GridCoord(0, 0)));
        }

        [Test]
        public void OpenCorridor_FindsStraightPath()
        {
            var path = GridPathfinderBfs.FindPath(new GridCoord(0, 0), new GridCoord(2, 0), null, Corridor);

            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(path[0], Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(path[path.Count - 1], Is.EqualTo(new GridCoord(2, 0)));
            Assert.That(GridMath.StepCount(path), Is.EqualTo(2));
        }

        [Test]
        public void BlockedCorridor_ReturnsNoPath()
        {
            var blocked = new GridOccupancy();
            blocked.Add(new GridCoord(1, 0));

            var path = GridPathfinderBfs.FindPath(new GridCoord(0, 0), new GridCoord(2, 0), blocked, Corridor);

            Assert.That(path, Is.Empty);
        }

        [Test]
        public void GoalOutOfBounds_ReturnsNoPath()
        {
            var path = GridPathfinderBfs.FindPath(new GridCoord(0, 0), new GridCoord(5, 0), null, Corridor);
            Assert.That(path, Is.Empty);
        }
    }
}
