using CraftMCP.App.Models;
using CraftMCP.Domain.Exports;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Persistence.Assets;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.Packaging;
using CraftMCP.Rendering.Assets;
using CraftMCP.Rendering.Export;
using CraftMCP.Rendering.Scene;

namespace CraftMCP.App.Services;

public sealed class WorkspaceDocumentService
{
    private readonly CraftPackageReader _packageReader;
    private readonly CraftPackageWriter _packageWriter;
    private readonly CraftAssetImporter _assetImporter;
    private readonly DocumentRenderPlanBuilder _planBuilder;
    private readonly DocumentPngExporter _pngExporter;

    public WorkspaceDocumentService()
        : this(
            new CraftPackageReader(),
            new CraftPackageWriter(),
            new CraftAssetImporter(),
            new DocumentRenderPlanBuilder(),
            new DocumentPngExporter())
    {
    }

    internal WorkspaceDocumentService(
        CraftPackageReader packageReader,
        CraftPackageWriter packageWriter,
        CraftAssetImporter assetImporter,
        DocumentRenderPlanBuilder planBuilder,
        DocumentPngExporter pngExporter)
    {
        _packageReader = packageReader;
        _packageWriter = packageWriter;
        _assetImporter = assetImporter;
        _planBuilder = planBuilder;
        _pngExporter = pngExporter;
    }

    public DocumentState CreateNewDocument(DocumentPresetDefinition preset, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return new DocumentState(
            StableIdGenerator.CreateDocumentId(),
            SchemaVersion.V1,
            string.IsNullOrWhiteSpace(name) ? preset.DefaultDocumentName : name.Trim(),
            new CanvasModel(
                preset.Width,
                preset.Height,
                preset.Preset,
                preset.Background,
                preset.SafeArea),
            new Dictionary<NodeId, CraftMCP.Domain.Nodes.NodeBase>(),
            Array.Empty<NodeId>(),
            new Dictionary<AssetId, AssetManifestEntry>());
    }

    public WorkspaceDocumentSnapshot Load(string path)
    {
        var result = _packageReader.Read(path);
        return new WorkspaceDocumentSnapshot(
            result.Package.Document,
            result.Package.Assets.ToDictionary(entry => entry.Key, entry => entry.Value),
            Path.GetFullPath(path),
            result.Warnings.Select(warning => warning.Message).ToArray());
    }

    public void Save(
        string path,
        DocumentState document,
        IReadOnlyDictionary<AssetId, PackagedAssetContent> assets)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assets);

        var package = new CraftPackageDocument(document, assets);
        _packageWriter.Save(path, package);
    }

    public void ExportJson(string path, DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);
        File.WriteAllText(path, DocumentJsonExporter.Serialize(document));
    }

    public WorkspaceExportResult ExportPng(
        string path,
        DocumentState document,
        IReadOnlyDictionary<AssetId, PackagedAssetContent> assets)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assets);

        var plan = _planBuilder.Build(document);
        var assetSource = new InMemoryRenderAssetSource(
            assets.ToDictionary(entry => entry.Key, entry => entry.Value.Bytes));
        var export = _pngExporter.Export(plan, assetSource);
        File.WriteAllBytes(path, export.PngBytes);

        return new WorkspaceExportResult(export.Warnings.Select(warning => warning.Message).ToArray());
    }

    public PackagedAssetContent ImportAsset(string path) => _assetImporter.Import(path);
}

public sealed record WorkspaceDocumentSnapshot(
    DocumentState Document,
    IReadOnlyDictionary<AssetId, PackagedAssetContent> Assets,
    string Path,
    IReadOnlyList<string> Warnings);

public sealed record WorkspaceExportResult(IReadOnlyList<string> Warnings);
