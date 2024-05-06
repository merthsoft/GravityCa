using System.Runtime.CompilerServices;

namespace Merthsoft.GravityCa;
public static class TopologyHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Cycle(int n, int c)
        => (n + c) % c;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ShiftBool(this bool b, int amount)
        => b ? 1 << amount : 0;

    public static (int x, int y) TranslatePoint(int c, int r, Topology toTopology, int mapWidth, int mapHeight)
    {
        if (toTopology == Topology.Plane)
            return new(c, r);

        TranslatePoint(c, r, toTopology, mapWidth, mapHeight, out var lC, out var lR);
        return new(lC, lR);
    }

    public static void TranslatePoint(int c, int r, Topology toTopology, int mapWidth, int mapHeight, out int lC, out int lR)
    {
        lC = c;
        lR = r;

        if (toTopology == Topology.Plane)
            return;

        var lE = c <= -1;
        var tEE = r <= -1;
        var rE = c >= mapWidth;
        var bE = r >= mapHeight;
        var ovC = lE || rE;
        var ovR = tEE || bE;

        if (!ovC && !ovR || toTopology == Topology.Plane)
            return;

        switch (toTopology)
        {
            case Topology.Plane:
                lC = c; lR = r;
                break;
            case Topology.Ring:
                lC = ovC ? Cycle(c, mapWidth) : c;
                lR = r;
                break;
            case Topology.Mobius:
                lC = ovC ? Cycle(c, mapWidth) : c;
                lR = ovC ? mapHeight - r : r;
                break;
            case Topology.Torus:
                lC = ovC ? Cycle(c, mapWidth) : c;
                lR = ovR ? Cycle(r, mapHeight) : r;
                break;
            case Topology.Sphere:
                {
                    /* This only makes sense if the board is square. Otherwise, all hell will break loose */
                    var mask = lE.ShiftBool(3) | rE.ShiftBool(2) | tEE.ShiftBool(1) | (bE ? 1 : 0);
                    switch (mask)
                    {
                        case 0xA:
                        case 0x5:
                            lC = 0;
                            lR = 0;
                            return;
                        case 0x9:
                            lC = mapWidth;
                            lR = 1;
                            break;
                        case 0x8:
                            lC = r;
                            lR = 1;
                            break;
                        case 0x6:
                            lC = 1;
                            lR = mapHeight;
                            break;
                        case 0x4:
                            lC = r;
                            lR = mapHeight;
                            break;
                        case 0x2:
                            lC = 1;
                            lR = c;
                            break;
                        case 0x1:
                            lC = mapWidth;
                            lR = c;
                            break;
                        default:
                            lC = c;
                            lR = r;
                            break;
                    }
                    break;
                }
            case Topology.Klein:
                lC = ovC ? Cycle(c, mapWidth) : c;
                lR = ovR ? Cycle(r, mapHeight) : r;
                lR = ovC ? mapHeight - lR : lR;
                break;
            case Topology.ProjectivePlane:
                lC = ovC ? Cycle(c, mapWidth) : c;
                lC = ovR ? mapWidth - lC : lC;
                lR = ovR ? Cycle(r, mapHeight) : r;
                lR = ovC ? mapHeight - lR : lR;
                break;
        }
    }
}
