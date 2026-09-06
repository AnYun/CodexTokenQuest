using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CodexTokenQuest.Desktop;

// Small pixel sprites drawn by the shared UI, with no platform font or bitmap dependency.
internal sealed class ResetItemIcon : Control
{
    private static readonly string[][] Sprites =
    [
        ["................", ".....GGGGGG.....", ".....GHHHHG.....", "......OOOO......",
         "......O..O......", ".....OH...O.....", "....OH.....O....", "...OH.......O...",
         "...OHHAAAAAAO...", "...OHAAHHAAAO...", "...OAAHHHHAAO...", "...OAAAHHAAAO...",
         "...OAAAAAAAAO...", "....OAAAAAAO....", ".....OOOOOO.....", "................"],
        [".......H........", "......HHH.......", ".......H........", "......GGGG......",
         "......O..O......", ".....OH...O.....", "....OH.....O....", "...OHAAAAAAAO...",
         "..OHAAAAHAAAAO..", "...OAAAHHHAAO...", "....OAAAHAAO....", ".....OAAAAO.....",
         "......OAAO......", ".......OO.......", "................", "................"],
        ["................", ".....GGGGGG.....", ".....GHHHHG.....", "....OOOOOOOO....",
         "...OHHAAAAAAO...", "...OHAAAAAAAO...", "...OAAAAAAAAO...", "...OGGGGGGGGO...",
         "...OGGHHHHGGO...", "...OGGGHHGGGO...", "...OGGGHHGGGO...", "...OGGGGGGGGO...",
         "...OAAAAAAAAO...", "....OOOOOOOO....", "................", "................"],
        ["................", "....G.G.G.G.....", "...OOOOOOOOOO...", "..GOAAAAAAAAOG..",
         "...OAHHHHAAAO...", "..GOAAAAHAAAOG..", "...OAAAHAAAAO...", "..GOAAHHHHAAOG..",
         "...OAAHAAAAAO...", "..GOAAHHHHAAOG..", "...OAAAAAAAAO...", "..GOAAAAAAAAOG..",
         "...OOOOOOOOOO...", "....G.G.G.G.....", "................", "................"]
    ];

    public override void Render(DrawingContext context)
    {
        var sprite = Sprites[(int)HudColors.Theme];
        var pixel = Math.Max(1, Math.Floor(Math.Min(Bounds.Width, Bounds.Height) / 16));
        var left = Math.Floor((Bounds.Width - pixel * 16) / 2);
        var top = Math.Floor((Bounds.Height - pixel * 16) / 2);
        var accent = HudColors.Theme == HudTheme.ArcaneGlass ? HudColors.Cyan : HudColors.Green;
        for (var y = 0; y < sprite.Length; y++)
        for (var x = 0; x < sprite[y].Length; x++)
        {
            var color = sprite[y][x] switch
            {
                'O' => HudColors.Muted, 'G' => HudColors.Gold,
                'H' => HudColors.Cream, 'A' => accent, _ => (Color?)null
            };
            if (color is { } value)
                context.DrawRectangle(PixelArt.Brush(value), null, new Rect(left + x * pixel, top + y * pixel, pixel, pixel));
        }
    }
}
