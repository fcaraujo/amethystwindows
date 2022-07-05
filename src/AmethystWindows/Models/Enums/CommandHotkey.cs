namespace AmethystWindows.Models.Enums
{
    public enum CommandHotkey
    {
        None = 0,
        RotateLayoutClockwise = 17,
        RotateLayoutAntiClockwise = 33,
        SetMainPane = 18,
        SwapFocusedAnticlockwise = 19, // move focused window left to right
        SwapFocusedClockwise = 22, // move focused window right to left
        ChangeWindowFocusAntiClockwise = 20,
        ChangeWindowFocusClockwise = 21,
        MoveFocusPreviousScreen = 24,
        MoveFocusNextScreen = 25,
        ExpandMainPane = 35,
        Shrink = 34,
        MoveFocusedPreviousScreen = 37, // is this another monitor?
        MoveFocusedNextScreen = 36, // is this another monitor?
        Redraw = 23,
        MoveNextSpace = 38, // move to next virtual desktop (it should be alt shift win right)
        MovePreviousSpace = 39, // move to next virtual desktop (it should be alt shift win left)
        MoveFocusedToSpace1 = 1,
        MoveFocusedToSpace2 = 2,
        MoveFocusedToSpace3 = 3,
        MoveFocusedToSpace4 = 4,
        MoveFocusedToSpace5 = 5,
    }
}
