using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NINA.Avalonia.Utility;
using NINA.Avalonia.ViewModels;
using NINA.Core.Enum;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;

namespace NINA.Avalonia.Views;

public partial class MainWindow : Window
{
    private static readonly DataFormat<ISequenceEntity> SequenceDragFormat =
        DataFormat.CreateInProcessFormat<ISequenceEntity>("NINA.Sequencer.DragDrop.Source");

    private const double DragThreshold = 6;

    private ISequenceEntity? dragCandidate;
    private PointerPressedEventArgs? dragCandidatePressArgs;
    private Point dragStartPoint;
    private bool dragInProgress;

    private Border? dragGhost;
    private TextBlock? dragGhostText;
    private Border? dropZoneHighlight;
    private DropTargetEnum? currentDropZone;

    public MainWindow()
    {
        InitializeComponent();

        dragGhost = this.FindControl<Border>("DragGhost");
        dragGhostText = this.FindControl<TextBlock>("DragGhostText");
        dropZoneHighlight = this.FindControl<Border>("DropZoneHighlight");

        var quitButton = this.FindControl<Button>("QuitButton");
        if (quitButton != null) {
            quitButton.Click += (_, _) => Close();
        }

        var tree = this.FindControl<TreeView>("SequencerTree");
        if (tree != null) {
            SetupSequencerDragDrop(tree);
        }

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Real NINA theme support (2026-08-22) - NinaThemeService only sets Avalonia resources, it
    /// has no reference to the View, so the actual "does the theme visually apply" switch (the
    /// "nina-themed" class on this root Window, matched by MainWindow.axaml's themed Styles
    /// block) lives here, same as other view-layer glue this project keeps in code-behind
    /// (OnLoadFromFileClick's file picker is the precedent). Defaults off (today's plain look)
    /// until OptionsViewModel.UsePlainDefault/SelectedTheme say otherwise.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e) {
        if (DataContext is not MainViewModel mainViewModel) {
            return;
        }
        ApplyThemeState(mainViewModel.OptionsVM);
        mainViewModel.OptionsVM.PropertyChanged += (_, args) => {
            if (args.PropertyName is nameof(OptionsViewModel.UsePlainDefault) or nameof(OptionsViewModel.SelectedTheme)) {
                ApplyThemeState(mainViewModel.OptionsVM);
            }
        };
    }

    private void ApplyThemeState(OptionsViewModel optionsViewModel) {
        if (!optionsViewModel.UsePlainDefault && optionsViewModel.SelectedTheme != null) {
            NinaThemeService.ApplyTheme(optionsViewModel.SelectedTheme);
            Classes.Add("nina-themed");
        } else {
            Classes.Remove("nina-themed");
        }
    }

    /// <summary>
    /// Real Sequencer drag-and-drop, first slice (2026-07-24). Reuses the real, unchanged domain
    /// contracts NINA.Sequencer already defines for this (IDroppable/IDropContainer/
    /// DropIntoParameters/DropTargetEnum, and SequenceContainer's own DropInSequenceItem logic -
    /// see NINA.Sequencer/Container/SequenceContainer.cs and NINA.Sequencer/Behaviors/
    /// DragOverBehavior.cs for the real WPF version's Top/Bottom/Center zone rule this mirrors).
    /// The real WPF DragDropBehavior/DragOverBehavior/DropIntoBehavior trio (Microsoft.Xaml.
    /// Behaviors + manual VisualTreeHelper hit-testing + RenderTargetBitmap drag-clone adorners)
    /// is deeply WPF-specific and not portable - this uses Avalonia's own DragDrop API instead
    /// (DataTransfer/DataFormat.CreateInProcessFormat, not the older, now-obsolete DataObject).
    /// Visual polish added 2026-08-22: a hand-tracked ghost overlay (DragGhost in MainWindow.axaml)
    /// follows the pointer for the dragged item's name, and a drop-zone highlight bar shows which
    /// of Top/Bottom/Center DetermineDropZone currently resolves to. Neither is OS drag imagery -
    /// Avalonia's DoDragDropAsync hands the actual drag session to the platform and doesn't expose
    /// a custom drag-cursor image API, so both are ordinary controls in an overlay Panel,
    /// repositioned from DragOver event coordinates instead.
    /// </summary>
    private void SetupSequencerDragDrop(TreeView tree)
    {
        DragDrop.SetAllowDrop(tree, true);

        tree.AddHandler(InputElement.PointerPressedEvent, OnSequencerPointerPressed);
        tree.AddHandler(InputElement.PointerMovedEvent, OnSequencerPointerMoved);
        tree.AddHandler(InputElement.PointerReleasedEvent, OnSequencerPointerReleased);

        tree.AddHandler(DragDrop.DragOverEvent, OnSequencerDragOver);
        tree.AddHandler(DragDrop.DropEvent, OnSequencerDrop);
    }

