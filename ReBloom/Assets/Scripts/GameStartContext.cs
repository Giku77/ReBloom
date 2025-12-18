public static class GameStartContext
{
    public enum Mode { NewGame, Continue, Debug }
    public static Mode StartMode = Mode.Debug;
    public static string SlotId = "slot1";
}
