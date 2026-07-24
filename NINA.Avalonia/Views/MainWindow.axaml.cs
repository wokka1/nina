using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
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

    public MainWindow()
    {
        InitializeComponent();

        var tree = this.FindControl<TreeView>("SequencerTree");
        if (tree != null) {
            SetupSequencerDragDrop(tree);
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
    /// Deliberately a functional first slice, not visual parity: no drag-clone/adorner preview,
    /// no drop-zone highlight yet - dropping in the wrong zone by a few pixels just reorders/
    /// reparents differently, it doesn't fail.
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

        try {
            await DragDrop.DoDragDropAsync(pressArgs, transfer, DragDropEffects.Move);
        } finally {
            dragInProgress = false;
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
    }

    private void OnSequencerDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
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
}
