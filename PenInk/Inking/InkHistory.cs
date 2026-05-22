using System.Windows.Controls;
using System.Windows.Ink;

namespace PenInk.Inking;

internal sealed class InkHistory
{
    private readonly InkCanvas canvas;
    private readonly Stack<IInkCommand> undoStack = new();
    private bool replaying;
    private DateTime lastChangeUtc = DateTime.MinValue;

    public InkHistory(InkCanvas canvas)
    {
        this.canvas = canvas;
        // 监听 StrokeCollection 的真实变化，撤销时记录增删差量。
        canvas.Strokes.StrokesChanged += (_, e) =>
        {
            if (replaying || (e.Added.Count == 0 && e.Removed.Count == 0))
            {
                return;
            }

            undoStack.Push(new StrokeDeltaCommand(e.Added.Clone(), e.Removed.Clone()));
            lastChangeUtc = DateTime.UtcNow;
            Changed?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? Changed;

    public bool CanUndo => undoStack.Count > 0;

    public bool ChangedAfter(DateTime utc) => lastChangeUtc >= utc;

    public void Undo()
    {
        if (!undoStack.TryPop(out var command))
        {
            return;
        }

        replaying = true;
        try
        {
            command.Undo(canvas.Strokes);
        }
        finally
        {
            replaying = false;
            lastChangeUtc = DateTime.UtcNow;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private interface IInkCommand
    {
        void Undo(StrokeCollection strokes);
    }

    private sealed class StrokeDeltaCommand(StrokeCollection added, StrokeCollection removed) : IInkCommand
    {
        public void Undo(StrokeCollection strokes)
        {
            // 先移除新增笔迹，再恢复被删除的笔迹，保证橡皮和清屏都能撤销。
            foreach (var stroke in added.ToList())
            {
                if (strokes.Contains(stroke))
                {
                    strokes.Remove(stroke);
                }
            }

            foreach (var stroke in removed)
            {
                if (!strokes.Contains(stroke))
                {
                    strokes.Add(stroke);
                }
            }
        }
    }
}
