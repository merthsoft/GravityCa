using System.Runtime.CompilerServices;

namespace Merthsoft.GravityCa.GameLibrary;

public class GravityMap
{
    public readonly UInt128[] GravityLayer;
    public readonly UInt128[] MassLayer;
    public readonly UInt128[] BackBoard;

    public UInt128 GlobalMaxGravity { get; set; }
    public UInt128 GlobalMaxMass { get; set; }

    public int Height { get; private set; }
    public int Width { get; private set; }

    public Topology Topology { get; set; }
    public int Generation { get; private set; } = 0;
    public bool Running { get; set; } = false;
    public double SystemTotalMass { get; private set; } = 0;
    public double SystemMaxMass { get; private set; } = 0;
    public double SystemMinMass { get; private set; }
    public double SystemTotalGravity { get; private set; } = 0;
    public double SystemMaxGravity { get; private set; } = 0;
    public double SystemMinGravity { get; private set; }

    private readonly static AdjacentTile<UInt128>[] AdjacentTiles = new AdjacentTile<UInt128>[9];

    public int UpdateState { get; private set; } = 0;

    private readonly Random random;

    public GravityMap(int width, int height, UInt128 globalMaxGravity, UInt128 globalMaxMass, Topology topology, Random? random = null)
    {
        this.random = random ?? Random.Shared;

        Width = width;
        Height = height;
        GlobalMaxMass = globalMaxMass;
        GlobalMaxGravity = globalMaxGravity;
        Topology = topology;

        GravityLayer = new UInt128[Width * Height];
        MassLayer = new UInt128[Width * Height];
        BackBoard = new UInt128[Width * Height];
        Array.Fill<UInt128>(GravityLayer, 0);
        Array.Fill<UInt128>(MassLayer, 0);
        Array.Fill<UInt128>(BackBoard, 0);

        var index = 0;
        for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                AdjacentTiles[index++] = new() { XOffset = x, YOffset = y };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int x, int y) TranslatePoint(int x, int y)
        => TopologyHelper.TranslatePoint(x, y, Topology, Width, Height);

    public void SetMass(int x, int y, UInt128 mass)
        => SafeSet(MassLayer, x, y, Width, Height, mass, Topology);

    public UInt128 GetMassAt(int x, int y, UInt128 @default)
        => SafeGet(MassLayer, x, y, Width, Height, Topology, @default);

    public UInt128 GetGravityAt(int x, int y, UInt128 @default)
            => SafeGet(GravityLayer, x, y, Width, Height, Topology, @default);

    public AdjacentTile<UInt128>[] GetGravityAdjacent(int x, int y)
    {
        FillAdjacentCells(GravityLayer, x, y, Width, Height, Topology);
        return AdjacentTiles;
    }

