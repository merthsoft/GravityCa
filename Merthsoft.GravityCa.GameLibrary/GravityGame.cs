using Merthsoft.GravityCa;
using Merthsoft.GravityCa.GameLibrary;
using Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.Reflection;

namespace Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary;

public class GravityGame : Game
{
    protected Random random = Random.Shared;
    protected Color DefaultBackgroundColor = Color.Black;
    protected bool ShouldQuit { get; set; } = false;
    protected GraphicsDeviceManager Graphics = null!;
    protected SpriteBatch SpriteBatch = null!; // Set in load 
    public int ScreenWidth => GraphicsDevice.Viewport.Width;
    public int ScreenHeight => GraphicsDevice.Viewport.Height;
    public Vector2 ScreenSize => new(ScreenWidth, ScreenHeight);
    protected FramesPerSecondCounter FramesPerSecondCounter { get; } = new();

    public List<MouseState> PreviousMouseStates { get; } = [];
    public MouseState PreviousMouseState => PreviousMouseStates[^1];
    public MouseState CurrentMouseState { get; private set; }

    public List<KeyboardState> PreviousKeyStates { get; } = [];
    public KeyboardState PreviousKeyState => PreviousKeyStates[^1];

    public KeyboardState CurrentKeyState { get; private set; }

    public bool IsActiveAndMouseInBounds
    {
        get
        {
            var (mx, my) = CurrentMouseState.Position;
            return IsActive && mx >= 0 && mx < ScreenWidth && my >= 0 && my < ScreenHeight;
        }
    }


    public const int MapSize = 125;

    public static readonly UInt128 MaxGravity = UInt128.MaxValue;
    public static readonly UInt128 MaxMass = UInt128.MaxValue >> 1;
    public static readonly GravityMap Map = new(MapSize, MapSize, MaxGravity, MaxMass, Topology.Torus);
    public static Point ScreenScale { get; private set; } = Point.Zero;
    public static bool DrawGravity { get; set; } = true;
    public static bool DrawMass { get; set; } = true;
    public static Color[] GravityColors { get; set; } = Palettes.AllPalettes[1];
    public static LerpMode GravityColorLerpMode { get; set; } = LerpMode.SystemMinToSystemMax;
    public static LerpMode GravityHeightLerpMode { get; set; } = LerpMode.SystemMinToSystemMax;
    public static Color[] MassColors { get; set; } = Palettes.AllPalettes[0];
    public static UInt128? MassMinDrawValue { get; set; }
    public static bool ConnectCells { get; set; } = true;
    public static GravityRendererMode RenderMode { get; set; } = GravityRendererMode.ThreeDimmensionalRectangularPrism2;

    bool genRandom = true;
    bool hasRenderMinimum = false;
    private Gravity2dRenderer TwoDRenderer = null!;
    private Gravity3dRenderer ThreeDRenderer = null!;
    public UInt128 MassDivisor = (UInt128)Math.Pow(2, 25);
    public bool DrawText = false;
    public object mapLock = new();
    private string version = "Gravity";

    float ScreenWidthRatio => (float)ScreenWidth / MapSize;
    float ScreenHeightRatio => (float)ScreenHeight / MapSize;
    Vector2 MouseLocation => new(CurrentMouseState.X / (int)ScreenWidthRatio, CurrentMouseState.Y / (int)ScreenHeightRatio);


