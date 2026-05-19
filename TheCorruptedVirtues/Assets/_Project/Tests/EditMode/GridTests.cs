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

        [Test]
        public void AddFootprint_OccupiesEveryCoveredCell()
        {
            var occ = new GridOccupancy();
            occ.AddFootprint(new GridFootprint(2, 2), new GridCoord(3, 4));

            Assert.That(occ.IsOccupied(new GridCoord(3, 4)), Is.True);
            Assert.That(occ.IsOccupied(new GridCoord(4, 4)), Is.True);
            Assert.That(occ.IsOccupied(new GridCoord(3, 5)), Is.True);
            Assert.That(occ.IsOccupied(new GridCoord(4, 5)), Is.True);
            Assert.That(occ.IsOccupied(new GridCoord(5, 5)), Is.False);
        }

        [Test]
        public void RemoveFootprint_FreesEveryCoveredCell()
        {
            var occ = new GridOccupancy();
            var beast = new GridFootprint(2, 2);
            occ.AddFootprint(beast, new GridCoord(1, 1));

            occ.RemoveFootprint(beast, new GridCoord(1, 1));

            Assert.That(occ.IsOccupied(new GridCoord(1, 1)), Is.False);
            Assert.That(occ.IsOccupied(new GridCoord(2, 2)), Is.False);
        }

        [Test]
        public void SingleFootprint_MatchesLegacySingleCellApi()
        {
            var footprintOcc = new GridOccupancy();
            var legacyOcc = new GridOccupancy();
            var c = new GridCoord(2, 3);

            footprintOcc.AddFootprint(GridFootprint.Single, c);
            legacyOcc.Add(c);

            Assert.That(footprintOcc.IsOccupied(c), Is.EqualTo(legacyOcc.IsOccupied(c)));
            Assert.That(footprintOcc.IsOccupied(new GridCoord(3, 3)), Is.False);
        }

        [Test]
        public void CanPlace_TrueOnlyWhenAllCellsClearAndInBounds()
        {
            var occ = new GridOccupancy();
            var bounds = new GridBounds(5, 5);
            var beast = new GridFootprint(2, 2);

            Assert.That(occ.CanPlace(beast, new GridCoord(0, 0), bounds), Is.True);

            occ.Add(new GridCoord(1, 1));
            Assert.That(occ.CanPlace(beast, new GridCoord(0, 0), bounds), Is.False);

            Assert.That(occ.CanPlace(beast, new GridCoord(4, 4), bounds), Is.False);
        }
    }

    public class GridFootprintTests
    {
        [Test]
        public void Single_IsOneByOne_CoversAnchorOnly()
        {
            var cells = new List<GridCoord>(GridFootprint.Single.Cells(new GridCoord(7, 2)));

            Assert.That(GridFootprint.Single.Width, Is.EqualTo(1));
            Assert.That(GridFootprint.Single.Height, Is.EqualTo(1));
            Assert.That(cells.Count, Is.EqualTo(1));
            Assert.That(cells[0], Is.EqualTo(new GridCoord(7, 2)));
        }

        [Test]
        public void Cells_EnumeratesEveryCellFromAnchor()
        {
            var cells = new List<GridCoord>(new GridFootprint(2, 3).Cells(new GridCoord(1, 1)));

            Assert.That(cells.Count, Is.EqualTo(6));
            Assert.That(cells, Has.Member(new GridCoord(1, 1)));
            Assert.That(cells, Has.Member(new GridCoord(2, 1)));
            Assert.That(cells, Has.Member(new GridCoord(1, 3)));
            Assert.That(cells, Has.Member(new GridCoord(2, 3)));
            Assert.That(cells, Has.No.Member(new GridCoord(3, 1)));
        }

        [Test]
        public void NonPositiveDimensions_ClampToOne()
        {
            var footprint = new GridFootprint(0, -4);

            Assert.That(footprint.Width, Is.EqualTo(1));
            Assert.That(footprint.Height, Is.EqualTo(1));
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

        [Test]
        public void FootprintBlocker_RoutesBfsSameAsManualCells()
        {
            var bounds = new GridBounds(3, 3);
            var wall = new GridFootprint(3, 1);
            var start = new GridCoord(0, 0);
            var goal = new GridCoord(0, 2);

            // A 3x1 footprint at row 1 walls the grid in half: no path.
            var footprintOcc = new GridOccupancy();
            footprintOcc.AddFootprint(wall, new GridCoord(0, 1));
            var footprintPath = GridPathfinderBfs.FindPath(start, goal, footprintOcc, bounds);

            // The same cells added one-by-one must produce the identical result.
            var manualOcc = new GridOccupancy();
            manualOcc.Add(new GridCoord(0, 1));
            manualOcc.Add(new GridCoord(1, 1));
            manualOcc.Add(new GridCoord(2, 1));
            var manualPath = GridPathfinderBfs.FindPath(start, goal, manualOcc, bounds);

            Assert.That(footprintPath, Is.Empty);
            Assert.That(manualPath, Is.Empty);
            Assert.That(footprintPath, Is.EqualTo(manualPath));
        }
    }
}
