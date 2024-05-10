using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using OneJump.src;
using OneJump.src.engine;
using OneJump.src.ui;

namespace OneJump;

public class Main : Game {
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    public static RenderTarget2D irisRenderTarget;
    public static RenderTarget2D mainRenderTarget;
    public static RenderTarget2D gameRenderTarget;
    public static BasicEffect shader;
    public static UIGrid UI { get; set; }
    public static Color GameColor { get; set; } = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    public static int GlobalTimer { get; set; }
    public static Tileset Tileset { get; private set; }
    public static Scene CurrentScene { get; set; }
    public static bool DoExit { get; set; }
    public static bool Paused { get; set; }
    public static Viewport Viewport { get; private set; }
    public static float IrisSize { get; set; } = 1f;
    public static Vector2 IrisPos { get; private set; } = new();
    public static string IrisText { get; private set; } = "";
    public static bool IrisActive { get; set; } = false;
    public static SoundEffectInstance Music { get; set; }
    public Main() {
        graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }
    protected override void LoadContent() {
        Tileset = Tiles.CreateTileset();
        Assets.LoadAssets("assets.bin", GraphicsDevice);
        Utils.CreateWhitePixel(GraphicsDevice);
        UI = MenuLoader.LoadTitleScreen();
        CurrentScene = Assets.GetAsset<Scene>("levels/level0.lvl");
        CurrentScene.Scale = 4;
        spriteBatch = new SpriteBatch(GraphicsDevice);
        shader = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true };
    }
    protected override void Update(GameTime gameTime) {
        if (DoExit) Exit();
        Input.Update();
        UI.Update();
        base.Update(gameTime);
        Sliders.Update();
        Delay.Update();
        SFXPlayer.DisposeFinished();
        GlobalTimer++;
    }
    protected override void Draw(GameTime gameTime) {
        Viewport = GraphicsDevice.Viewport;

        int w = GraphicsDevice.Viewport.Width, h = GraphicsDevice.Viewport.Height;

        shader.World = Matrix.Identity;
        shader.View = Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up);
        shader.Projection = Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1);

        mainRenderTarget = new RenderTarget2D(GraphicsDevice, w, h, false, SurfaceFormat.Vector4, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
        irisRenderTarget = new RenderTarget2D(GraphicsDevice, w, h, false, SurfaceFormat.Vector4, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
        gameRenderTarget = CurrentScene?.Render(spriteBatch);

        GraphicsDevice.SetRenderTarget(irisRenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        spriteBatch.DrawRect(0, 0, w, h, Color.White);
        float width  = (IrisText.Length * 5 + (IrisText.Length - 1)) * 6;
        float height = 5 * 6;
        float tx = (w - width ) / 2;
        float ty = (h - height) / 2;
        spriteBatch.DrawText(IrisText, tx, ty, 6, Color.Black);
        spriteBatch.End();
        float x = 0, y = 0, r = 0;
        CurrentScene?.CalculateIrisParams(out x, out y, out r);
        GraphicsDevice.BlendState = new BlendState {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaDestinationBlend = Blend.SourceAlpha,
            ColorBlendFunction = BlendFunction.Subtract,
            AlphaBlendFunction = BlendFunction.Subtract
        };
        if (IrisSize >= 0) spriteBatch.DrawCircle(x, y, r * IrisSize, 128, Color.Transparent);
        
        GraphicsDevice.SetRenderTarget(mainRenderTarget);
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        UI.Render(spriteBatch);
        spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        spriteBatch.Begin();
        spriteBatch.Draw(mainRenderTarget, new Vector2(0, 0), Color.White);
        spriteBatch.End();
        
        base.Draw(gameTime);
        mainRenderTarget .Dispose();
        irisRenderTarget .Dispose();
        gameRenderTarget?.Dispose();
    }
    public static void Iris(float x, float y, string text, int delay, bool fadeOut, Action action, Func<float, float> interpolation = null) {
        interpolation ??= fadeOut ? Sliders.EasingQuadraticIn : Sliders.EasingQuadraticOut;
        IrisPos = new(x, y);
        IrisText = text;
        IrisActive = true;
        Sliders<Main, float>.Add(null, "IrisSize", fadeOut ? 0 : 1, delay, Sliders.FloatInterpolator, interpolation, () => {
            IrisActive = false;
            action?.Invoke();
        });
    }
}
