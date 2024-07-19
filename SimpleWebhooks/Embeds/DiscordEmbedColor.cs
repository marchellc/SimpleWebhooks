using System.Globalization;
using System.Drawing;

namespace SimpleWebhooks.Embeds
{
    public struct DiscordEmbedColor
    {
        public Color Color { get; }

        public DiscordEmbedColor(int color)
            => Color = ColorTranslator.FromHtml(color.ToString("X6"));

        public DiscordEmbedColor(Color color)
            => Color = color;

        public int ToHexRgb()
        {
            var s = Color.R.ToString("X2") + Color.G.ToString("X2") + Color.B.ToString("X2");

            return int.Parse(s, NumberStyles.HexNumber, null);
        }
    }
}