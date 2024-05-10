using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneJump.src.engine;

namespace OneJump.src.ui {
    public class MenuLoader {
        public static UIGrid LoadTitleScreen() {
            UIGrid grid = new();
            grid.AddRow(3);
            grid.AddRowPx(60);
            grid.AddRow(3);
            grid.AddRowPx(45);
            grid.AddRow(1);
            grid.AddRowPx(45);
            grid.AddRow(3);
            grid.AddColumn(1);
            grid.AddColumnPx(85);
            grid.AddColumnPx(250);
            grid.AddColumnPx(85);
            grid.AddColumn(1);
            grid.AddComponent(new UIGame(), 0, 6, 0, 4);
            grid.AddComponent(new UITransition(), 0, 6, 0, 4);
            grid.AddComponent(new UIText("OneJump", 12), 1, 1, 1, 3);
            grid.AddComponent(new UIButton("Start", 3, () => {
                if (Main.IrisActive) return;
                float x = Input.X - Main.Viewport.Width  / 2f;
                float y = Input.Y - Main.Viewport.Height / 2f;
                x /= Main.CurrentScene.Tilemap.tileset.TileWidth  * Main.CurrentScene.Scale;
                y /= Main.CurrentScene.Tilemap.tileset.TileHeight * Main.CurrentScene.Scale;
                Main.CurrentScene.SpawnDeathParticles(x, y);
                Main.Iris(x, y, "start", 30, true, () => {
                    Main.CurrentScene = Assets.GetAsset<Scene>("levels/level1.lvl");
                    Main.CurrentScene.Reload();
                    Main.UI = LoadGameScreen();
                    Main.Music = SFXPlayer.Play("sounds/bgm.wav");
                    Main.Music.IsLooped = true;
                });
            }), 3, 3, 2, 2);
            grid.AddComponent(new UIButton("Quit", 3, () => {
                if (Main.IrisActive) return;
                Main.UI = LoadQuitScreen(false);
            }), 5, 5, 2, 2);
            return grid;
        }
        public static UIGrid LoadQuitScreen(bool pauseMenu) {
            UIGrid grid = new();
            grid.AddRow(5);
            grid.AddRowPx(45);
            grid.AddRow(1);
            grid.AddRowPx(45);
            grid.AddRow(5);
            grid.AddColumn(1);
            grid.AddColumnPx(100);
            grid.AddColumnPx(40);
            grid.AddColumnPx(100);
            grid.AddColumn(1);
            if (pauseMenu) grid.AddComponent(new UIGame(), 0, 4, 0, 4);
            grid.AddComponent(new UIText("Are you sure?", 4), 1, 1, 1, 3);
            grid.AddComponent(new UIButton("Yes", 3, () => {
                Main.DoExit = true;
            }), 3, 3, 1, 1);
            grid.AddComponent(new UIButton("No", 3, () => {
                Main.UI = pauseMenu ? LoadPauseMenu() : LoadTitleScreen();
            }, "sounds/back.wav"), 3, 3, 3, 3);
            return grid;
        }
        public static UIGrid LoadGameScreen() {
            UIGrid grid = new();
            grid.AddRow(1);
            grid.AddColumn(1);
            grid.AddComponent(new UIGame(), 0, 0, 0, 0);
            grid.AddComponent(new UITransition(), 0, 0, 0, 0);
            return grid;
        }
        public static UIGrid LoadPauseMenu() {
            static void Unpause() {
                Main.Paused = false;
                Sliders<Scene, float>.Add(Main.CurrentScene, "Scale", Scene.GAME_SCALE, 30, Sliders.FloatInterpolator, Sliders.EasingCubicInOut);
                Entity player = Main.CurrentScene.EntityWithTag("player");
                float backdropScale = 0.8f;
                if (player.GetOrDefaultProperty("can_jump", true)) backdropScale = 1f;
                Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", backdropScale, 30, Sliders.FloatInterpolator, Sliders.EasingCubicInOut);
                Main.UI = LoadGameScreen();
                Main.Music.Resume();
            }
            UIGrid grid = new();
            grid.AddRow(3);
            grid.AddRowPx(40);
            grid.AddRow(3);
            grid.AddRowPx(45);
            grid.AddRow(1);
            grid.AddRowPx(45);
            grid.AddRow(1);
            grid.AddRowPx(45);
            grid.AddRow(3);
            grid.AddColumn(1);
            grid.AddColumnPx(250);
            grid.AddColumn(1);
            grid.AddComponent(new UIGame(), 0, 8, 0, 2);
            grid.AddComponent(new UIText("Paused", 8), 1, 1, 1, 1);
            grid.AddComponent(new UIButton("Resume", 3, Unpause, "sounds/back.wav"), 3, 3, 1, 1);
            grid.AddComponent(new UIButton("Reset", 3, () => {
                Unpause();
                Main.Paused = true;
                Delay.Add(() => {
                    Main.Paused = false;
                    Main.CurrentScene.Die(Main.CurrentScene.EntityWithTag("player"), "reset");
                }, 15);
            }), 5, 5, 1, 1);
            grid.AddComponent(new UIButton("Quit", 3, () => {
                Main.UI = LoadQuitScreen(true);
            }), 7, 7, 1, 1);
            return grid;
        }
    }
}