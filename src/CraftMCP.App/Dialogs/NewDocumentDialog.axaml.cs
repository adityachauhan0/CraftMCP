using Avalonia.Controls;
using CraftMCP.App.Models;

namespace CraftMCP.App.Dialogs;

public partial class NewDocumentDialog : Window
{
    public NewDocumentDialog()
    {
        InitializeComponent();

        PresetListBox.ItemsSource = DocumentPresetDefinition.BuiltIn;
        PresetListBox.SelectionChanged += (_, _) => SyncFromPreset();
        PresetListBox.SelectedIndex = 0;
    }

    public DocumentPresetDefinition? SelectedPreset { get; private set; }

    public string? DocumentName => DocumentNameTextBox.Text;

    private void SyncFromPreset()
    {
        if (PresetListBox.SelectedItem is not DocumentPresetDefinition preset)
        {
            return;
        }

        SelectedPreset = preset;
        WidthTextBox.Text = preset.Width.ToString("0");
        HeightTextBox.Text = preset.Height.ToString("0");
        DocumentNameTextBox.Text ??= preset.DefaultDocumentName;
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);

    private void OnCreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedPreset is null)
        {
            Close(false);
            return;
        }

        if (!double.TryParse(WidthTextBox.Text, out var width) || width <= 0
            || !double.TryParse(HeightTextBox.Text, out var height) || height <= 0)
        {
            return;
        }

        SelectedPreset = SelectedPreset with
        {
            Width = width,
            Height = height,
            Preset = SelectedPreset.Preset == CraftMCP.Domain.ValueObjects.CanvasPreset.Custom
                ? CraftMCP.Domain.ValueObjects.CanvasPreset.Custom
                : SelectedPreset.Preset,
        };

        Close(true);
    }
}
