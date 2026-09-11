#nullable enable

using LumaFlow;
using UnityEditor;
using Framework = LumaFlow.LumaFlow;

public sealed class LumaFlowComponentGalleryWindow : EditorWindow
{
    private readonly OverlayController _overlay = new();
    private MountHandle? _mount;

    [MenuItem("Window/LumaFlow/Component Gallery")]
    public static void Open() => GetWindow<LumaFlowComponentGalleryWindow>("LumaFlow Gallery");

    public void CreateGUI()
    {
        _mount?.Dispose();
        rootVisualElement.Clear();
        var gallery = new LumaFlowGalleryApp(_overlay);
        _mount = Framework.Mount(
            new Theme(
                LumaFlowGalleryTheme.Data,
                new BackNavigation(
                    new OverlayHost(gallery, _overlay),
                    gallery.Navigator,
                    _overlay)),
            rootVisualElement);
    }

    public void OnDisable() => _mount?.Dispose();
}
