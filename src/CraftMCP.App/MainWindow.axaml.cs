using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CraftMCP.App.Dialogs;
using CraftMCP.App.Models.Session;
using CraftMCP.App.ViewModels;

namespace CraftMCP.App;

public partial class MainWindow : Window
{
    public WorkspaceViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new WorkspaceViewModel();
        DataContext = ViewModel;
        Closed += (_, _) => ViewModel.Dispose();
    }

    private async void OnNewClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewDocumentDialog();
        var result = await dialog.ShowDialog<bool?>(this);
        if (result == true && dialog.SelectedPreset is not null)
        {
            ViewModel.CreateNewDocument(dialog.SelectedPreset, dialog.DocumentName);
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenPathAsync("Craft document", ["*.craft"]);
        if (path is not null)
        {
            ViewModel.OpenDocument(path);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.DocumentPath))
        {
            ViewModel.SaveDocument(ViewModel.DocumentPath);
            return;
        }

        await SaveAsAsync();
    }

    private async void OnSaveAsClick(object? sender, RoutedEventArgs e) => await SaveAsAsync();

    private async void OnExportPngClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickSavePathAsync($"{ViewModel.Document.Name}.png", "PNG image", ["*.png"]);
        if (path is not null)
        {
            ViewModel.ExportPng(path);
        }
    }

    private async void OnExportJsonClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickSavePathAsync($"{ViewModel.Document.Name}.json", "JSON file", ["*.json"]);
        if (path is not null)
        {
            ViewModel.ExportJson(path);
        }
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e) => ViewModel.Undo();

    private void OnRedoClick(object? sender, RoutedEventArgs e) => ViewModel.Redo();

    private void OnToolClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && Enum.TryParse<ToolMode>(button.Tag?.ToString(), out var toolMode))
        {
            ViewModel.SelectTool(toolMode);
        }
    }

    private async void OnImageToolClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenPathAsync("Image", ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp"]);
        if (path is not null)
        {
            ViewModel.PrepareImageCreation(path);
        }
    }

    private void OnDeleteSelectionClick(object? sender, RoutedEventArgs e) => ViewModel.DeleteSelection();

    private void OnDuplicateSelectionClick(object? sender, RoutedEventArgs e) => ViewModel.DuplicateSelection();

    private void OnGroupSelectionClick(object? sender, RoutedEventArgs e) => ViewModel.GroupSelection();

    private void OnUngroupSelectionClick(object? sender, RoutedEventArgs e) => ViewModel.UngroupSelection();

    private void OnLayerSelectClick(object? sender, RoutedEventArgs e) => WithTaggedNode(sender, ViewModel.SelectLayerNode);

    private void OnLayerVisibilityClick(object? sender, RoutedEventArgs e) => WithTaggedNode(sender, ViewModel.ToggleVisibility);

    private void OnLayerLockClick(object? sender, RoutedEventArgs e) => WithTaggedNode(sender, ViewModel.ToggleLock);

    private void OnLayerMoveUpClick(object? sender, RoutedEventArgs e) => WithTaggedNode(sender, nodeId => ViewModel.MoveLayer(nodeId, -1));

    private void OnLayerMoveDownClick(object? sender, RoutedEventArgs e) => WithTaggedNode(sender, nodeId => ViewModel.MoveLayer(nodeId, 1));

    private void OnApplyCanvasClick(object? sender, RoutedEventArgs e) => ViewModel.ApplyCanvasProperties();

    private void OnApplySelectionClick(object? sender, RoutedEventArgs e) => ViewModel.ApplySelectionProperties();

    private async Task SaveAsAsync()
    {
        var path = await PickSavePathAsync($"{ViewModel.Document.Name}.craft", "Craft document", ["*.craft"]);
        if (path is not null)
        {
            ViewModel.SaveDocument(path);
        }
    }

    private void WithTaggedNode(object? sender, Action<string> action)
    {
        if (sender is Button button && button.Tag is string nodeIdText)
        {
            action(nodeIdText);
        }
    }

    private async Task<string?> PickOpenPathAsync(string name, string[] patterns)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = name,
                FileTypeFilter =
                [
                    new FilePickerFileType(name) { Patterns = patterns },
                ],
            });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private async Task<string?> PickSavePathAsync(string suggestedName, string name, string[] patterns)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                SuggestedFileName = suggestedName,
                Title = name,
                FileTypeChoices =
                [
                    new FilePickerFileType(name) { Patterns = patterns },
                ],
            });

        return file?.TryGetLocalPath();
    }
}
