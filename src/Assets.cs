using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using OneJump.src.engine;

namespace OneJump.src {
    internal interface IAssetLoader {
        object LoadAsset(byte[] data, params object[] args);
    }
    public class Assets {
        private static readonly Dictionary<string, object> files = new();
        private static IAssetLoader GetLoader(string extension) => extension switch {
            "png" => new TextureAssetLoader(),
            "wav" => new SoundAssetLoader(),
            "txt" => new StringAssetLoader(),
            "lvl" => new LevelAssetLoader(),
            _ => new BinaryAssetLoader(),
        };
        public static void LoadAssets(string filename, params object[] args) {
            byte[] data = File.ReadAllBytes(filename);
            ByteArrayReader reader = new(data);
            while (true) {
                string name = reader.String();
                if (name == "") break;
                int size = reader.SInt();
                byte[] filedata = reader.Binary(size);
                string extension = Regex.Replace(name, ".*\\.", "");
                IAssetLoader loader = GetLoader(extension);
                files.Add(name, loader.LoadAsset(filedata, args));
            }
        }
        public static T GetAsset<T>(string name) {
            if (files.ContainsKey(name)) return (T)files[name];
            Console.WriteLine("Asset '" + name + "' not found!");
            return default;
        }
    }
    internal class TextureAssetLoader : IAssetLoader {
        public object LoadAsset(byte[] data, params object[] args) {
            return Texture2D.FromStream((GraphicsDevice)args[0], new MemoryStream(data));
        }
    }
    internal class SoundAssetLoader : IAssetLoader {
        public object LoadAsset(byte[] data, params object[] args) {
            return SoundEffect.FromStream(new MemoryStream(data));
        }
    }
    internal class StringAssetLoader : IAssetLoader {
        public object LoadAsset(byte[] data, params object[] args) {
            char[] chars = new char[data.Length];
            for (int i = 0; i < data.Length; i++) {
                chars[i] = (char)data[i];
            }
            return new string(chars);
        }
    }
    internal class LevelAssetLoader : IAssetLoader {
        public object LoadAsset(byte[] data, params object[] args) {
            return new Scene(data);
        }
    }
    internal class BinaryAssetLoader : IAssetLoader {
        public object LoadAsset(byte[] data, params object[] args) {
            return data;
        }
    }
}