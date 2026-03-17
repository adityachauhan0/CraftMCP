using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using CraftMCP.Agent;
using CraftMCP.App.Infrastructure;
using CraftMCP.App.Models;
using CraftMCP.App.Models.Session;
using CraftMCP.App.Services;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Persistence.Contracts;

namespace CraftMCP.App.ViewModels;

public sealed partial class WorkspaceViewModel : ObservableObject, IDisposable
{
    private const double FitPadding = 64d;
    private const double MinimumZoom = 0.1d;
    private const double MaximumZoom = 8d;

    private readonly WorkspaceDocumentService _documentService;
    private readonly WorkspaceCommandDispatcher _commandDispatcher;
    private readonly WorkspaceAgentService _agentService;
    private readonly WorkspaceRenderer _renderer;
    private readonly DocumentHitTester _hitTester;
    private readonly NodeFactory _nodeFactory;
    private readonly ObservableCollection<LayerItemViewModel> _layerItems = [];
    private readonly ObservableCollection<WorkspaceActivityEntry> _activityEntries = [];

    private DocumentState _document;
    private DocumentState? _previewDocument;
    private IReadOnlyDictionary<AssetId, PackagedAssetContent> _assets;
    private CommandHistoryStack _history;
    private WorkspaceSessionState _sessionState;
    private Bitmap? _canvasBitmap;
    private Size _surfaceSize;
    private string? _documentPath;
    private string _statusMessage;
    private string _renderWarningsText;
    private string _canvasWidthText = string.Empty;
    private string _canvasHeightText = string.Empty;
    private string _canvasBackgroundText = string.Empty;
    private string _nameText = string.Empty;
    private string _xText = string.Empty;
    private string _yText = string.Empty;
    private string _rotationText = string.Empty;
    private string _opacityText = string.Empty;
    private string _widthText = string.Empty;
    private string _heightText = string.Empty;
    private string _fillText = string.Empty;
    private string _strokeText = string.Empty;
    private string _strokeWidthText = string.Empty;
    private string _cornerRadiusText = string.Empty;
    private string _textContent = string.Empty;
    private string _fontFamilyText = string.Empty;
    private string _fontSizeText = string.Empty;
    private string _fontWeightText = string.Empty;
    private string _alignmentText = string.Empty;
    private string _fitModeText = string.Empty;
    private string _startXText = string.Empty;
    private string _startYText = string.Empty;
    private string _endXText = string.Empty;
    private string _endYText = string.Empty;
    private string _promptText = string.Empty;
    private bool _isDirty;
    private PlannerOutput? _currentProposal;
    private PackagedAssetContent? _pendingImageAsset;
    private WorkspaceInteractionMode _interactionMode;
    private CanvasHandleKind _activeHandle;
    private Point _interactionStartScreenPoint;
    private Point _interactionStartCanvasPoint;
    private ViewportState _interactionStartViewport = ViewportState.Default;
    private IReadOnlyDictionary<NodeId, NodeBase> _interactionStartNodes = new Dictionary<NodeId, NodeBase>();
    private Rect _interactionStartBounds;
    private Point _rotationAnchor;
    private CraftMCP.Rendering.Scene.DocumentRenderPlan? _currentRenderPlan;

    public WorkspaceViewModel()
        : this(
            new WorkspaceDocumentService(),
            new WorkspaceCommandDispatcher(),
            new WorkspaceRenderer(),
            new DocumentHitTester(),
            new NodeFactory(),
            new WorkspaceAgentService())
    {
    }

    public WorkspaceViewModel(
        WorkspaceDocumentService documentService,
        WorkspaceCommandDispatcher commandDispatcher,
        WorkspaceRenderer renderer,
        DocumentHitTester hitTester,
        NodeFactory nodeFactory,
        WorkspaceAgentService agentService)
    {
        _documentService = documentService;
        _commandDispatcher = commandDispatcher;
        _agentService = agentService;
        _renderer = renderer;
        _hitTester = hitTester;
        _nodeFactory = nodeFactory;

        _document = _documentService.CreateNewDocument(DocumentPresetDefinition.BuiltIn[0]);
        _assets = new Dictionary<AssetId, PackagedAssetContent>();
        _history = CommandHistoryStack.Empty;
        _sessionState = WorkspaceSessionState.Default;
        _statusMessage = "Ready.";
        _renderWarningsText = string.Empty;

        RefreshEditorState();
        RefreshLayers();
    }

    public ObservableCollection<LayerItemViewModel> LayerItems => _layerItems;

    public ObservableCollection<WorkspaceActivityEntry> ActivityEntries => _activityEntries;

    public WorkspaceSessionState SessionState => _sessionState;

    public PlannerOutput? CurrentProposal => _currentProposal;

    public DocumentState Document => _document;

    public Bitmap? CanvasBitmap => _canvasBitmap;

    public string DocumentTitle =>
        _documentPath is null
            ? $"{_document.Name}{(_isDirty ? " *" : string.Empty)}"
            : $"{Path.GetFileName(_documentPath)}{(_isDirty ? " *" : string.Empty)}";

    public string? DocumentPath => _documentPath;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RenderWarningsText
    {
        get => _renderWarningsText;
        private set => SetProperty(ref _renderWarningsText, value);
    }

    public bool HasRenderWarnings => !string.IsNullOrWhiteSpace(RenderWarningsText);

    public bool IsDirty => _isDirty;

    public bool CanUndo => _history.UndoEntries.Count > 0;

    public bool CanRedo => _history.RedoEntries.Count > 0;

    public bool HasSelection => _sessionState.Selection.HasSelection;

    public bool HasSingleSelection => _sessionState.Selection.HasSingleSelection;

    public bool HasMultipleSelection => _sessionState.Selection.HasMultipleSelection;

    public bool ShowCanvasEditor => !HasSelection;

    public bool ShowSingleNodeEditor => HasSingleSelection;

    public bool ShowMultiSelectionEditor => HasMultipleSelection;

    public bool ShowShapeEditor => SelectedNode is RectangleNode or CircleNode;

    public bool ShowTextEditor => SelectedNode is TextNode;

    public bool ShowImageEditor => SelectedNode is ImageNode;

    public bool ShowLineEditor => SelectedNode is LineNode;

    public bool ShowGroupEditor => SelectedNode is GroupNode;

    public string SelectionSummaryText =>
        HasMultipleSelection
            ? $"{_sessionState.Selection.SelectedNodeIds.Count} nodes selected"
            : SelectedNode is null
                ? "Canvas"
                : $"{SelectedNode.Kind} selected";

    public string ActiveToolText => _sessionState.ToolMode.ToString();

    public string WorkspaceContextText =>
        _documentPath is null
            ? "Unsaved local workspace"
            : Path.GetFileName(_documentPath);

    public string WorkspaceStateText =>
        HasProposal
            ? "Proposal in review"
            : _isDirty
                ? "Unsaved changes"
                : "Ready";

    public string PromptScopeText => DescribeCurrentPromptScope();

    public string PromptScopeDetailText => DescribeCurrentPromptScopeDetail();

    public string PromptExpectationText => "Planner proposes. You review before apply.";

    public string PlannerSurfaceText => "Local planner only";

    public string McpSurfaceText => "External MCP execution is not wired in this MVP.";

    public string InspectorIntroText =>
        _currentProposal is null
            ? "Direct edits and layers stay available while review remains explicit."
            : "Direct edits stay available while the pending proposal remains in review.";

    public string AgentContextSummaryText =>
        _currentProposal is null
            ? "Manual edits remain available while agent review stays explicit."
            : _currentProposal.CanApprove
                ? "This proposal can be approved or rejected before any mutation is applied."
                : "This proposal needs revision before it can be applied.";

    public string AgentContextSelectionText =>
        HasMultipleSelection
            ? $"{_sessionState.Selection.SelectedNodeIds.Count} selected layers"
            : SelectedNode is null
                ? "No layer selected."
                : $"{SelectedNode.Kind} '{SelectedNode.Name}'";

    public string AgentContextSelectionDetailText =>
        SelectedNode is null
            ? "Select a layer to give the planner object-specific context. Without a selection, the local planner can only handle whole-document or canvas-background prompts."
            : $"{SelectedNode.Kind} '{SelectedNode.Name}' is {(SelectedNode.IsVisible ? "visible" : "hidden")} and {(SelectedNode.IsLocked ? "locked" : "unlocked")} in the current workspace state.";

