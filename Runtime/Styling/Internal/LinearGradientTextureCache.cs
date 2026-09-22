#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace LumaFlow {
    internal static class LinearGradientTextureCache {
        private const int TextureSize = 64;
        private static readonly Dictionary<LinearGradient, Texture2D> Textures = new();

        internal static Texture2D Get(LinearGradient gradient) {
            if (Textures.TryGetValue(gradient, out var texture) && texture != null) return texture;
            texture = Create(gradient);
            Textures[gradient] = texture;
            return texture;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            foreach (var texture in Textures.Values) {
                if (texture != null) Object.Destroy(texture);
            }
            Textures.Clear();
        }

        private static Texture2D Create(LinearGradient gradient) {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) {
                name = $"LumaFlow gradient {gradient.Angle:0.##}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var radians = gradient.Angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var extent = Mathf.Abs(direction.x) + Mathf.Abs(direction.y);
            var pixels = new Color[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++) {
                for (var x = 0; x < TextureSize; x++) {
                    var point = new Vector2(
                        x / (TextureSize - 1f) - 0.5f,
                        y / (TextureSize - 1f) - 0.5f);
                    var position = 0.5f + Vector2.Dot(point, direction) / extent;
                    pixels[y * TextureSize + x] = Color.LerpUnclamped(
                        gradient.StartColor,
                        gradient.EndColor,
                        Mathf.Clamp01(position));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