    public void SetGravity(int x, int y, UInt128 gravity)
        => SafeSet(GravityLayer, x, y, Width, Height, gravity, Topology);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SafeSet(UInt128[] array, int x, int y, int w, int h, UInt128 v, Topology topology)
    {
        TopologyHelper.TranslatePoint(x, y, topology, w, h, out var lX, out var lY);
        if (lX < 0 || lY < 0 || lX >= w || lY >= h)
            return;
        array[lX * w + lY] = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UInt128 SafeGet(UInt128[] array, int x, int y, int w, int h, Topology topology, UInt128 @default)
    {
        TopologyHelper.TranslatePoint(x, y, topology, w, h, out var lX, out var lY);
        if (lX < 0 || lY < 0 || lX >= w || lY >= h)
            return @default;
        return array[lX * w + lY];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FillAdjacentCells(UInt128[] array, int x, int y, int w, int h, Topology topology)
    {
        AdjacentTiles[0].Value = SafeGet(array, x - 1, y - 1, w, h, topology, 0);
        AdjacentTiles[1].Value = SafeGet(array, x - 1, y, w, h, topology, 0);
        AdjacentTiles[2].Value = SafeGet(array, x - 1, y + 1, w, h, topology, 0);
        AdjacentTiles[3].Value = SafeGet(array, x, y - 1, w, h, topology, 0);
        AdjacentTiles[4].Value = SafeGet(array, x, y, w, h, topology, 0);
        AdjacentTiles[5].Value = SafeGet(array, x, y + 1, w, h, topology, 0);
        AdjacentTiles[6].Value = SafeGet(array, x + 1, y - 1, w, h, topology, 0);
        AdjacentTiles[7].Value = SafeGet(array, x + 1, y, w, h, topology, 0);
        AdjacentTiles[8].Value = SafeGet(array, x + 1, y + 1, w, h, topology, 0);
    }

    public void Update()
    {
        if (!Running)
        {
            UpdateTotals();
            return;
        }

        Generation++;
        Array.Fill<UInt128>(BackBoard, 0);
        if (UpdateState == 0)
        {
            SystemTotalGravity = 0;
            SystemMaxGravity = 0;
            SystemMinGravity = double.PositiveInfinity;
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                {
                    FillAdjacentCells(GravityLayer, x, y, Width, Height, Topology);
                    const int massReducer = 8;
                    var adjGrav = AdjacentTiles[0].Value / massReducer
                                + AdjacentTiles[1].Value / massReducer
                                + AdjacentTiles[2].Value / massReducer
                                + AdjacentTiles[3].Value / massReducer
                                + AdjacentTiles[5].Value / massReducer
                                + AdjacentTiles[6].Value / massReducer
                                + AdjacentTiles[7].Value / massReducer
                                + AdjacentTiles[8].Value / massReducer;
                    var gravity = MassLayer[x * Width + y] / (massReducer - 2) + adjGrav;
                    if (gravity == 0 && adjGrav > 0)
                        gravity = 1;

                    if (gravity < 0)
                        gravity = 0;

                    if (gravity > GlobalMaxGravity)
                        gravity = GlobalMaxGravity;

                    BackBoard[x * Width + y] = gravity;

                    var floatingPointGravity = (double)gravity;

                    if (floatingPointGravity > SystemMaxGravity)
                        SystemMaxGravity = (double)gravity;

                    if (floatingPointGravity < SystemMinGravity)
                        SystemMinGravity = floatingPointGravity;

                    SystemTotalGravity += floatingPointGravity;
                }

            Array.Copy(BackBoard, GravityLayer, Width * Height);
        }
        else if (UpdateState == 1)
        {
            SystemTotalMass = 0;
            SystemMaxMass = 0;
            SystemMinMass = double.PositiveInfinity;
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                {
                    var mass = MassLayer[x * Width + y];
                    if (mass == 0)
                        continue;

                    var floatingPointMass = (double)mass;
                    SystemTotalMass += floatingPointMass;
                    if (floatingPointMass > SystemMaxMass)
                        SystemMaxMass = floatingPointMass;

                    if (floatingPointMass < SystemMinMass)
                        SystemMinMass = floatingPointMass;

                    FillAdjacentCells(MassLayer, x, y, Width, Height, Topology);

                    var surrounded = AdjacentTiles.All(t => t.Value > 0);
                    var set = false;

                    if (!surrounded)
                    {
                        var spotGrav = GravityLayer[x * Width + y];

                        FillAdjacentCells(GravityLayer, x, y, Width, Height, Topology);
                        var gravList = AdjacentTiles.GroupBy(x => x.Value).OrderByDescending(x => x.Key);
                        foreach (var g in gravList)
                        {
                            if (g.Key < spotGrav)
                                break;
                            var gArr = g.ToArray();
                            random.Shuffle(gArr);
                            foreach (var selected in gArr)
                            {
                                var (xOffset, yOffset, _) = selected;
                                var (newX, newY) = TranslatePoint(x + xOffset, y + yOffset);
                                if (newX < 0 || newX >= Width || newY < 0 || newY >= Height
                                    || MassLayer[newX * Width + newY] != 0
                                    || BackBoard[newX * Width + newY] != 0)
                                    continue;
                                BackBoard[newX * Width + newY] = mass;
                                MassLayer[x * Width + y] = 0;
                                set = true;
                                break;
                            }
                            if (set)
                                break;
                        }
                    }
                    if (!set)
                        BackBoard[x * Width + y] = mass;
                }

            Array.Copy(BackBoard, MassLayer, Width * Height);
        }
        else if (UpdateState == 2)
        {
            SystemTotalMass = 0;
            for (var x = 0; x < Width; x++)
                for (var y = 0; y < Height; y++)
                {
                    var mass = MassLayer[x * Width + y];
                    if (mass == 0)
                        continue;
                    var floatingPointMass = (double)mass;
                    SystemTotalMass += floatingPointMass;
                    if (floatingPointMass > SystemMaxMass)
                        SystemMaxMass = floatingPointMass;

                    if (floatingPointMass < SystemMinMass)
                        SystemMinMass = floatingPointMass;

                    FillAdjacentCells(MassLayer, x, y, Width, Height, Topology);

                    var surrounded = AdjacentTiles.Count(t => t.Value > 0) > 1;
                    var set = false;
                    var hungry = random.NextDouble() < .25f;
                    var cellGravityPercent = (double)GetGravityAt(x, y, 0) / (double)GlobalMaxGravity;
                    var localMassMax = (UInt128)((double)GlobalMaxMass * cellGravityPercent);
                    if (hungry && surrounded && mass < localMassMax)
                    {
                        var smallestGroup = AdjacentTiles.Where((x, i) => i != 4 && x.Value > 0 && x.Value <= mass)
                                                            .GroupBy(x => x.Value).OrderBy(x => x.Key).FirstOrDefault()?.ToArray();
                        if (smallestGroup == null)
                            continue;
                        random.Shuffle(smallestGroup);
                        var (xOffset, yOffset, eatenMass) = smallestGroup.First();
                        var (newX, newY) = TranslatePoint(x + xOffset, y + yOffset);
                        if (newX >= 0 && newX < Width && newY >= 0 && newY < Height)
                            if (eatenMass + mass < localMassMax)
                            {
                                BackBoard[x * Width + y] = mass + eatenMass;
                                BackBoard[newX * Width + newY] = 0;
                                MassLayer[newX * Width + newY] = 0;
                                set = true;
                            }
                            else
                            {
                                //var diff = GravityCellularAutomataGame.MaxMass - mass;
                                //BackBoard[x * Width + y] = GravityCellularAutomataGame.MaxMass;
                                //BackBoard[newX, newY] = eatenMass - diff;
                                //MassLayer[newX, newY] = eatenMass - diff;
                                //set = true;
                            }
                    }
                    if (!set)
                        BackBoard[x * Width + y] = mass;
                }

            Array.Copy(BackBoard, MassLayer, Width * Height);
        }

        UpdateState = (UpdateState + 1) % 4;
    }

    private void UpdateTotals()
    {
        SystemTotalMass = 0;
        SystemMaxMass = 0;
        SystemMinMass = double.PositiveInfinity;
        SystemTotalGravity = 0;
        SystemMaxGravity = 0;
        SystemMinGravity = double.PositiveInfinity;

        for (var x = 0; x < Width * Height; x++)
        {
            BackBoard[x] = 0;
            var gravity = GravityLayer[x];
            var mass = MassLayer[x];

            if (gravity != 0)
            {
                var floatingPointGravity = (double)gravity;

                if (floatingPointGravity > SystemMaxGravity)
                    SystemMaxGravity = (double)gravity;

                if (floatingPointGravity < SystemMinGravity)
                    SystemMinGravity = floatingPointGravity;

                SystemTotalGravity += floatingPointGravity;
            }

            if (mass != 0)
            {
                var floatingPointMass = (double)mass;
                SystemTotalMass += floatingPointMass;
                if (floatingPointMass > SystemMaxMass)
                    SystemMaxMass = floatingPointMass;

                if (floatingPointMass < SystemMinMass)
                    SystemMinMass = floatingPointMass;
            }
        }
    }

    public void Reset()
    {
        Generation = 0;
        Array.Fill<UInt128>(GravityLayer, 0);
        Array.Fill<UInt128>(MassLayer, 0);
        Array.Fill<UInt128>(BackBoard, 0);
        SystemTotalMass = 0;
        SystemMaxMass = 0;
        SystemMinMass = (double)GlobalMaxMass;
        SystemTotalGravity = 0;
        SystemMaxGravity = 0;
        SystemMinGravity = (double)GlobalMaxGravity;
    }
}