    public string AgentContextProposalImpactText =>
        _currentProposal is null
            ? HasSelection
                ? "No pending proposal is targeting this selection."
                : "No pending proposal yet. Submit a prompt to review a batch before anything mutates."
            : ProposalTargetsCurrentSelection()
                ? "Pending proposal targets this selection and still requires explicit approval before any mutation."
                : $"Pending proposal is scoped to {ProposalScopeText.ToLowerInvariant()}.";

    public string AgentContextRecentActivityText =>
        _activityEntries.FirstOrDefault(entry => entry.SourceLabel == "Agent") is { } activity
            ? $"Latest agent activity: {activity.Summary}"
            : "No agent activity yet.";

    public string CanvasWidthText { get => _canvasWidthText; set => SetProperty(ref _canvasWidthText, value); }

    public string CanvasHeightText { get => _canvasHeightText; set => SetProperty(ref _canvasHeightText, value); }

    public string CanvasBackgroundText { get => _canvasBackgroundText; set => SetProperty(ref _canvasBackgroundText, value); }

    public string NameText { get => _nameText; set => SetProperty(ref _nameText, value); }

    public string XText { get => _xText; set => SetProperty(ref _xText, value); }

    public string YText { get => _yText; set => SetProperty(ref _yText, value); }

    public string RotationText { get => _rotationText; set => SetProperty(ref _rotationText, value); }

    public string OpacityText { get => _opacityText; set => SetProperty(ref _opacityText, value); }

    public string WidthText { get => _widthText; set => SetProperty(ref _widthText, value); }

    public string HeightText { get => _heightText; set => SetProperty(ref _heightText, value); }

    public string FillText { get => _fillText; set => SetProperty(ref _fillText, value); }

    public string StrokeText { get => _strokeText; set => SetProperty(ref _strokeText, value); }

    public string StrokeWidthText { get => _strokeWidthText; set => SetProperty(ref _strokeWidthText, value); }

    public string CornerRadiusText { get => _cornerRadiusText; set => SetProperty(ref _cornerRadiusText, value); }

    public string TextContent { get => _textContent; set => SetProperty(ref _textContent, value); }

    public string FontFamilyText { get => _fontFamilyText; set => SetProperty(ref _fontFamilyText, value); }

    public string FontSizeText { get => _fontSizeText; set => SetProperty(ref _fontSizeText, value); }

    public string FontWeightText { get => _fontWeightText; set => SetProperty(ref _fontWeightText, value); }

    public string AlignmentText { get => _alignmentText; set => SetProperty(ref _alignmentText, value); }

    public string FitModeText { get => _fitModeText; set => SetProperty(ref _fitModeText, value); }

    public string StartXText { get => _startXText; set => SetProperty(ref _startXText, value); }

    public string StartYText { get => _startYText; set => SetProperty(ref _startYText, value); }

    public string EndXText { get => _endXText; set => SetProperty(ref _endXText, value); }

    public string EndYText { get => _endYText; set => SetProperty(ref _endYText, value); }

    public string PromptText
    {
        get => _promptText;
        set
        {
            if (SetProperty(ref _promptText, value))
            {
                OnPropertyChanged(nameof(CanSubmitPrompt));
            }
        }
    }

    public bool CanSubmitPrompt => !string.IsNullOrWhiteSpace(PromptText);

    public bool HasProposal => _currentProposal is not null;

    public bool CanApproveProposal => _currentProposal?.CanApprove ?? false;

    public bool CanRejectProposal => _currentProposal is not null;

    public bool HasProposalRationale => !string.IsNullOrWhiteSpace(_currentProposal?.Rationale);

    public bool HasProposalIssues => _currentProposal is { Warnings.Count: > 0 } || _currentProposal is { Errors.Count: > 0 };

    public string ProposalStatusText => _currentProposal is null ? "No pending proposal." : _currentProposal.Status.ToString();

    public string ProposalSummaryText => _currentProposal?.Summary ?? "Submit a prompt to review a proposal.";

    public string ProposalRationaleText => _currentProposal?.Rationale ?? string.Empty;

    public string ProposalScopeText =>
        _currentProposal?.Batch is null
            ? "No pending scope."
            : DescribeBatchScope(_currentProposal.Batch, "Affects");

    public string ProposalChangeCountText =>
        _currentProposal?.Batch is null
            ? "No proposed changes"
            : FormatChangeCount(_currentProposal.Batch.Commands.Count);

    public string ProposalChangeSummaryText =>
        _currentProposal?.Batch is null
            ? "Submit a prompt to review proposed changes."
            : string.Join(
                Environment.NewLine,
                _currentProposal.Batch.Commands.Select(DescribeProposalChange));

    public string ProposalReviewHintText =>
        _currentProposal is null
            ? "Planner output stays read-only until you explicitly approve it."
            : "Approve applies this batch through the same command history used for direct edits.";

    public string PendingProposalBadgeText =>
        _currentProposal is null
            ? string.Empty
            : "Pending proposal • Review before apply";

    public string ProposalCommandPreviewText =>
        _currentProposal?.Batch is null
            ? string.Empty
            : string.Join(
                Environment.NewLine,
                _currentProposal.Batch.Commands.Select((command, index) => $"{index + 1}. {DescribeCommand(command)}"));

    public string ProposalIssueText =>
        _currentProposal is null
            ? string.Empty
            : string.Join(
                Environment.NewLine,
                _currentProposal.Errors.Select(error => $"Error: {error.Message}")
                    .Concat(_currentProposal.Warnings.Select(warning => $"Warning: {warning.Message}")));

    private NodeBase? SelectedNode =>
        HasSingleSelection && _document.Nodes.TryGetValue(_sessionState.Selection.PrimaryNodeId!.Value, out var node)
            ? node
            : null;

    public void SetSurfaceSize(Size surfaceSize)
    {
        if (surfaceSize.Width <= 0 || surfaceSize.Height <= 0)
        {
            return;
        }

        _surfaceSize = surfaceSize;
        EnsureViewportInitialized();
        RefreshCanvas();
    }

    public Rect GetCanvasScreenRect(Size surfaceSize)
    {
        var document = _previewDocument ?? _document;
        return _hitTester.GetCanvasScreenRect(surfaceSize, document.Canvas, _sessionState.Viewport);
    }

    public void SelectTool(ToolMode toolMode)
    {
        _pendingImageAsset = toolMode == ToolMode.CreateImage ? _pendingImageAsset : null;
        SetSessionState(_sessionState with { ToolMode = toolMode });
        StatusMessage = toolMode switch
        {
            ToolMode.Select => "Select tool active.",
            ToolMode.Pan => "Pan tool active.",
            ToolMode.CreateRectangle => "Drag on the canvas to create a rectangle.",
            ToolMode.CreateCircle => "Drag on the canvas to create a circle.",
            ToolMode.CreateLine => "Drag on the canvas to create a line.",
            ToolMode.CreateText => "Click on the canvas to create a text node.",
            ToolMode.CreateImage => _pendingImageAsset is null
                ? "Choose an image file first, then click on the canvas."
                : "Click on the canvas to place the selected image.",
            _ => StatusMessage,
        };
    }

    public void PrepareImageCreation(PackagedAssetContent asset)
    {
        _pendingImageAsset = asset;
        SelectTool(ToolMode.CreateImage);
    }

    public void PrepareImageCreation(string filePath)
    {
        PrepareImageCreation(_documentService.ImportAsset(filePath));
    }

    public void CreateNewDocument(DocumentPresetDefinition preset, string? name)
    {
        _document = _documentService.CreateNewDocument(preset, name);
        _previewDocument = null;
        _assets = new Dictionary<AssetId, PackagedAssetContent>();
        _history = CommandHistoryStack.Empty;
        _documentPath = null;
        _isDirty = false;
        _pendingImageAsset = null;
        _interactionMode = WorkspaceInteractionMode.None;
        SetSessionState(WorkspaceSessionState.Default);
        SetCurrentProposal(null);
        ResetActivityEntries();
        AddActivity("Created new document.", WorkspaceActivitySeverity.Info, preset.DisplayName);
        StatusMessage = $"Started a new {preset.DisplayName.ToLowerInvariant()} document.";
        OnPropertiesChanged(nameof(DocumentTitle), nameof(IsDirty), nameof(WorkspaceContextText), nameof(WorkspaceStateText));
        EnsureViewportInitialized(force: true);
        RefreshAll();
    }

