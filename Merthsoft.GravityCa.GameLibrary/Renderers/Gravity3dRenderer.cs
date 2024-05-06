using Merthsoft.GravityCa.GameLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary.Renderers;
internal class Gravity3dRenderer(
    GravityMap gravityMap, 
    Point screenSize,
    GraphicsDevice graphicsDevice,
    BasicEffect effect,
    int initialPrimitiveCount = 10_000_000)
{
    public GraphicsDevice GraphicsDevice { get; set; } = graphicsDevice;
    public BasicEffect Effect { get; set; } = effect;

    protected PrimitiveType PrimitiveType = PrimitiveType.TriangleList;
    protected VertexPositionColor[] VertexBuffer = new VertexPositionColor[initialPrimitiveCount];
    protected int[] IndexBuffer = new int[initialPrimitiveCount];
    protected int PrimitiveCount;

    protected int VertexBufferIndex = 0;
    protected int IndexBufferIndex = 0;

    public Vector3 CameraPosition { get; set; }
    public Vector3 CameraFacing { get; set; }

    private static Vector3[] Vectors = new Vector3[4];

    public static string RenderKey => "3DPlane";

    public Point ScreenSize { get; set; } = screenSize;
    public Camera3D Camera { get; } = Camera3D.CreateDefaultOrthographic();

    public void ResetView()
    {
        Camera.MoveTo(new(-45, 135, 82));
        Camera.LookAt(new(gravityMap.Width / 2f, gravityMap.Height / 2f, 0));
    }

    public void Update(GameTime gameTime)
    {
        Camera.HandleControls(gameTime);

        if (!GravityGame.DrawMass && !GravityGame.DrawGravity)
            return;

        PrimitiveType = GravityGame.RenderMode switch
        {
            GravityRendererMode.ThreeDimmensionalDots => PrimitiveType.PointList,
            _ => PrimitiveType.TriangleList,
        };

        VertexBufferIndex = 0;
        IndexBufferIndex = 0;
        PrimitiveCount = 0;

        Effect.View = Camera.View;

        var (divisor, multiplier, reducer) = GravityGame.GravityHeightLerpMode switch
        {
            LerpMode.ZeroToSystemMax => (gravityMap.SystemMaxGravity, GravityGame.MapSize / 4f, 0),
            LerpMode.SystemMinToSystemMax => (gravityMap.SystemMaxGravity, (float)GravityGame.MapSize / 4f, gravityMap.SystemMinGravity),
            _ => ((double)GravityGame.MaxGravity, (float)GravityGame.MapSize, 0)
        };

        for (var i = 0; i < gravityMap.Width; i++)
            for (var j = 0; j < gravityMap.Height; j++)
                AddCell(i, j, divisor, multiplier, reducer);
    }

    public void Draw(GameTime gameTime)
    {
        if (VertexBuffer == null || VertexBufferIndex == 0)
            return;

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        GraphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType, VertexBuffer, 0, VertexBufferIndex, IndexBuffer, 0, PrimitiveCount);
        }
    }

    protected virtual void PushVertex(Vector3 vector, Color c)
    {
        VertexBuffer[VertexBufferIndex].Position.X = vector.X;
        VertexBuffer[VertexBufferIndex].Position.Y = vector.Y;
        VertexBuffer[VertexBufferIndex].Position.Z = vector.Z;
        VertexBuffer[VertexBufferIndex].Color = c;
        VertexBufferIndex++;
    }

    protected virtual void AddTri(Vector3[] vectors, Color c)
    {
        var currIndex = VertexBufferIndex;
        PushVertex(vectors[0], c);
        PushVertex(vectors[1], c);
        PushVertex(vectors[2], c);

        IndexBuffer[IndexBufferIndex++] = currIndex + 0;
        IndexBuffer[IndexBufferIndex++] = currIndex + 1;
        IndexBuffer[IndexBufferIndex++] = currIndex + 2;
        PrimitiveCount++;
    }

    protected virtual void AddQuad(Vector3[] vectors, Color c)
    {
        var currIndex = VertexBufferIndex;
        PushVertex(vectors[0], c);
        PushVertex(vectors[1], c);
        PushVertex(vectors[2], c);
        PushVertex(vectors[3], c);

        IndexBuffer[IndexBufferIndex++] = currIndex + 0;
        IndexBuffer[IndexBufferIndex++] = currIndex + 1;
        IndexBuffer[IndexBufferIndex++] = currIndex + 2;
        PrimitiveCount++;

        IndexBuffer[IndexBufferIndex++] = currIndex + 2;
        IndexBuffer[IndexBufferIndex++] = currIndex + 3;
        IndexBuffer[IndexBufferIndex++] = currIndex + 0;
        PrimitiveCount++;
    }

    protected virtual void AddQuad(Vector3[] vectors, Color[] colors)
    {
        var currIndex = VertexBufferIndex;
        PushVertex(vectors[0], colors[0]);
        PushVertex(vectors[1], colors[1]);
        PushVertex(vectors[2], colors[2]);
        PushVertex(vectors[3], colors[3]);

        IndexBuffer[IndexBufferIndex++] = currIndex + 0;
        IndexBuffer[IndexBufferIndex++] = currIndex + 1;
        IndexBuffer[IndexBufferIndex++] = currIndex + 2;
        PrimitiveCount++;

        IndexBuffer[IndexBufferIndex++] = currIndex + 2;
        IndexBuffer[IndexBufferIndex++] = currIndex + 3;
        IndexBuffer[IndexBufferIndex++] = currIndex + 0;
        PrimitiveCount++;
    }

    private void AddCell(int i, int j, double divisor, float multiplier, double reducer)
    {
        //var gravity = GravityMap.GetGravityAt(i, j, 0);
        var allGravities = gravityMap.GetGravityAdjacent(i, j).Select(a => (double)a.Value).ToArray();
        var cellGravity = allGravities[4];
        var gravity = (float)((cellGravity - reducer) / (divisor - reducer)) * multiplier;
        var color = GravityGame.GetColorAt(i, j);
        var x = (float)i;
        var z = (float)j;

        switch (GravityGame.RenderMode)
        {
            case GravityRendererMode.ThreeDimmensionalPlane:
                Vectors[0] = new Vector3(x, gravity, z);
                Vectors[1] = new Vector3(x + 1, gravity, z);
                Vectors[2] = new Vector3(x + 1, gravity, z + 1);
                Vectors[3] = new Vector3(x, gravity, z + 1);
                AddQuad(Vectors, color);
                break;
            case GravityRendererMode.ThreeDimmensionalSheet:
                AddSheetQuad(x, z, color, allGravities, divisor, multiplier, reducer);
                break;
            case GravityRendererMode.ThreeDimmensionalDots:
                Vectors[0] = new Vector3(x, gravity, z);
                PushVertex(Vectors[0], color);
                PrimitiveCount++;
                break;
            case GravityRendererMode.ThreeDimmensionalTinyCube:
                CreateCube(x, gravity, z, .1f, .1f, .1f, color);
                break;
            case GravityRendererMode.ThreeDimmensionalCube:
                CreateCube(x, gravity, z, 1f, 1f, 1f, color);
                break;
            case GravityRendererMode.ThreeDimmensionalRectangularPrism1:
                CreateCube(x, gravity, z, 1, GravityGame.MapSize - gravity, 1, color);
                break;
            case GravityRendererMode.ThreeDimmensionalRectangularPrism2:
                CreateCube(x, 0, z, 1, gravity, 1, color);
                break;
        }
    }

    private void AddSheetQuad(float x, float z, Color color, double[] tiles, double divisor, float multiplier, double reducer)
    {
        double[] sums = [
            tiles[3] + tiles[4] + tiles[0] + tiles[1],
            tiles[3] + tiles[4] + tiles[6] + tiles[7],
            tiles[4] + tiles[5] + tiles[7] + tiles[8],
            tiles[1] + tiles[2] + tiles[4] + tiles[5],
        ];
        var averages = sums.Select(sum => (float)(((double)sum / 4.0 - reducer) / (divisor - reducer)) * multiplier).ToArray();
        Vectors[0] = new Vector3(x, averages[0], z);
        Vectors[1] = new Vector3(x + 1, averages[1], z);
        Vectors[2] = new Vector3(x + 1, averages[2], z + 1);
        Vectors[3] = new Vector3(x, averages[3], z + 1);
        AddQuad(Vectors, color);
    }

    private void CreateCube(float x, float y, float z, float cellSizeX, float cellSizeY, float cellSizeZ, Color color)
    {
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 0, color);
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 1, color);
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 2, color);
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 3, color);
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 4, color);
        CreateWall(x, y, z, cellSizeX, cellSizeY, cellSizeZ, 5, color);
    }

    private void CreateWall(float x, float y, float z, float cellSizeX, float cellSizeY, float cellSizeZ, int direction, Color? color = null)
    {
        switch (direction)
        {
            case 0:
                Vectors[0] = new Vector3(x, y, z);
                Vectors[1] = new Vector3(x, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x + cellSizeX, y, z + cellSizeZ);
                Vectors[3] = new Vector3(x + cellSizeX, y, z);
                break;
            case 1:
                Vectors[0] = new Vector3(x + cellSizeX, y, z);
                Vectors[1] = new Vector3(x + cellSizeX, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x + cellSizeX, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x + cellSizeX, y + cellSizeY, z);
                break;
            case 2:
                Vectors[0] = new Vector3(x + cellSizeX, y + cellSizeY, z);
                Vectors[1] = new Vector3(x + cellSizeX, y + cellSizeY, z + cellSizeZ);
                Vectors[2] = new Vector3(x, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x, y + cellSizeY, z);
                break;
            case 3:
                Vectors[0] = new Vector3(x, y, z);
                Vectors[1] = new Vector3(x, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x, y + cellSizeY, z);
                break;
            case 4:
                Vectors[0] = new Vector3(x, y, z);
                Vectors[1] = new Vector3(x + cellSizeX, y, z);
                Vectors[2] = new Vector3(x + cellSizeX, y + cellSizeY, z);
                Vectors[3] = new Vector3(x, y + cellSizeY, z);
                break;
            case 5:
                Vectors[0] = new Vector3(x, y, z + cellSizeZ);
                Vectors[1] = new Vector3(x + cellSizeX, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x + cellSizeX, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x, y + cellSizeY, z + cellSizeZ);
                break;
            case 6:
            case 8:
                Vectors[0] = new Vector3(x, y, z);
                Vectors[1] = new Vector3(x, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x + cellSizeX, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x + cellSizeX, y + cellSizeY, z);
                break;
            case 7:
            case 9:
                Vectors[0] = new Vector3(x + cellSizeX, y, z);
                Vectors[1] = new Vector3(x + cellSizeX, y, z + cellSizeZ);
                Vectors[2] = new Vector3(x, y + cellSizeY, z + cellSizeZ);
                Vectors[3] = new Vector3(x, y + cellSizeY, z);
                break;
        }

        AddQuad(Vectors, color ?? Color.Transparent);
    }
}