    private void OnSequencerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        dragCandidate = null;
        dragCandidatePressArgs = null;
        dragInProgress = false;

        if (!e.GetCurrentPoint((Visual?)sender).Properties.IsLeftButtonPressed) {
            return;
        }

        var item = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>();
        if (item?.DataContext is ISequenceEntity entity) {
            dragCandidate = entity;
            dragCandidatePressArgs = e;
            dragStartPoint = e.GetPosition(null);
        }
    }

    private async void OnSequencerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragCandidate == null || dragCandidatePressArgs == null || dragInProgress) {
            return;
        }
        if (!e.GetCurrentPoint((Visual?)sender).Properties.IsLeftButtonPressed) {
            return;
        }

        var current = e.GetPosition(null);
        var distance = Math.Sqrt(Math.Pow(current.X - dragStartPoint.X, 2) + Math.Pow(current.Y - dragStartPoint.Y, 2));
        if (distance < DragThreshold) {
            return;
        }

        dragInProgress = true;
        var source = dragCandidate;
        var pressArgs = dragCandidatePressArgs;
        dragCandidate = null;
        dragCandidatePressArgs = null;

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(SequenceDragFormat, source));

        ShowDragGhost(source, dragStartPoint);

        try {
            await DragDrop.DoDragDropAsync(pressArgs, transfer, DragDropEffects.Move);
        } finally {
            dragInProgress = false;
            HideDragGhost();
            HideDropZoneHighlight();
        }
    }

    private void ShowDragGhost(ISequenceEntity source, Point atWindowPoint)
    {
        if (dragGhost == null || dragGhostText == null) {
            return;
        }
        dragGhostText.Text = source.Name;
        dragGhost.RenderTransform = new TranslateTransform(atWindowPoint.X + 12, atWindowPoint.Y + 12);
        dragGhost.IsVisible = true;
    }

    private void HideDragGhost()
    {
        if (dragGhost != null) {
            dragGhost.IsVisible = false;
        }
    }

    private void HideDropZoneHighlight()
    {
        currentDropZone = null;
        if (dropZoneHighlight != null) {
            dropZoneHighlight.IsVisible = false;
        }
    }

    private void OnSequencerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        dragCandidate = null;
        dragCandidatePressArgs = null;
    }

    private void OnSequencerDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(SequenceDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;

        if (dragGhost != null) {
            var windowPos = e.GetPosition(this);
            dragGhost.RenderTransform = new TranslateTransform(windowPos.X + 12, windowPos.Y + 12);
        }

        UpdateDropZoneHighlight(e);
    }

    private void UpdateDropZoneHighlight(DragEventArgs e)
    {
        var hovered = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>();
        if (dropZoneHighlight == null || hovered?.DataContext is not ISequenceEntity target) {
            HideDropZoneHighlight();
            return;
        }

        var bounds = hovered.Bounds;
        var pos = e.GetPosition(hovered);
        var zone = DetermineDropZone(pos.Y, bounds.Height, target is ISequenceContainer);
        currentDropZone = zone;

        var topLeftInWindow = hovered.TranslatePoint(new Point(0, 0), this) ?? default;
        const double barThickness = 3;

        switch (zone) {
            case DropTargetEnum.Top:
                dropZoneHighlight.Width = bounds.Width;
                dropZoneHighlight.Height = barThickness;
                dropZoneHighlight.RenderTransform = new TranslateTransform(topLeftInWindow.X, topLeftInWindow.Y);
                break;
            case DropTargetEnum.Bottom:
                dropZoneHighlight.Width = bounds.Width;
                dropZoneHighlight.Height = barThickness;
                dropZoneHighlight.RenderTransform =
                    new TranslateTransform(topLeftInWindow.X, topLeftInWindow.Y + bounds.Height - barThickness);
                break;
            default:
                dropZoneHighlight.Width = bounds.Width;
                dropZoneHighlight.Height = bounds.Height;
                dropZoneHighlight.RenderTransform = new TranslateTransform(topLeftInWindow.X, topLeftInWindow.Y);
                break;
        }
        dropZoneHighlight.IsVisible = true;
    }

    private void OnSequencerDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        HideDropZoneHighlight();

        var source = e.DataTransfer.TryGetValue(SequenceDragFormat);
        if (source == null) {
            return;
        }

        var hovered = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>();
        if (hovered?.DataContext is not ISequenceEntity target) {
            return;
        }

        if (ReferenceEquals(source, target)) {
            return;
        }

        var bounds = hovered.Bounds;
        var pos = e.GetPosition(hovered);
        var zone = DetermineDropZone(pos.Y, bounds.Height, target is ISequenceContainer);

        IDropContainer? containerToInvoke = target is ISequenceContainer targetContainer && zone == DropTargetEnum.Center
            ? targetContainer as IDropContainer
            : target.Parent as IDropContainer;

        if (containerToInvoke == null) {
            return;
        }

        var parameters = new DropIntoParameters(source, target, zone);
        if (containerToInvoke.DropIntoCommand.CanExecute(parameters)) {
            containerToInvoke.DropIntoCommand.Execute(parameters);
        }
    }

    private static DropTargetEnum DetermineDropZone(double relativeY, double height, bool targetIsContainer)
    {
        if (height <= 0) {
            return DropTargetEnum.Center;
        }

        // Mirrors the real WPF DragOverBehavior's zone rule (top/bottom margins reorder relative
        // to the hovered item, the middle band drops "into" it if it's a container) - simplified
        // to fixed fractions of the row height instead of configurable pixel margins.
        var topBand = height * 0.25;
        var bottomBand = height * 0.75;

        if (relativeY < topBand) {
            return DropTargetEnum.Top;
        }
        if (relativeY > bottomBand) {
            return DropTargetEnum.Bottom;
        }
        return targetIsContainer ? DropTargetEnum.Center : DropTargetEnum.Top;
    }

    /// <summary>
    /// Framing Assistant's "Load From File..." button (2026-08-02). File *picking* has to happen
    /// here, not in FramingAssistantViewModel - Avalonia's IStorageProvider is only reachable
    /// through a real TopLevel/window reference, which a portable ViewModel deliberately doesn't
    /// have. Once a file's chosen, everything else (decode/stretch/WCS framing) is real portable
    /// work in FileSkySurvey.GetImagePortableFromPath, called via
    /// FramingAssistantViewModel.LoadFromFileAsync.
    /// </summary>
    private async void OnLoadFromFileClick(object? sender, RoutedEventArgs e) {
        if (DataContext is not MainViewModel mainViewModel) {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Load image for Framing Assistant",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> {
                new("Image files") {
                    Patterns = new[] {
                        "*.tif", "*.tiff", "*.jpeg", "*.jpg", "*.png",
                        "*.cr2", "*.cr3", "*.nef", "*.raw", "*.raf", "*.pef", "*.dng", "*.arw", "*.orf", "*.rw2",
                        "*.fit", "*.fts", "*.fits", "*.fit.fz", "*.fits.fz", "*.xisf"
                    }
                }
            }
        });

        var picked = files.FirstOrDefault();
        if (picked?.TryGetLocalPath() is string path) {
            await mainViewModel.FramingAssistantVM.LoadFromFileAsync(path);
        }
    }
}