    public void OpenDocument(string path)
    {
        var snapshot = _documentService.Load(path);
        _document = snapshot.Document;
        _assets = snapshot.Assets;
        _history = CommandHistoryStack.Empty;
        _documentPath = snapshot.Path;
        _isDirty = false;
        _previewDocument = null;
        _pendingImageAsset = null;
        SetSessionState(WorkspaceSessionState.Default);
        SetCurrentProposal(null);
        ResetActivityEntries();
        StatusMessage = $"Opened '{Path.GetFileName(path)}'.";
        AddActivity("Opened document.", WorkspaceActivitySeverity.Info, snapshot.Path);

        foreach (var warning in snapshot.Warnings)
        {
            AddActivity("Package warning.", WorkspaceActivitySeverity.Warning, warning);
        }

        OnPropertiesChanged(nameof(DocumentTitle), nameof(IsDirty), nameof(WorkspaceContextText), nameof(WorkspaceStateText));
        EnsureViewportInitialized(force: true);
        RefreshAll();
    }

    public void SaveDocument(string path)
    {
        _documentService.Save(path, _document, _assets);
        _documentPath = Path.GetFullPath(path);
        _isDirty = false;
        StatusMessage = $"Saved '{Path.GetFileName(path)}'.";
        AddActivity("Saved document.", WorkspaceActivitySeverity.Info, _documentPath);
        OnPropertiesChanged(nameof(IsDirty), nameof(DocumentTitle));
        OnPropertiesChanged(nameof(WorkspaceContextText), nameof(WorkspaceStateText));
    }

    public void ExportJson(string path)
    {
        _documentService.ExportJson(path, _document);
        StatusMessage = $"Exported JSON to '{Path.GetFileName(path)}'.";
        AddActivity("Exported JSON.", WorkspaceActivitySeverity.Info, path);
    }

    public void ExportPng(string path)
    {
        var result = _documentService.ExportPng(path, _document, _assets);
        StatusMessage = $"Exported PNG to '{Path.GetFileName(path)}'.";
        AddActivity("Exported PNG.", WorkspaceActivitySeverity.Info, path);

        foreach (var warning in result.Warnings)
        {
            AddActivity("Export warning.", WorkspaceActivitySeverity.Warning, warning);
        }
    }

    public void Undo()
    {
        var dispatch = _commandDispatcher.Undo(_document, _history);
        HandleDispatch(dispatch, "Undo applied.", false);
    }

    public void Redo()
    {
        var dispatch = _commandDispatcher.Redo(_document, _history);
        HandleDispatch(dispatch, "Redo applied.", false);
    }

    public void SubmitPrompt()
    {
        var proposal = _agentService.CreateProposal(_document, _sessionState.Selection, PromptText);
        SetCurrentProposal(proposal);

        StatusMessage = proposal.CanApprove
            ? "Proposal ready for review."
            : proposal.Errors.FirstOrDefault()?.Message ?? "Proposal requires review.";

        AddActivity(
            proposal.CanApprove ? "Agent proposal ready." : "Agent proposal issue.",
            proposal.CanApprove ? WorkspaceActivitySeverity.Info : WorkspaceActivitySeverity.Warning,
            proposal.Summary,
            "Agent",
            proposal.Batch?.Provenance.Actor ?? "planner:local",
            PromptScopeText,
            proposal.ProposalId);
    }

    public void ApproveProposal()
    {
        if (_currentProposal?.Batch is null || !_currentProposal.CanApprove)
        {
            return;
        }

        var dispatch = _commandDispatcher.Commit(_document, _history, _currentProposal.Batch);
        HandleDispatch(dispatch, "Approved agent proposal.", true);
    }

    public void RejectProposal()
    {
        if (_currentProposal is null)
        {
            return;
        }

        AddActivity(
            "Rejected agent proposal.",
            WorkspaceActivitySeverity.Info,
            _currentProposal.Summary,
            "Agent",
            _currentProposal.Batch?.Provenance.Actor ?? "planner:local",
            ProposalScopeText.Replace("Affects", string.Empty, StringComparison.Ordinal).Trim(),
            _currentProposal.ProposalId);

        SetCurrentProposal(null);
        StatusMessage = "Proposal rejected.";
    }

    public void ToggleVisibility(string nodeIdText)
    {
        if (!TryParseNodeId(nodeIdText, out var nodeId) || !_document.Nodes.TryGetValue(nodeId, out var node))
        {
            return;
        }

        CommitCommands(
            $"Toggle visibility for {node.Name}",
            [new SetVisibilityCommand(nodeId, !node.IsVisible)]);
    }

    public void ToggleLock(string nodeIdText)
    {
        if (!TryParseNodeId(nodeIdText, out var nodeId) || !_document.Nodes.TryGetValue(nodeId, out var node))
        {
            return;
        }

        CommitCommands(
            $"Toggle lock for {node.Name}",
            [new SetLockStateCommand(nodeId, !node.IsLocked)]);
    }

    public void MoveLayer(string nodeIdText, int delta)
    {
        if (!TryParseNodeId(nodeIdText, out var nodeId) || !_document.Nodes.TryGetValue(nodeId, out var node))
        {
            return;
        }

        var siblingIds = GetSiblingIds(node.ParentId);
        var index = IndexOfNodeId(siblingIds, nodeId);
        if (index < 0)
        {
            return;
        }

        var newIndex = Math.Clamp(index + delta, 0, siblingIds.Count - 1);
        if (newIndex == index)
        {
            return;
        }

        CommitCommands(
            $"Reorder {node.Name}",
            [new ReorderNodeCommand(nodeId, node.ParentId, newIndex)]);
    }

    public void SelectLayerNode(string nodeIdText)
    {
        if (!TryParseNodeId(nodeIdText, out var nodeId))
        {
            return;
        }

        SelectNode(nodeId, false);
    }

