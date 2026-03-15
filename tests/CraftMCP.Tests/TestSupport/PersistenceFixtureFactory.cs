using System.Security.Cryptography;
using System.Text.Json;
using CraftMCP.Domain.Exports;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Serialization;
using CraftMCP.Persistence.Contracts;

namespace CraftMCP.Tests.TestSupport;

internal static class PersistenceFixtureFactory
{
    public static IEnumerable<object[]> RoundTripCases()
    {
        yield return new object[] { "social-graphic", CreateSocialGraphicPackage() };
        yield return new object[] { "ui-mockup", CreateUiMockupPackage() };
        yield return new object[] { "slide", CreateSlidePackage() };
    }

    public static CraftPackageDocument CreateSocialGraphicPackage()
    {
        var document = DocumentExportFixtureFactory.CreateSocialGraphic();
        return CreatePackage(
            document,
            new Dictionary<AssetId, byte[]>
            {
                [document.Assets.Keys.Single()] = FixtureFile.ReadCraftAsset("social-hero.png"),
            });
    }

    public static CraftPackageDocument CreateUiMockupPackage()
    {
        var document = DocumentExportFixtureFactory.CreateUiMockup();
        return CreatePackage(
            document,
            new Dictionary<AssetId, byte[]>
            {
                [document.Assets.Keys.Single()] = FixtureFile.ReadCraftAsset("avatar.png"),
            });
    }

    public static CraftPackageDocument CreateSlidePackage()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        return new CraftPackageDocument(document, new Dictionary<AssetId, PackagedAssetContent>());
    }

    public static string ExportJson(CraftPackageDocument packageDocument) =>
        DocumentJsonExporter.Serialize(packageDocument.Document);

    public static string SerializeCanonicalDocument(DocumentState document) =>
        JsonSerializer.Serialize(document, CraftJsonSerializerOptions.Default);

    private static CraftPackageDocument CreatePackage(DocumentState source, IReadOnlyDictionary<AssetId, byte[]> assetBytes)
    {
        var assetMap = source.Assets.Values.ToDictionary(
            manifest => manifest.Id,
            manifest => CreateAsset(manifest, assetBytes[manifest.Id]));
        var manifestMap = assetMap.ToDictionary(entry => entry.Key, entry => entry.Value.Manifest);
        var document = source with { Assets = manifestMap };
        return new CraftPackageDocument(document, assetMap);
    }

    private static PackagedAssetContent CreateAsset(AssetManifestEntry existingManifest, byte[] bytes)
    {
        var extension = Path.GetExtension(existingManifest.FileName);
        var contentHash = ComputeSha256(bytes);
        var manifest = new AssetManifestEntry(existingManifest.Id, existingManifest.FileName, existingManifest.MediaType, contentHash);
        return new PackagedAssetContent(manifest, $"assets/{contentHash}{extension.ToLowerInvariant()}", bytes);
    }

    private static string ComputeSha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
