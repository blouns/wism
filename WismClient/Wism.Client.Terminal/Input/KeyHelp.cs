namespace Wism.Client.Terminal.Input;

public static class KeyHelp
{
    public static string Text => """
        WISM Terminal

        Commands:
          wism play
          wism new profile=classic-warlords world=Illuria
          wism load save=<path>
          wism replay capture=<recording-dir>
          wism mod validate profile=<id> packs=a,b
          wism keys
          wism doctor
          wism render-test

        Keys:
          Arrows      Move selected stack; otherwise move cursor/pan
          S           Select the stack at the cursor
          Space/Tab   Select next movable army
          Esc         Deselect armies
          M           Move selected stack to cursor
          A           Attack cursor target
          D           Defend selected stack
          Q           Quit selected stack for the turn
          Z           Search current site
          T / O       Take / drop hero items
          P           Show or start production at cursor city
          E           End turn
          :           Command palette
          ?           Toggle help
          + / -       Change tile density
          F           Follow selected stack
        """;
}
