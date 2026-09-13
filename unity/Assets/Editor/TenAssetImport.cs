using UnityEditor;

namespace Ten.Editor
{
    /// <summary>
    /// Codex の包みから持ってきた画像の取り込み設定（presentation.md 2.3）。
    ///
    /// **9 スライスのボタンは縮小・圧縮で角が滲む。**滲むと透明と不透明の間に半透明の画素ができ、
    /// 「半透明を使わない」（ASTRA-COMMON.md 4 節）が取り込みの段階で崩れる。
    /// </summary>
    public sealed class TenAssetImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Ui/") && !assetPath.StartsWith("Assets/Art/Icon/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = assetPath.StartsWith("Assets/Resources/Ui/")
                ? UnityEngine.FilterMode.Point
                : UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
        }
    }
}