    public GravityGame()
    {
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        IsFixedTimeStep = false;
        Graphics.SynchronizeWithVerticalRetrace = false;
        IsMouseVisible = true;

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        Graphics.PreferredBackBufferWidth = 1800;
        Graphics.PreferredBackBufferHeight = 1800;
        Graphics.IsFullScreen = false;
        Graphics.ApplyChanges(); 
        
        for (var i = 0; i < 5; i++)
        {
            PreviousMouseStates.Add(default);
            PreviousKeyStates.Add(default);
        }

        var assembly = Assembly.GetAssembly(typeof(GravityMap))!;
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        var verParts = fileVersionInfo.ProductVersion!.Split('-');
        var ver = verParts[0] + '-' + verParts[1];
        version = $"GravityCA - v{ver}";
        Window.Title = version;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        ScreenScale = new Point(ScreenWidth, ScreenHeight);
        TwoDRenderer = new Gravity2dRenderer(Map, GraphicsDevice, SpriteBatch, ScreenScale);
        ThreeDRenderer = new Gravity3dRenderer(Map, ScreenScale, GraphicsDevice, new(GraphicsDevice)
        {
            Alpha = 1,
            TextureEnabled = false,
            VertexColorEnabled = true,
            LightingEnabled = false,
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(100), 1, 1f, 1000f),
            World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up),
        });
        ThreeDRenderer.ResetView();
    }

    protected override void Update(GameTime gameTime)
    {
        PreviousMouseStates.RemoveAt(0);
        PreviousMouseStates.Add(CurrentMouseState);
        CurrentMouseState = Mouse.GetState();

        PreviousKeyStates.RemoveAt(0);
        PreviousKeyStates.Add(CurrentKeyState);
        CurrentKeyState = Keyboard.GetState();

        if (WasKeyJustPressed(Keys.Escape))
            Exit();

        if (WasKeyJustPressed(Keys.Enter) && IsKeyDown(Keys.LeftAlt))
            Graphics.ToggleFullScreen();

        if (WasKeyJustPressed(Keys.Space))
            Map.Running = !Map.Running;

        if (WasKeyJustPressed(Keys.T))
            DrawText = !DrawText;

        if (WasKeyJustPressed(Keys.M))
            DrawMass = !DrawMass;

        if (WasKeyJustPressed(Keys.G))
            DrawGravity = !DrawGravity;

        var keyPressed = CurrentKeyState.GetPressedKeys().FirstOrDefault();
        if (keyPressed >= Keys.D0 && keyPressed <= Keys.D9)
            if (IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt))
                MassColors = Palettes.AllPalettes[keyPressed - Keys.D0];
            else
                GravityColors = Palettes.AllPalettes[keyPressed - Keys.D0];
        else if (keyPressed >= Keys.F1 && keyPressed <= Keys.F12)
        {
            var index = keyPressed - Keys.F1;
            if (index < Enum.GetValues(typeof(GravityRendererMode)).Length)
                RenderMode = (GravityRendererMode)index;
        }

        if (IsActive && (IsLeftMouseDown() || IsRightMouseDown()))
        {
            var l = MouseLocation;
            var x = (int)l.X;
            var y = (int)l.Y;
            var mass = IsLeftMouseDown() ? Map.GetMassAt(x, y, 0) + MaxMass / MassDivisor : 0;
            for (var i = 0; i <= 2; i++)
                for (var j = 0; j <= 2; j++)
                    Map.SetMass(x + i, y + j, mass);
        }

        if (GetScrollWheelDelta() > 0)
            MassDivisor <<= 1;
        else if (GetScrollWheelDelta() < 0)
            MassDivisor >>= 1;

        if (MassDivisor < 1)
            MassDivisor = 1;

        if (WasKeyJustPressed(Keys.Z) || WasKeyJustPressed(Keys.C))
            for (var x = 0; x < MapSize; x++)
                for (var y = 0; y < MapSize; y++)
                    Map.SetMass(x, y, 0);

        if (WasKeyJustPressed(Keys.X) || WasKeyJustPressed(Keys.C))
            for (var x = 0; x < MapSize; x++)
                for (var y = 0; y < MapSize; y++)
                    Map.SetGravity(x, y, 0);

        if (WasKeyJustPressed(Keys.C))
            Map.Reset();

        if (WasKeyJustPressed(Keys.Q))
            GravityColorLerpMode = GravityColorLerpMode.Next();

        if (WasKeyJustPressed(Keys.E))
            GravityHeightLerpMode = GravityHeightLerpMode.Next();

        if (WasKeyJustPressed(Keys.V))
            hasRenderMinimum = !hasRenderMinimum;

        if (WasKeyJustPressed(Keys.B))
            genRandom = !genRandom;

        if (WasKeyJustPressed(Keys.T))
            Map.Topology = Map.Topology.Next();

        if (genRandom && Map.Running || WasKeyJustPressed(Keys.R) || WasKeyJustPressed(Keys.H))
        {
            var chance = genRandom && Map.Running ? .002 : WasKeyJustPressed(Keys.R) ? .02f : 1f;
            for (var x = 0; x < MapSize; x++)
                for (var y = 0; y < MapSize; y++)
                    if (Map.GetMassAt(x, y, 0) == 0 && random.NextSingle() <= chance)
                        Map.SetMass(x, y, MaxMass / MassDivisor);
        }


        //if (hasRenderMinimum)
        //    Renderer.MassMinDrawValue = MaxMass / (MassDivisor+1);
        //else
        //    Renderer.MassMinDrawValue = null;

        Window.Title = $"{version} - {(Map.Running ? "Running" : "Paused")}{(genRandom ? "*" : "")} | Div: {MassDivisor:N0} | Generation {Map.Generation:N0} | FPS {FramesPerSecondCounter.FramesPerSecond} | {Map.Topology}";

        if (ShouldQuit)
            Exit();

        FramesPerSecondCounter.Update(gameTime);

        Map.Update();
        if (RenderMode != GravityRendererMode.Flat)
            ThreeDRenderer.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        FramesPerSecondCounter.Draw(gameTime);
        GraphicsDevice.Clear(DefaultBackgroundColor);

        if (RenderMode == GravityRendererMode.Flat)
            TwoDRenderer.Draw(gameTime);
        else
            ThreeDRenderer.Draw(gameTime);
        
        if (RenderMode != GravityRendererMode.Flat)
            return;

        base.Draw(gameTime);
    }

    public static Color GetColorAt(int i, int j)
    {
        Color? color = null;
        if (DrawMass && Map.SystemTotalMass > 0)
            color = GetColor(Map.MassLayer, i, j, MassColors, (double)Map.GlobalMaxMass, (UInt128)(Map.SystemMaxMass / 2.0), null, Map.SystemMinMass, Map.SystemMaxMass, LerpMode.ZeroToSystemMax);
        if (DrawGravity && color == null && Map.SystemTotalGravity > 0)
            color = GetColor(Map.GravityLayer, i, j, GravityColors, (double)Map.GlobalMaxGravity, null, null, Map.SystemMinGravity, Map.SystemMaxGravity, GravityColorLerpMode);

        return color ?? Color.Transparent;
    }

    private static Color? GetColor(
                       UInt128[] tileLayer,
                       int i,
                       int j,
                       Color[] colors,
                       double overallMax,
                       UInt128? minDrawValue,
                       UInt128? maxDrawValue,
                       double systemMin,
                       double systemMax,
                       LerpMode lerpMode)
    {
        var value = (double)tileLayer[i * Map.Width + j];
        var percentage = lerpMode switch
        {
            LerpMode.ZeroToSystemMax => value / systemMax,
            LerpMode.SystemMinToSystemMax => (value - systemMin) / (systemMax - systemMin),
            _ => value / overallMax,
        };

        if (minDrawValue.HasValue && value < (double)minDrawValue.Value
            || maxDrawValue.HasValue && value > (double)maxDrawValue.Value)
            return null;
        else if (percentage == 0 || percentage <= 0 || double.IsNaN(percentage) || !double.IsPositive(percentage))
            return colors[0];
        else if (percentage >= 1 || double.IsInfinity(percentage))
            return colors.Last();

        var colorLocation = (colors.Length - 1) * percentage;
        var colorIndex = (int)colorLocation;
        var newPercentage = colorLocation - colorIndex;
        return Extensions.ColorGradientPercentage(colors[colorIndex], colors[colorIndex + 1], newPercentage);
    }

    public bool WasMouseMoved()
        => CurrentMouseState.X != PreviousMouseState.X
        || CurrentMouseState.Y != PreviousMouseState.Y;

    public int GetScrollWheelDelta()
        => CurrentMouseState.ScrollWheelValue - PreviousMouseState.ScrollWheelValue;

    public bool WasLeftMouseJustPressed()
        => CurrentMouseState.LeftButton == ButtonState.Pressed && PreviousMouseState.LeftButton == ButtonState.Released;

    public bool WasMiddleMouseJustPressed()
        => CurrentMouseState.MiddleButton == ButtonState.Pressed && PreviousMouseState.MiddleButton == ButtonState.Released;

    public bool WasRightMouseJustPressed()
        => CurrentMouseState.RightButton == ButtonState.Pressed && PreviousMouseState.RightButton == ButtonState.Released;

    public bool IsLeftMouseDown()
        => CurrentMouseState.LeftButton == ButtonState.Pressed;

    public bool IsMiddleMouseDown()
        => CurrentMouseState.MiddleButton == ButtonState.Pressed;

    public bool IsRightMouseDown()
        => CurrentMouseState.RightButton == ButtonState.Pressed;

    public int GetPressedKeyCount()
        => CurrentKeyState.GetPressedKeyCount();

    public Keys[] GetPressedKeys()
        => CurrentKeyState.GetPressedKeys();

    public int GetPreviousPressedKeyCount()
        => PreviousKeyState.GetPressedKeyCount();

    public Keys[] GetPreviousPressedKeys()
        => PreviousKeyState.GetPressedKeys();

    public bool WasKeyJustPressed(Keys key)
        => CurrentKeyState.IsKeyDown(key) && PreviousKeyState.IsKeyUp(key);

    public bool WasKeyJustPressed(params Keys[] keys)
        => keys.Any(WasKeyJustPressed);

    public bool WasKeyJustReleased(Keys key)
        => CurrentKeyState.IsKeyUp(key) && PreviousKeyState.IsKeyDown(key);

    public bool WasKeyJustReleased(params Keys[] keys)
        => keys.Any(WasKeyJustReleased);

    public bool IsKeyDown(Keys key)
        => CurrentKeyState.IsKeyDown(key);

    public bool IsKeyDown(params Keys[] keys)
        => keys.Any(IsKeyDown);

    public bool WasKeyDown(Keys key)
        => PreviousKeyState.IsKeyDown(key);

    public bool WasKeyDown(params Keys[] keys)
        => keys.Any(WasKeyDown);

    public bool IsKeyHeld(Keys key)
        => CurrentKeyState.IsKeyDown(key) && PreviousKeyState.IsKeyDown(key);

    public bool IsKeyHeld(params Keys[] keys)
        => keys.Any(IsKeyHeld);

    public bool IsKeyHeldLong(Keys key)
        => IsKeyHeld(key) && PreviousKeyStates[^2].IsKeyDown(key) && PreviousKeyStates[^3].IsKeyDown(key) && PreviousKeyStates[^4].IsKeyDown(key) && PreviousKeyStates[^5].IsKeyDown(key);

}