    public void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var targetIds = GetTopLevelSelectedNodeIds();
        var commands = targetIds.Select(id => (DesignCommand)new DeleteNodeCommand(id)).ToArray();
        CommitCommands("Delete selection", commands);
        ClearSelection();
    }

    public void DuplicateSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var commands = new List<DesignCommand>();
        NodeId? lastDuplicateId = null;

        foreach (var nodeId in _sessionState.Selection.SelectedNodeIds)
        {
            var source = _document.Nodes[nodeId];
            if (source is GroupNode)
            {
                AddActivity("Duplicate skipped.", WorkspaceActivitySeverity.Warning, $"Grouped subtree duplication is not supported for '{source.Name}'.");
                continue;
            }

            var duplicate = _nodeFactory.CreateDuplicate(source);
            var insertIndex = GetInsertIndex(nodeId) + 1;
            commands.Add(new DuplicateNodeCommand(nodeId, duplicate, insertIndex));
            lastDuplicateId = duplicate.Id;
        }

        if (commands.Count == 0)
        {
            StatusMessage = "Nothing selected could be duplicated.";
            return;
        }

        CommitCommands("Duplicate selection", commands);
        if (lastDuplicateId is not null)
        {
            SelectNode(lastDuplicateId.Value, false);
        }
    }

    public void GroupSelection()
    {
        if (!HasMultipleSelection)
        {
            return;
        }

        var ordered = GetOrderedSelectedSiblings();
        if (ordered.Count < 2)
        {
            StatusMessage = "Grouping requires at least two sibling nodes.";
            return;
        }

        var parentId = _document.Nodes[ordered[0]].ParentId;
        var insertIndex = IndexOfNodeId(GetSiblingIds(parentId), ordered[0]);
        var group = new GroupNode(
            StableIdGenerator.CreateNodeId(),
            "Group",
            TransformValue.Identity,
            parentId,
            true,
            false,
            OpacityValue.Full,
            ordered);

        CommitCommands("Group selection", [new GroupNodesCommand(group, insertIndex)]);
        SelectNode(group.Id, false);
    }

    public void UngroupSelection()
    {
        if (SelectedNode is not GroupNode group)
        {
            return;
        }

        CommitCommands("Ungroup selection", [new UngroupNodeCommand(group.Id)]);
        SetSessionState(_sessionState with { Selection = SelectionState.Empty });
    }

    public void ApplyCanvasProperties()
    {
        if (!TryParsePositiveDouble(CanvasWidthText, out var width, "Canvas width")
            || !TryParsePositiveDouble(CanvasHeightText, out var height, "Canvas height")
            || !TryParseColor(CanvasBackgroundText, out var background, "Canvas background"))
        {
            return;
        }

        var preset = width == _document.Canvas.Width
            && height == _document.Canvas.Height
            && background.Equals(_document.Canvas.Background)
            ? _document.Canvas.Preset
            : CanvasPreset.Custom;

        var canvas = new CanvasModel(width, height, preset, background, _document.Canvas.SafeArea);
        CommitCommands("Update canvas", [new SetCanvasCommand(canvas)]);
        EnsureViewportInitialized(force: true);
    }

    public void ApplySelectionProperties()
    {
        if (SelectedNode is null)
        {
            return;
        }

        if (!TryParseDouble(XText, out var x, "X")
            || !TryParseDouble(YText, out var y, "Y")
            || !TryParseDouble(RotationText, out var rotation, "Rotation")
            || !TryParseOpacity(OpacityText, out var opacity))
        {
            return;
        }

        NodeBase updated = SelectedNode with
        {
            Name = string.IsNullOrWhiteSpace(NameText) ? SelectedNode.Name : NameText.Trim(),
            Transform = SelectedNode.Transform with
            {
                X = x,
                Y = y,
                RotationDegrees = rotation,
            },
            Opacity = opacity,
        };

        switch (updated)
        {
            case RectangleNode rectangle:
                if (!TryParsePositiveDouble(WidthText, out var rectangleWidth, "Width")
                    || !TryParsePositiveDouble(HeightText, out var rectangleHeight, "Height")
                    || !TryParseColor(FillText, out var rectangleFill, "Fill")
                    || !TryParseOptionalColor(StrokeText, out var rectangleStroke, "Stroke")
                    || !TryParseNonNegativeDouble(StrokeWidthText, out var rectangleStrokeWidth, "Stroke width")
                    || !TryParseNonNegativeDouble(CornerRadiusText, out var cornerRadius, "Corner radius"))
                {
                    return;
                }

                updated = rectangle with
                {
                    Size = new SizeValue(rectangleWidth, rectangleHeight),
                    Fill = rectangleFill,
                    Stroke = rectangleStroke is null ? null : new StrokeStyle(rectangleStroke.Value, rectangleStrokeWidth),
                    CornerRadius = cornerRadius,
                };
                break;

            case CircleNode circle:
                if (!TryParsePositiveDouble(WidthText, out var circleWidth, "Width")
                    || !TryParsePositiveDouble(HeightText, out var circleHeight, "Height")
                    || !TryParseColor(FillText, out var circleFill, "Fill")
                    || !TryParseOptionalColor(StrokeText, out var circleStroke, "Stroke")
                    || !TryParseNonNegativeDouble(StrokeWidthText, out var circleStrokeWidth, "Stroke width"))
                {
                    return;
                }

                updated = circle with
                {
                    Size = new SizeValue(circleWidth, circleHeight),
                    Fill = circleFill,
                    Stroke = circleStroke is null ? null : new StrokeStyle(circleStroke.Value, circleStrokeWidth),
                };
                break;

            case TextNode text:
                if (!TryParsePositiveDouble(WidthText, out var textWidth, "Width")
                    || !TryParsePositiveDouble(HeightText, out var textHeight, "Height")
                    || !TryParseColor(FillText, out var textFill, "Fill")
                    || !TryParsePositiveDouble(FontSizeText, out var fontSize, "Font size")
                    || !TryParseInt(FontWeightText, out var fontWeight, "Font weight"))
                {
                    return;
                }

                updated = text with
                {
                    Content = string.IsNullOrWhiteSpace(TextContent) ? text.Content : TextContent,
                    Bounds = new RectValue(0, 0, textWidth, textHeight),
                    Fill = textFill,
                    Typography = new TypographyStyle(
                        string.IsNullOrWhiteSpace(FontFamilyText) ? text.Typography.FontFamily : FontFamilyText.Trim(),
                        fontSize,
                        fontWeight,
                        string.IsNullOrWhiteSpace(AlignmentText) ? text.Typography.Alignment : AlignmentText.Trim().ToLowerInvariant(),
                        text.Typography.LineHeight,
                        text.Typography.LetterSpacing),
                };
                break;

            case ImageNode image:
                if (!TryParsePositiveDouble(WidthText, out var imageWidth, "Width")
                    || !TryParsePositiveDouble(HeightText, out var imageHeight, "Height"))
                {
                    return;
                }

                updated = image with
                {
                    Bounds = new RectValue(0, 0, imageWidth, imageHeight),
                    FitMode = string.IsNullOrWhiteSpace(FitModeText) ? image.FitMode : FitModeText.Trim().ToLowerInvariant(),
                };
                break;

            case LineNode line:
                if (!TryParseDouble(StartXText, out var startX, "Start X")
                    || !TryParseDouble(StartYText, out var startY, "Start Y")
                    || !TryParseDouble(EndXText, out var endX, "End X")
                    || !TryParseDouble(EndYText, out var endY, "End Y")
                    || !TryParseColor(StrokeText, out var lineStroke, "Stroke")
                    || !TryParseNonNegativeDouble(StrokeWidthText, out var lineStrokeWidth, "Stroke width"))
                {
                    return;
                }

                updated = line with
                {
                    Start = new PointValue(startX, startY),
                    End = new PointValue(endX, endY),
                    Stroke = new StrokeStyle(lineStroke, lineStrokeWidth),
                };
                break;
        }

        CommitCommands($"Update {updated.Kind}", [new UpdateNodeCommand(updated)]);
    }

    public void OnCanvasPointerPressed(Point point, KeyModifiers modifiers, bool isMiddleButton)
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            return;
        }

        if (isMiddleButton || _sessionState.ToolMode == ToolMode.Pan)
        {
            BeginPan(point);
            return;
        }

        if (_currentRenderPlan is null)
        {
            RefreshCanvas();
        }

        var hit = _currentRenderPlan is null
            ? new CanvasHitResult(point, null, CanvasHandleKind.None, false)
            : _hitTester.HitTest(_currentRenderPlan, _sessionState, _surfaceSize, point);

        _interactionStartScreenPoint = point;
        _interactionStartCanvasPoint = ClampToCanvas(hit.CanvasPoint);

        switch (_sessionState.ToolMode)
        {
            case ToolMode.CreateRectangle:
            case ToolMode.CreateCircle:
            case ToolMode.CreateLine:
            case ToolMode.CreateText:
            case ToolMode.CreateImage:
                _interactionMode = WorkspaceInteractionMode.Create;
                break;

            case ToolMode.Select:
            default:
                if (hit.HandleKind is CanvasHandleKind.TopLeft or CanvasHandleKind.TopRight or CanvasHandleKind.BottomLeft or CanvasHandleKind.BottomRight)
                {
                    BeginResize(hit.HandleKind);
                    return;
                }

                if (hit.HandleKind == CanvasHandleKind.Rotate)
                {
                    BeginRotate();
                    return;
                }

                if (hit.NodeId is { } nodeId)
                {
                    SelectNode(nodeId, modifiers.HasFlag(KeyModifiers.Shift));
                    BeginMoveSelection();
                }
                else
                {
                    ClearSelection();
                }

                break;
        }
    }

    public void OnCanvasPointerMoved(Point point)
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            return;
        }

        if (_interactionMode == WorkspaceInteractionMode.None)
        {
            UpdateHover(point);
            return;
        }

        var canvasPoint = ClampToCanvas(_hitTester.ScreenToCanvas(_surfaceSize, (_previewDocument ?? _document).Canvas, _sessionState.Viewport, point));

        switch (_interactionMode)
        {
            case WorkspaceInteractionMode.Pan:
                var dx = point.X - _interactionStartScreenPoint.X;
                var dy = point.Y - _interactionStartScreenPoint.Y;
                SetSessionState(_sessionState with
                {
                    Viewport = _interactionStartViewport with
                    {
                        PanX = _interactionStartViewport.PanX + dx,
                        PanY = _interactionStartViewport.PanY + dy,
                    },
                });
                RefreshCanvas();
                break;

            case WorkspaceInteractionMode.Move:
                ApplyMovePreview(canvasPoint);
                break;

            case WorkspaceInteractionMode.Resize:
                ApplyResizePreview(canvasPoint);
                break;

            case WorkspaceInteractionMode.Rotate:
                ApplyRotatePreview(canvasPoint);
                break;
        }
    }

    public void OnCanvasPointerReleased(Point point)
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            _interactionMode = WorkspaceInteractionMode.None;
            return;
        }

        var canvasPoint = ClampToCanvas(_hitTester.ScreenToCanvas(_surfaceSize, (_previewDocument ?? _document).Canvas, _sessionState.Viewport, point));

        switch (_interactionMode)
        {
            case WorkspaceInteractionMode.Create:
                CommitCreate(canvasPoint);
                break;

            case WorkspaceInteractionMode.Move:
            case WorkspaceInteractionMode.Resize:
            case WorkspaceInteractionMode.Rotate:
                CommitPreviewUpdate();
                break;
        }

        _interactionMode = WorkspaceInteractionMode.None;
        _activeHandle = CanvasHandleKind.None;
        _previewDocument = null;
        RefreshCanvas();
    }

    public void OnCanvasPointerWheel(Point point, Vector delta)
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            return;
        }

        var factor = delta.Y > 0 ? 1.1d : 0.9d;
        var document = _previewDocument ?? _document;
        var beforeCanvasPoint = _hitTester.ScreenToCanvas(_surfaceSize, document.Canvas, _sessionState.Viewport, point);
        var zoom = Math.Clamp(_sessionState.Viewport.Zoom * factor, MinimumZoom, MaximumZoom);
        var nextViewport = _sessionState.Viewport with { Zoom = zoom, IsInitialized = true };
        var nextScreenPoint = _hitTester.CanvasToScreen(_surfaceSize, document.Canvas, nextViewport, beforeCanvasPoint);
        nextViewport = nextViewport with
        {
            PanX = nextViewport.PanX + (point.X - nextScreenPoint.X),
            PanY = nextViewport.PanY + (point.Y - nextScreenPoint.Y),
        };

        SetSessionState(_sessionState with { Viewport = nextViewport });
        RefreshCanvas();
    }

    public void OnCanvasPointerExited()
    {
        SetSessionState(_sessionState with { HoverNodeId = null });
    }

    public void Dispose()
    {
        _canvasBitmap?.Dispose();
    }

    private void BeginPan(Point point)
    {
        _interactionMode = WorkspaceInteractionMode.Pan;
        _interactionStartScreenPoint = point;
        _interactionStartViewport = _sessionState.Viewport;
    }

    private void BeginMoveSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var selectedNodes = _sessionState.Selection.SelectedNodeIds
            .Select(id => _document.Nodes[id])
            .Where(node => !node.IsLocked)
            .ToDictionary(node => node.Id, node => node);

        if (selectedNodes.Count == 0)
        {
            StatusMessage = "Locked nodes cannot be moved.";
            return;
        }

        _interactionMode = WorkspaceInteractionMode.Move;
        _interactionStartNodes = selectedNodes;
    }

    private void BeginResize(CanvasHandleKind handleKind)
    {
        if (SelectedNode is null || _currentRenderPlan is null || SelectedNode.IsLocked)
        {
            return;
        }

        var bounds = _currentRenderPlan.Nodes.Last(node => node.NodeId == SelectedNode.Id).Bounds;
        _interactionMode = WorkspaceInteractionMode.Resize;
        _activeHandle = handleKind;
        _interactionStartNodes = new Dictionary<NodeId, NodeBase> { [SelectedNode.Id] = SelectedNode };
        _interactionStartBounds = new Rect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    private void BeginRotate()
    {
        if (SelectedNode is null || _currentRenderPlan is null || SelectedNode.IsLocked)
        {
            return;
        }

        var bounds = _currentRenderPlan.Nodes.Last(node => node.NodeId == SelectedNode.Id).Bounds;
        _interactionMode = WorkspaceInteractionMode.Rotate;
        _interactionStartNodes = new Dictionary<NodeId, NodeBase> { [SelectedNode.Id] = SelectedNode };
        _rotationAnchor = new Point((bounds.Left + bounds.Right) / 2d, (bounds.Top + bounds.Bottom) / 2d);
    }

    private void ApplyMovePreview(Point canvasPoint)
    {
        var deltaX = canvasPoint.X - _interactionStartCanvasPoint.X;
        var deltaY = canvasPoint.Y - _interactionStartCanvasPoint.Y;
        var nodes = _document.Nodes.ToDictionary(entry => entry.Key, entry => entry.Value);

        foreach (var entry in _interactionStartNodes)
        {
            nodes[entry.Key] = _nodeFactory.MoveNode(entry.Value, deltaX, deltaY);
        }

        _previewDocument = _document with { Nodes = nodes };
        RefreshCanvas();
    }

    private void ApplyResizePreview(Point canvasPoint)
    {
        if (SelectedNode is null)
        {
            return;
        }

        var start = _interactionStartBounds;
        var left = start.Left;
        var top = start.Top;
        var right = start.Right;
        var bottom = start.Bottom;

        switch (_activeHandle)
        {
            case CanvasHandleKind.TopLeft:
                left = canvasPoint.X;
                top = canvasPoint.Y;
                break;
            case CanvasHandleKind.TopRight:
                right = canvasPoint.X;
                top = canvasPoint.Y;
                break;
            case CanvasHandleKind.BottomLeft:
                left = canvasPoint.X;
                bottom = canvasPoint.Y;
                break;
            case CanvasHandleKind.BottomRight:
                right = canvasPoint.X;
                bottom = canvasPoint.Y;
                break;
            default:
                return;
        }

        var normalized = NormalizeRect(new Rect(left, top, right - left, bottom - top));
        var resized = _nodeFactory.ResizeNodeToBounds(SelectedNode, normalized);
        var nodes = _document.Nodes.ToDictionary(entry => entry.Key, entry => entry.Value);
        nodes[SelectedNode.Id] = resized;
        _previewDocument = _document with { Nodes = nodes };
        RefreshCanvas();
    }

    private void ApplyRotatePreview(Point canvasPoint)
    {
        if (SelectedNode is null || !_interactionStartNodes.TryGetValue(SelectedNode.Id, out var originalNode))
        {
            return;
        }

        var startAngle = Math.Atan2(_interactionStartCanvasPoint.Y - _rotationAnchor.Y, _interactionStartCanvasPoint.X - _rotationAnchor.X);
        var currentAngle = Math.Atan2(canvasPoint.Y - _rotationAnchor.Y, canvasPoint.X - _rotationAnchor.X);
        var deltaDegrees = (currentAngle - startAngle) * (180d / Math.PI);
        var rotated = _nodeFactory.RotateNode(originalNode, originalNode.Transform.RotationDegrees + deltaDegrees);
        var nodes = _document.Nodes.ToDictionary(entry => entry.Key, entry => entry.Value);
        nodes[SelectedNode.Id] = rotated;
        _previewDocument = _document with { Nodes = nodes };
        RefreshCanvas();
    }

    private void CommitPreviewUpdate()
    {
        if (_previewDocument is null)
        {
            return;
        }

        var commands = new List<DesignCommand>();
        foreach (var entry in _interactionStartNodes)
        {
            var updated = _previewDocument.Nodes[entry.Key];
            if (!Equals(updated, entry.Value))
            {
                commands.Add(new UpdateNodeCommand(updated));
            }
        }

        if (commands.Count > 0)
        {
            CommitCommands("Direct edit", commands);
        }
    }

    private void CommitCreate(Point canvasPoint)
    {
        var start = _interactionStartCanvasPoint;
        var defaultRect = new Rect(canvasPoint.X, canvasPoint.Y, 240, 160);
        DesignCommand[] commands;
        NodeId createdNodeId;

        switch (_sessionState.ToolMode)
        {
            case ToolMode.CreateRectangle:
                var rectangle = _nodeFactory.CreateRectangle(NormalizeRect(new Rect(start, canvasPoint)));
                commands = [new CreateNodeCommand(rectangle, _document.RootNodeIds.Count)];
                createdNodeId = rectangle.Id;
                break;

            case ToolMode.CreateCircle:
                var circle = _nodeFactory.CreateCircle(NormalizeRect(new Rect(start, canvasPoint)));
                commands = [new CreateNodeCommand(circle, _document.RootNodeIds.Count)];
                createdNodeId = circle.Id;
                break;

            case ToolMode.CreateLine:
                var line = _nodeFactory.CreateLine(start, canvasPoint);
                commands = [new CreateNodeCommand(line, _document.RootNodeIds.Count)];
                createdNodeId = line.Id;
                break;

            case ToolMode.CreateText:
                var text = _nodeFactory.CreateText(start);
                commands = [new CreateNodeCommand(text, _document.RootNodeIds.Count)];
                createdNodeId = text.Id;
                break;

            case ToolMode.CreateImage:
                if (_pendingImageAsset is null)
                {
                    StatusMessage = "Choose an image file before placing it.";
                    return;
                }

                var image = _nodeFactory.CreateImage(defaultRect, _pendingImageAsset.Manifest.Id);
                var imageCommands = new List<DesignCommand>();
                if (!_document.Assets.ContainsKey(_pendingImageAsset.Manifest.Id))
                {
                    imageCommands.Add(new ImportAssetCommand(_pendingImageAsset.Manifest));
                }

                imageCommands.Add(new CreateNodeCommand(image, _document.RootNodeIds.Count));
                commands = imageCommands.ToArray();
                createdNodeId = image.Id;
                var updatedAssets = _assets.ToDictionary(entry => entry.Key, entry => entry.Value);
                updatedAssets.TryAdd(_pendingImageAsset.Manifest.Id, _pendingImageAsset);
                _assets = updatedAssets;
                _pendingImageAsset = null;
                break;

            default:
                return;
        }

        CommitCommands("Create node", commands);
        SelectTool(ToolMode.Select);
        SelectNode(createdNodeId, false);
    }

    private void UpdateHover(Point point)
    {
        if (_currentRenderPlan is null)
        {
            return;
        }

        var hit = _hitTester.HitTest(_currentRenderPlan, _sessionState, _surfaceSize, point);
        if (_sessionState.HoverNodeId != hit.NodeId)
        {
            SetSessionState(_sessionState with { HoverNodeId = hit.NodeId });
        }
    }

    private void SelectNode(NodeId nodeId, bool toggle)
    {
        IReadOnlyList<NodeId> selected;
        if (toggle)
        {
            selected = _sessionState.Selection.SelectedNodeIds.Contains(nodeId)
                ? _sessionState.Selection.SelectedNodeIds.Where(id => id != nodeId).ToArray()
                : _sessionState.Selection.SelectedNodeIds.Concat([nodeId]).ToArray();
        }
        else
        {
            selected = [nodeId];
        }

        SetSessionState(_sessionState with { Selection = new SelectionState(selected) });
        RefreshEditorState();
        RefreshLayers();
        RefreshCanvas();
    }

    private void ClearSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        SetSessionState(_sessionState with { Selection = SelectionState.Empty });
        RefreshEditorState();
        RefreshLayers();
        RefreshCanvas();
    }

    private void CommitCommands(string summary, IReadOnlyList<DesignCommand> commands)
    {
        if (commands.Count == 0)
        {
            return;
        }

        var dispatch = _commandDispatcher.Commit(_document, _history, summary, commands, "workspace:ui");
        HandleDispatch(dispatch, $"{summary} applied.", true);
    }

    private void HandleDispatch(WorkspaceCommandDispatchResult dispatch, string successMessage, bool markDirty)
    {
        if (!dispatch.Result.IsSuccess)
        {
            var error = dispatch.Result.Errors.FirstOrDefault();
            StatusMessage = error?.Message ?? "The command could not be applied.";
            AddActivity("Command failed.", WorkspaceActivitySeverity.Error, StatusMessage);
            _previewDocument = null;
            RefreshCanvas();
            return;
        }

        _document = dispatch.Document;
        _history = dispatch.History;
        _previewDocument = null;
        SetCurrentProposal(null);
        if (markDirty)
        {
            _isDirty = true;
        }

        StatusMessage = successMessage;
        var provenance = dispatch.HistoryEntry?.Batch.Provenance;
        AddActivity(
            provenance?.Source == CommandSource.Agent ? "Applied agent proposal." : "Applied command batch.",
            WorkspaceActivitySeverity.Info,
            dispatch.HistoryEntry?.Batch.Summary ?? successMessage,
            GetSourceLabel(provenance?.Source ?? CommandSource.System),
            provenance?.Actor,
            dispatch.HistoryEntry?.Batch is null
                ? null
                : DescribeBatchScope(dispatch.HistoryEntry.Batch, leadingVerb: null),
            provenance?.CorrelationId);

        foreach (var warning in dispatch.Result.Warnings)
        {
            AddActivity(
                "Command warning.",
                WorkspaceActivitySeverity.Warning,
                warning.Message,
                GetSourceLabel(provenance?.Source ?? CommandSource.System),
                provenance?.Actor,
                dispatch.HistoryEntry?.Batch is null
                    ? null
                    : DescribeBatchScope(dispatch.HistoryEntry.Batch, leadingVerb: null),
                provenance?.CorrelationId);
        }

        OnPropertiesChanged(nameof(IsDirty), nameof(DocumentTitle), nameof(CanUndo), nameof(CanRedo));
        OnPropertiesChanged(nameof(WorkspaceStateText));
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshEditorState();
        RefreshLayers();
        RefreshCanvas();
    }

    private void RefreshLayers()
    {
        _layerItems.Clear();
        AppendLayerItems(_document.RootNodeIds, 0);
    }

    private void AppendLayerItems(IReadOnlyList<NodeId> nodeIds, int depth)
    {
        for (var index = 0; index < nodeIds.Count; index++)
        {
            var nodeId = nodeIds[index];
            var node = _document.Nodes[nodeId];
            _layerItems.Add(
                new LayerItemViewModel(
                    nodeId,
                    node.Name,
                    node.Kind,
                    depth,
                    _sessionState.Selection.SelectedNodeIds.Contains(nodeId),
                    node.IsVisible,
                    node.IsLocked,
                    index > 0,
                    index < nodeIds.Count - 1));

            if (node is GroupNode group)
            {
                AppendLayerItems(group.ChildNodeIds, depth + 1);
            }
        }
    }

    private void RefreshEditorState()
    {
        CanvasWidthText = Format(_document.Canvas.Width);
        CanvasHeightText = Format(_document.Canvas.Height);
        CanvasBackgroundText = FormatColor(_document.Canvas.Background);

        var node = SelectedNode;
        if (node is null)
        {
            NameText = _document.Name;
            XText = "0";
            YText = "0";
            RotationText = "0";
            OpacityText = "1";
            WidthText = string.Empty;
            HeightText = string.Empty;
            FillText = string.Empty;
            StrokeText = string.Empty;
            StrokeWidthText = string.Empty;
            CornerRadiusText = string.Empty;
            TextContent = string.Empty;
            FontFamilyText = string.Empty;
            FontSizeText = string.Empty;
            FontWeightText = string.Empty;
            AlignmentText = string.Empty;
            FitModeText = string.Empty;
            StartXText = string.Empty;
            StartYText = string.Empty;
            EndXText = string.Empty;
            EndYText = string.Empty;
        }
        else
        {
            NameText = node.Name;
            XText = Format(node.Transform.X);
            YText = Format(node.Transform.Y);
            RotationText = Format(node.Transform.RotationDegrees);
            OpacityText = Format(node.Opacity.Value);
            WidthText = string.Empty;
            HeightText = string.Empty;
            FillText = string.Empty;
            StrokeText = string.Empty;
            StrokeWidthText = string.Empty;
            CornerRadiusText = string.Empty;
            TextContent = string.Empty;
            FontFamilyText = string.Empty;
            FontSizeText = string.Empty;
            FontWeightText = string.Empty;
            AlignmentText = string.Empty;
            FitModeText = string.Empty;
            StartXText = string.Empty;
            StartYText = string.Empty;
            EndXText = string.Empty;
            EndYText = string.Empty;

            switch (node)
            {
                case RectangleNode rectangle:
                    WidthText = Format(rectangle.Size.Width);
                    HeightText = Format(rectangle.Size.Height);
                    FillText = FormatColor(rectangle.Fill);
                    StrokeText = rectangle.Stroke is null ? string.Empty : FormatColor(rectangle.Stroke.Value.Color);
                    StrokeWidthText = rectangle.Stroke is null ? "0" : Format(rectangle.Stroke.Value.Width);
                    CornerRadiusText = Format(rectangle.CornerRadius);
                    break;

                case CircleNode circle:
                    WidthText = Format(circle.Size.Width);
                    HeightText = Format(circle.Size.Height);
                    FillText = FormatColor(circle.Fill);
                    StrokeText = circle.Stroke is null ? string.Empty : FormatColor(circle.Stroke.Value.Color);
                    StrokeWidthText = circle.Stroke is null ? "0" : Format(circle.Stroke.Value.Width);
                    break;

                case TextNode text:
                    WidthText = Format(text.Bounds.Width);
                    HeightText = Format(text.Bounds.Height);
                    FillText = FormatColor(text.Fill);
                    TextContent = text.Content;
                    FontFamilyText = text.Typography.FontFamily;
                    FontSizeText = Format(text.Typography.FontSize);
                    FontWeightText = text.Typography.Weight.ToString();
                    AlignmentText = text.Typography.Alignment;
                    break;

                case ImageNode image:
                    WidthText = Format(image.Bounds.Width);
                    HeightText = Format(image.Bounds.Height);
                    FitModeText = image.FitMode;
                    break;

                case LineNode line:
                    StartXText = Format(line.Start.X);
                    StartYText = Format(line.Start.Y);
                    EndXText = Format(line.End.X);
                    EndYText = Format(line.End.Y);
                    StrokeText = FormatColor(line.Stroke.Color);
                    StrokeWidthText = Format(line.Stroke.Width);
                    break;
            }
        }

        OnPropertiesChanged(
            nameof(HasSelection),
            nameof(HasSingleSelection),
            nameof(HasMultipleSelection),
            nameof(ShowCanvasEditor),
            nameof(ShowSingleNodeEditor),
            nameof(ShowMultiSelectionEditor),
            nameof(ShowShapeEditor),
            nameof(ShowTextEditor),
            nameof(ShowImageEditor),
            nameof(ShowLineEditor),
            nameof(ShowGroupEditor),
            nameof(SelectionSummaryText),
            nameof(ActiveToolText),
            nameof(PromptScopeText),
            nameof(PromptScopeDetailText),
            nameof(AgentContextSummaryText),
            nameof(AgentContextSelectionText),
            nameof(AgentContextSelectionDetailText),
            nameof(AgentContextProposalImpactText));
    }

    private void RefreshCanvas()
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            return;
        }

        var document = _previewDocument ?? _document;
        var snapshot = _renderer.Render(
            document,
            _assets,
            _sessionState.Selection.SelectedNodeIds,
            showSafeAreaGuides: true);

        var previousBitmap = _canvasBitmap;
        _canvasBitmap = snapshot.Bitmap;
        previousBitmap?.Dispose();

        _currentRenderPlan = snapshot.Plan;
        RenderWarningsText = snapshot.Warnings.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, snapshot.Warnings);

        OnPropertyChanged(nameof(CanvasBitmap));
        OnPropertyChanged(nameof(HasRenderWarnings));
    }

    private void EnsureViewportInitialized(bool force = false)
    {
        if (_surfaceSize.Width <= 0 || _surfaceSize.Height <= 0)
        {
            return;
        }

        if (_sessionState.Viewport.IsInitialized && !force)
        {
            return;
        }

        var widthScale = Math.Max((_surfaceSize.Width - FitPadding) / _document.Canvas.Width, MinimumZoom);
        var heightScale = Math.Max((_surfaceSize.Height - FitPadding) / _document.Canvas.Height, MinimumZoom);
        var zoom = Math.Clamp(Math.Min(widthScale, heightScale), MinimumZoom, MaximumZoom);

        SetSessionState(_sessionState with
        {
            Viewport = new ViewportState(zoom, 0, 0, true),
        });
    }

    private void SetSessionState(WorkspaceSessionState sessionState)
    {
        _sessionState = sessionState;
        OnPropertyChanged(nameof(SessionState));
        OnPropertiesChanged(
            nameof(HasSelection),
            nameof(HasSingleSelection),
            nameof(HasMultipleSelection),
            nameof(ActiveToolText),
            nameof(PromptScopeText),
            nameof(PromptScopeDetailText),
            nameof(AgentContextSummaryText),
            nameof(AgentContextSelectionText),
            nameof(AgentContextSelectionDetailText),
            nameof(AgentContextProposalImpactText));
    }

    private IReadOnlyList<NodeId> GetSiblingIds(NodeId? parentId) =>
        parentId is null
            ? _document.RootNodeIds
            : ((GroupNode)_document.Nodes[parentId.Value]).ChildNodeIds;

    private int GetInsertIndex(NodeId nodeId)
    {
        var node = _document.Nodes[nodeId];
        return IndexOfNodeId(GetSiblingIds(node.ParentId), nodeId);
    }

    private IReadOnlyList<NodeId> GetTopLevelSelectedNodeIds()
    {
        var selected = _sessionState.Selection.SelectedNodeIds.ToHashSet();
        return _sessionState.Selection.SelectedNodeIds
            .Where(id => !_document.Nodes[id].ParentId.HasValue || !selected.Contains(_document.Nodes[id].ParentId!.Value))
            .ToArray();
    }

    private IReadOnlyList<NodeId> GetOrderedSelectedSiblings()
    {
        var selected = _sessionState.Selection.SelectedNodeIds.ToHashSet();
        var firstNode = _document.Nodes[_sessionState.Selection.SelectedNodeIds[0]];
        var siblingIds = GetSiblingIds(firstNode.ParentId);
        return siblingIds.Where(selected.Contains).ToArray();
    }

    private void AddActivity(string summary, WorkspaceActivitySeverity severity, string? detail = null) =>
        AddActivity(summary, severity, detail, "System");

    private void AddActivity(
        string summary,
        WorkspaceActivitySeverity severity,
        string? detail,
        string sourceLabel,
        string? actor = null,
        string? scopeLabel = null,
        string? correlationId = null)
    {
        _activityEntries.Insert(
            0,
            new WorkspaceActivityEntry(DateTimeOffset.UtcNow, summary, severity, detail, sourceLabel, actor, scopeLabel, correlationId));
        OnPropertiesChanged(nameof(AgentContextRecentActivityText));
    }

    private void ResetActivityEntries()
    {
        _activityEntries.Clear();
        OnPropertiesChanged(nameof(AgentContextRecentActivityText));
    }

    private void SetCurrentProposal(PlannerOutput? proposal)
    {
        _currentProposal = proposal;
        OnPropertiesChanged(
            nameof(CurrentProposal),
            nameof(HasProposal),
            nameof(CanApproveProposal),
            nameof(CanRejectProposal),
            nameof(HasProposalRationale),
            nameof(HasProposalIssues),
            nameof(ProposalStatusText),
            nameof(ProposalSummaryText),
            nameof(ProposalRationaleText),
            nameof(ProposalScopeText),
            nameof(ProposalChangeCountText),
            nameof(ProposalChangeSummaryText),
            nameof(ProposalReviewHintText),
            nameof(PendingProposalBadgeText),
            nameof(ProposalCommandPreviewText),
            nameof(ProposalIssueText),
            nameof(WorkspaceStateText),
            nameof(AgentContextSummaryText),
            nameof(AgentContextProposalImpactText),
            nameof(InspectorIntroText));
    }

    private bool ProposalTargetsCurrentSelection()
    {
        if (!HasSelection || _currentProposal?.Batch is null)
        {
            return false;
        }

        var selected = _sessionState.Selection.SelectedNodeIds.ToHashSet();
        return _currentProposal.Batch.Commands
            .SelectMany(GetCommandTargetNodeIds)
            .Any(selected.Contains);
    }

    private string DescribeCurrentPromptScope() =>
        HasMultipleSelection
            ? $"{_sessionState.Selection.SelectedNodeIds.Count} selected layers"
            : HasSingleSelection
                ? "1 selected layer"
                : "Document or canvas";

    private string DescribeCurrentPromptScopeDetail()
    {
        if (HasMultipleSelection)
        {
            return $"Targeting {_sessionState.Selection.SelectedNodeIds.Count} selected layers. The local planner can review hide, show, lock, and unlock requests for this selection.";
        }

        if (SelectedNode is not null)
        {
            return $"Targeting {SelectedNode.Kind} '{SelectedNode.Name}'. The local planner can review hide, show, lock, and unlock requests for this selection.";
        }

        return "No layer selected. The local planner can review whole-document prompts and canvas background updates.";
    }

    private string DescribeProposalChange(DesignCommand command) =>
        command switch
        {
            SetVisibilityCommand setVisibility => $"{(setVisibility.IsVisible ? "Show" : "Hide")} {DescribeNodeReference(setVisibility.NodeId)}.",
            SetLockStateCommand setLockState => $"{(setLockState.IsLocked ? "Lock" : "Unlock")} {DescribeNodeReference(setLockState.NodeId)}.",
            SetCanvasCommand setCanvas => $"Update canvas background to {FormatColor(setCanvas.Canvas.Background)}.",
            CreateNodeCommand createNode => $"Create {createNode.Node.Kind} '{createNode.Node.Name}'.",
            UpdateNodeCommand updateNode => $"Update {updateNode.Node.Kind} '{updateNode.Node.Name}'.",
            DeleteNodeCommand deleteNode => $"Delete {DescribeNodeReference(deleteNode.NodeId)}.",
            ReorderNodeCommand reorderNode => $"Reorder {DescribeNodeReference(reorderNode.NodeId)}.",
            GroupNodesCommand groupNodes => $"Group nodes into '{groupNodes.Group.Name}'.",
            UngroupNodeCommand ungroupNode => $"Ungroup {DescribeNodeReference(ungroupNode.GroupId)}.",
            ImportAssetCommand importAsset => $"Import asset '{importAsset.Asset.FileName}'.",
            RemoveAssetCommand removeAsset => $"Remove asset '{removeAsset.AssetId.Value}'.",
            DuplicateNodeCommand duplicateNode => $"Duplicate {DescribeNodeReference(duplicateNode.SourceNodeId)}.",
            _ => DescribeCommand(command),
        };

    private string DescribeNodeReference(NodeId nodeId) =>
        _document.Nodes.TryGetValue(nodeId, out var node)
            ? $"{node.Kind} '{node.Name}'"
            : $"layer '{nodeId.Value}'";

    private string DescribeBatchScope(CommandBatch batch, string? leadingVerb)
    {
        var targetNodeIds = batch.Commands
            .SelectMany(GetCommandTargetNodeIds)
            .Distinct()
            .ToArray();

        string scope = targetNodeIds.Length switch
        {
            0 when batch.Commands.Any(command => command is SetCanvasCommand) => "canvas",
            1 => "1 selected layer",
            > 1 => $"{targetNodeIds.Length} selected layers",
            _ => "document",
        };

        if (string.IsNullOrWhiteSpace(leadingVerb))
        {
            return char.ToUpperInvariant(scope[0]) + scope[1..];
        }

        return $"{leadingVerb} {scope}";
    }

    private static string FormatChangeCount(int changeCount) =>
        changeCount == 1 ? "1 proposed change" : $"{changeCount} proposed changes";

    private static IEnumerable<NodeId> GetCommandTargetNodeIds(DesignCommand command) =>
        command switch
        {
            SetVisibilityCommand setVisibility => [setVisibility.NodeId],
            SetLockStateCommand setLockState => [setLockState.NodeId],
            UpdateNodeCommand updateNode => [updateNode.Node.Id],
            CreateNodeCommand createNode => [createNode.Node.Id],
            DeleteNodeCommand deleteNode => [deleteNode.NodeId],
            ReorderNodeCommand reorderNode => [reorderNode.NodeId],
            GroupNodesCommand groupNodes => groupNodes.Group.ChildNodeIds.Concat([groupNodes.Group.Id]),
            UngroupNodeCommand ungroupNode => [ungroupNode.GroupId],
            DuplicateNodeCommand duplicateNode => [duplicateNode.SourceNodeId],
            _ => Array.Empty<NodeId>(),
        };

    private static string GetSourceLabel(CommandSource source) =>
        source switch
        {
            CommandSource.Human => "Human",
            CommandSource.Agent => "Agent",
            _ => "System",
        };

    private static string DescribeCommand(DesignCommand command) =>
        command switch
        {
            CreateNodeCommand createNode => $"Create {createNode.Node.Kind} '{createNode.Node.Name}'.",
            UpdateNodeCommand updateNode => $"Update {updateNode.Node.Kind} '{updateNode.Node.Name}'.",
            DeleteNodeCommand deleteNode => $"Delete node '{deleteNode.NodeId.Value}'.",
            ReorderNodeCommand reorderNode => $"Reorder node '{reorderNode.NodeId.Value}'.",
            GroupNodesCommand groupNodes => $"Group nodes into '{groupNodes.Group.Name}'.",
            UngroupNodeCommand ungroupNode => $"Ungroup '{ungroupNode.GroupId.Value}'.",
            SetCanvasCommand => "Update canvas settings.",
            ImportAssetCommand importAsset => $"Import asset '{importAsset.Asset.FileName}'.",
            RemoveAssetCommand removeAsset => $"Remove asset '{removeAsset.AssetId.Value}'.",
            DuplicateNodeCommand duplicateNode => $"Duplicate node '{duplicateNode.SourceNodeId.Value}'.",
            SetVisibilityCommand setVisibility => $"Set visibility for '{setVisibility.NodeId.Value}' to {(setVisibility.IsVisible ? "visible" : "hidden")}.",
            SetLockStateCommand setLockState => $"Set lock state for '{setLockState.NodeId.Value}' to {(setLockState.IsLocked ? "locked" : "unlocked")}.",
            _ => command.Kind.ToString(),
        };

    private static int IndexOfNodeId(IReadOnlyList<NodeId> nodeIds, NodeId nodeId)
    {
        for (var index = 0; index < nodeIds.Count; index++)
        {
            if (nodeIds[index] == nodeId)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryParseNodeId(string nodeIdText, out NodeId nodeId)
    {
        nodeId = default;
        if (string.IsNullOrWhiteSpace(nodeIdText))
        {
            return false;
        }

        nodeId = NodeId.From(nodeIdText);
        return true;
    }

    private bool TryParseDouble(string value, out double result, string fieldName)
    {
        if (double.TryParse(value, out result))
        {
            return true;
        }

        StatusMessage = $"{fieldName} must be a number.";
        return false;
    }

    private bool TryParsePositiveDouble(string value, out double result, string fieldName)
    {
        if (double.TryParse(value, out result) && result > 0)
        {
            return true;
        }

        StatusMessage = $"{fieldName} must be greater than zero.";
        return false;
    }

    private bool TryParseNonNegativeDouble(string value, out double result, string fieldName)
    {
        if (double.TryParse(value, out result) && result >= 0)
        {
            return true;
        }

        StatusMessage = $"{fieldName} must be zero or greater.";
        return false;
    }

    private bool TryParseInt(string value, out int result, string fieldName)
    {
        if (int.TryParse(value, out result))
        {
            return true;
        }

        StatusMessage = $"{fieldName} must be an integer.";
        return false;
    }

    private bool TryParseOpacity(string value, out OpacityValue opacity)
    {
        opacity = OpacityValue.Full;
        if (double.TryParse(value, out var raw) && raw >= 0 && raw <= 1)
        {
            opacity = new OpacityValue(raw);
            return true;
        }

        StatusMessage = "Opacity must be between 0 and 1.";
        return false;
    }

    private bool TryParseColor(string value, out ColorValue color, string fieldName)
    {
        if (TryParseColorCore(value, out color))
        {
            return true;
        }

        StatusMessage = $"{fieldName} must use #RRGGBB or #AARRGGBB.";
        return false;
    }

    private bool TryParseOptionalColor(string value, out ColorValue? color, string fieldName)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (TryParseColorCore(value, out var parsed))
        {
            color = parsed;
            return true;
        }

        StatusMessage = $"{fieldName} must use #RRGGBB or #AARRGGBB.";
        return false;
    }

    private static bool TryParseColorCore(string value, out ColorValue color)
    {
        color = default;
        var trimmed = value.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length == 6
            && byte.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var red)
            && byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var green)
            && byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var blue))
        {
            color = new ColorValue(red, green, blue);
            return true;
        }

        if (trimmed.Length == 8
            && byte.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var alpha)
            && byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out red)
            && byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out green)
            && byte.TryParse(trimmed.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out blue))
        {
            color = new ColorValue(red, green, blue, alpha);
            return true;
        }

        return false;
    }

    private static string Format(double value) => value.ToString("0.##");

    private static string FormatColor(ColorValue color) => $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

    private static Rect NormalizeRect(Rect rect)
    {
        var width = Math.Abs(rect.Width);
        var height = Math.Abs(rect.Height);
        var x = rect.Width < 0 ? rect.X + rect.Width : rect.X;
        var y = rect.Height < 0 ? rect.Y + rect.Height : rect.Y;
        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        return new Rect(x, y, width, height);
    }

    private Point ClampToCanvas(Point point) =>
        new(
            Math.Clamp(point.X, 0, (_previewDocument ?? _document).Canvas.Width),
            Math.Clamp(point.Y, 0, (_previewDocument ?? _document).Canvas.Height));

    private enum WorkspaceInteractionMode
    {
        None,
        Pan,
        Move,
        Resize,
        Rotate,
        Create,
    }
}
