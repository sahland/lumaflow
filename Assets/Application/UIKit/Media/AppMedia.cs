#nullable enable

using Assets.Application.UIKit.Tokens;
using LumaFlow;
using UnityEngine;

namespace Assets.Application.UIKit.Media {
    /// <summary>Owns application media paths and lazily loads the textures used by the dogfood UI.</summary>
    public static class AppMedia {
        public static Texture2D Avatar => Load("avatars/avatar_ui");
        public static Texture2D GreenfieldTower => Load("thumbnails/greenfield_tower");
        public static Texture2D RiversideApartments => Load("thumbnails/riverside_apartments");
        public static Texture2D SunsetPlaza => Load("thumbnails/sunset_plaza");
        public static Texture2D MetroStation => Load("thumbnails/metro_station");
        public static Texture2D OakwoodVillas => Load("thumbnails/oakwood_villas");

        public static Image ProjectThumbnail(string mediaId, float size, string label) =>
            new(
                Load($"thumbnails/{mediaId}"),
                size,
                size,
                ImageFit.Cover,
                semanticsLabel: label,
                borderRadius: BorderRadius.All(AppRadius.Sm));

        private static Texture2D Load(string path) {
            return Resources.Load<Texture2D>($"LumaFlow/Images/{path}")
                ?? throw new System.InvalidOperationException($"Missing LumaFlow application media at '{path}'.");
        }
    }
}
