namespace PenInk.Core.Input;

public readonly record struct PointerTap(DateTime StartedUtc, InkPoint End, bool IsDotCandidate);
