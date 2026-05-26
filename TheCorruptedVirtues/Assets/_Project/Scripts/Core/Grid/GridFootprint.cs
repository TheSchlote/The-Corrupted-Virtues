using System.Collections.Generic;

namespace TheCorruptedVirtues.CombatSlice.Core
{
    // A rectangular W x H block of tiles a unit occupies, anchored at its
    // minimum (origin) corner. M1 only ever spawns 1x1 units; multi-tile
    // footprints (e.g. the 2x2 "Boss" bosses) are supported here from
    // the start so the grid core never has to be reworked for them later.
    public readonly struct GridFootprint
    {
        public static readonly GridFootprint Single = new GridFootprint(1, 1);

        public readonly int Width;
        public readonly int Height;

        // True for a 1x1 footprint — lets callers keep the simpler single-cell
        // path for normal units and only branch into multi-tile logic for the
        // bosses that actually need it.
        public bool IsSingle => Width <= 1 && Height <= 1;

        // Dimensions below 1 are clamped to 1 (matches the clamping
        // convention used elsewhere in the core, e.g. ExecutionCalculator).
        public GridFootprint(int width, int height)
        {
            Width = width < 1 ? 1 : width;
            Height = height < 1 ? 1 : height;
        }

        // Every cell this footprint covers when its origin corner sits at anchor.
        public IEnumerable<GridCoord> Cells(GridCoord anchor)
        {
            for (int dy = 0; dy < Height; dy++)
            {
                for (int dx = 0; dx < Width; dx++)
                {
                    yield return new GridCoord(anchor.X + dx, anchor.Y + dy);
                }
            }
        }

        // True if 'cell' falls inside this footprint anchored at 'anchor'. O(1)
        // and allocation-free (unlike scanning Cells). Treats sub-1 dimensions
        // as 1, matching the constructor's clamp, so default(GridFootprint) acts
        // as 1x1 rather than covering nothing.
        public bool Covers(GridCoord anchor, GridCoord cell)
        {
            int w = Width < 1 ? 1 : Width;
            int h = Height < 1 ? 1 : Height;
            return cell.X >= anchor.X && cell.X < anchor.X + w
                && cell.Y >= anchor.Y && cell.Y < anchor.Y + h;
        }
    }
}
