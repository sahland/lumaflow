#nullable enable

using LumaFlow;
using UnityEngine;

internal static class LumaFlowGalleryTheme
{
    public static readonly ThemeData Data = Create();

    private static ThemeData Create()
    {
        var colors = new ColorScheme(
            canvas: new Color(0.035f, 0.045f, 0.075f),
            surface: new Color(0.075f, 0.09f, 0.14f),
            surfaceVariant: new Color(0.12f, 0.14f, 0.21f),
            primary: new Color(0.42f, 0.72f, 1f),
            primaryContainer: new Color(0.10f, 0.24f, 0.42f),
            onPrimary: new Color(0.02f, 0.09f, 0.15f),
            onSurface: new Color(0.94f, 0.96f, 1f),
            onSurfaceVariant: new Color(0.68f, 0.74f, 0.85f),
            outline: new Color(0.20f, 0.25f, 0.36f));
        var typography = new TypographyTheme(
            title: new TextStyle(colors.OnSurface, 26f, FontStyle.Bold),
            headline: new TextStyle(colors.OnSurface, 17f, FontStyle.Bold),
            body: new TextStyle(colors.OnSurfaceVariant, 14f),
            label: new TextStyle(colors.OnSurface, 13f, FontStyle.Bold));
        var spacing = new SpacingTheme(8f, 12f, 16f, 20f, 28f);
        var radius = new RadiusTheme(BorderRadius.All(12f), BorderRadius.All(18f), BorderRadius.All(24f));
        var buttons = new ButtonTheme(
            primary: new ButtonStyle(
                background: colors.Primary,
                foreground: colors.OnPrimary,
                padding: EdgeInsets.Symmetric(horizontal: 18f, vertical: 10f),
                shape: radius.Small,
                typography: typography.Label,
                minimumSize: new Vector2(96f, 40f),
                hovered: new ButtonStateStyle(background: new Color(0.56f, 0.80f, 1f)),
                pressed: new ButtonStateStyle(background: new Color(0.28f, 0.59f, 0.88f))),
            secondary: new ButtonStyle(
                background: colors.SurfaceVariant,
                foreground: colors.OnSurface,
                padding: EdgeInsets.Symmetric(horizontal: 16f, vertical: 10f),
                shape: radius.Small,
                typography: typography.Label,
                minimumSize: new Vector2(96f, 40f),
                hovered: new ButtonStateStyle(background: new Color(0.17f, 0.20f, 0.30f)),
                pressed: new ButtonStateStyle(background: new Color(0.09f, 0.11f, 0.17f))));
        var textFields = new TextFieldTheme(
            new TextFieldStyle(
                background: colors.SurfaceVariant,
                foreground: colors.OnSurface,
                border: colors.Outline,
                placeholder: colors.OnSurfaceVariant,
                typography: typography.Body,
                labelStyle: typography.Label,
                supportingStyle: typography.Body,
                padding: EdgeInsets.Symmetric(horizontal: 14f, vertical: 10f),
                shape: radius.Small,
                focused: new TextFieldStateStyle(border: colors.Primary),
                disabled: new TextFieldStateStyle(foreground: colors.OnSurfaceVariant),
                error: new TextFieldStateStyle(border: new Color(1f, 0.40f, 0.45f))));
        return new ThemeData(
            colors,
            typography,
            spacing,
            radius,
            buttons,
            new IconThemeData(size: 20f, color: colors.OnSurfaceVariant),
            textFields);
    }
}
