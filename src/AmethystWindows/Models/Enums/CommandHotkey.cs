namespace AmethystWindows.Models.Enums
{
    public enum CommandHotkey
    {
        None,
        MoveFocusedToSpace1,
        MoveFocusedToSpace2,
        MoveFocusedToSpace3,
        MoveFocusedToSpace4,
        MoveFocusedToSpace5,
        MoveFocusMainWindow,
        DisplayCurrentInfo,
        RotateLayoutClockwise,
        SetMainPane,
        SwapFocusedAnticlockwise,
        ChangeWindowFocusAntiClockwise,
        ChangeWindowFocusClockwise,
        SwapFocusedClockwise,
        Redraw,
        MoveFocusPreviousScreen,
        MoveFocusNextScreen,
        RotateLayoutAntiClockwise,
        Shrink,
        ExpandMainPane,
        MoveFocusedNextScreen,
        MoveFocusedPreviousScreen,
        MoveNextSpace,
        MovePreviousSpace,

        // TODO ensure the numbers don't matter
        //None = 0,
        //MoveFocusedToSpace1 = 1,
        //MoveFocusedToSpace2 = 2,
        //MoveFocusedToSpace3 = 3,
        //MoveFocusedToSpace4 = 4,
        //MoveFocusedToSpace5 = 5,
        //MoveFocusMainWindow = 6,
        //DisplayCurrentInfo = 7,
        //RotateLayoutClockwise = 17,
        //SetMainPane = 18,
        //SwapFocusedAnticlockwise = 19, // move focused window left to right
        //ChangeWindowFocusAntiClockwise = 20,
        //ChangeWindowFocusClockwise = 21,
        //SwapFocusedClockwise = 22, // move focused window right to left        
        //Redraw = 23,
        //MoveFocusPreviousScreen = 24,
        //MoveFocusNextScreen = 25,
        //RotateLayoutAntiClockwise = 33,
        //Shrink = 34,
        //ExpandMainPane = 35,
        //MoveFocusedNextScreen = 36, // is this another monitor?
        //MoveFocusedPreviousScreen = 37, // is this another monitor?
        //MoveNextSpace = 38, // move to next virtual desktop (it should be alt shift win right)
        //MovePreviousSpace = 39, // move to next virtual desktop (it should be alt shift win left)

    }
}
