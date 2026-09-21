// Choices made before a run that the Game scene needs once it loads.
//
// Static, like PlayerProfile, so it survives the jump from the PreStart scene
// into the Game scene. Unlike PlayerProfile it isn't saved: it only matters
// for the next run, and pressing R after a defeat reuses it for a quick retry.
public static class RunSettings
{
    // The hero picked in PreStart. Null means nobody picked one -- e.g. Play was
    // pressed straight in the Game scene in the editor -- and the Game scene
    // shows its own hero picker instead.
    public static PlayerClassData ChosenClass;
}
