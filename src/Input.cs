using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace OneJump.src {
    public class Input {
        public static readonly int LeftButton   = 1 << 0;
        public static readonly int RightButton  = 1 << 1;
        public static readonly int MiddleButton = 1 << 2;
        public static readonly int MoveLeft     = 1 << 3;
        public static readonly int MoveRight    = 1 << 4;
        public static readonly int Jump         = 1 << 5;
        public static readonly int PickUp       = 1 << 6;
        public static readonly int Reset        = 1 << 7;
        public static readonly int Pause        = 1 << 8;
        public static readonly int InfJump      = 1 << 9;
        public static int X { get; private set; }
        public static int Y { get; private set; }
        private static int button;
        private static int prevButton;
        private static int buttonPressed;
        private static int buttonReleased;
        private static bool keyboardEnabled = false;
        private static readonly Dictionary<byte, int> keysPressed = new();
        private static void RegisterInputs() {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.onejump-pipe";
            if (File.Exists(path)) {
                ByteArrayReader reader = new(File.ReadAllBytes(path));
                if (reader.Size < 5) return;
                byte key = reader.UByte();
                int delay = reader.SInt();
                if (keysPressed.ContainsKey(key)) keysPressed[key] += delay;
                else keysPressed.Add(key, delay);
                File.Delete(path);
            }
            List<byte> remove = new();
            foreach (KeyValuePair<byte, int> entry in keysPressed) {
                if (keysPressed[entry.Key] <= 0) remove.Add(entry.Key);
                else keysPressed[entry.Key]--;
            }
            foreach (byte key in remove) {
                keysPressed.Remove(key);
            }
            KeyboardState keyboard = Keyboard.GetState();
            if (keyboardEnabled) {
                Keys[] keys = keyboard.GetPressedKeys();
                foreach (Keys key in keys) {
                    keysPressed.Add((byte)key, 0);
                }
            }
        } 
        private static void HandleInputs() {
            MouseState mouse = Mouse.GetState();
            if (mouse.LeftButton == ButtonState.Pressed) button |= LeftButton;
            if (mouse.RightButton == ButtonState.Pressed) button |= RightButton;
            if (mouse.MiddleButton == ButtonState.Pressed) button |= MiddleButton;
            if (keysPressed.ContainsKey((byte)Keys.A)) button |= MoveLeft;
            if (keysPressed.ContainsKey((byte)Keys.D)) button |= MoveRight;
            if (keysPressed.ContainsKey((byte)Keys.Q)) button |= PickUp;
            if (keysPressed.ContainsKey((byte)Keys.R)) button |= Reset;
            if (keysPressed.ContainsKey((byte)Keys.J)) button |= InfJump;
            if (keysPressed.ContainsKey((byte)Keys.K)) keyboardEnabled = true;
            if (keysPressed.ContainsKey((byte)Keys.Escape)) button |= Pause;
            if (keysPressed.ContainsKey((byte)Keys.Space)) button |= Jump;
            X = mouse.X;
            Y = mouse.Y;
        }
        public static void Update() {
            button = 0;
            RegisterInputs();
            HandleInputs();
            buttonPressed  = (prevButton | button) & ~prevButton;
            buttonReleased = (prevButton | button) & ~button;
            prevButton = button;
        }
        private static bool IsFlagSet(int flags, int flag) => (flags & flag) != 0;
        public static bool ButtonPressed(int button) => IsFlagSet(buttonPressed, button);
        public static bool ButtonReleased(int button) => IsFlagSet(buttonReleased, button);
        public static bool ButtonDown(int btn) => IsFlagSet(button, btn);
    }
}