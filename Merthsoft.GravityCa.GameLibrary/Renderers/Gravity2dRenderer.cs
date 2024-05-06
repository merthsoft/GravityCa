using Merthsoft.GravityCa.GameLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary.Renderers;
internal class Gravity2dRenderer(GravityMap gravityMap, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Point scaledSize)
{
    public static string RenderKey => "Standard";
    public SpriteBatch SpriteBatch = spriteBatch;
    public Point ScreenSize = scaledSize;
        

    private Texture2D BackingTexture = new Texture2D(graphicsDevice, gravityMap.Width, gravityMap.Height);
    private readonly Color[] ColorArray = new Color[gravityMap.Width * gravityMap.Height];

    private GravityMap GravityMap = gravityMap;

    public void Draw(GameTime gameTime)
    {
        if (!GravityGame.DrawMass && !GravityGame.DrawGravity)
            return;

        if (GravityMap.UpdateState != 3 && GravityMap.Running) // Always re-render when it's paused in case things change
        {
            DrawTexture();
            return;
        }

        for (var i = 0; i < GravityMap.Width; i++)
            for (var j = 0; j < GravityMap.Height; j++)
                ColorArray[j * GravityMap.Height + i] = GravityGame.GetColorAt(i, j);

        BackingTexture.SetData(ColorArray);
        DrawTexture();
    }

    private void DrawTexture()
    {
        SpriteBatch.Begin(
                        SpriteSortMode.FrontToBack,
                        BlendState.NonPremultiplied,
                        SamplerState.PointClamp);
        SpriteBatch.Draw(BackingTexture, new Rectangle(Point.Zero, ScreenSize), Color.White);
        SpriteBatch.End();
    }
}
