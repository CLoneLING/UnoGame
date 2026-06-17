using System.Drawing;

namespace UnoGame
{
    public enum CardColor { Red, Yellow, Green, Blue, _, }
    public enum CardType { Number, Skip, Reverse, DrawTwo, Wild, WildDrawFour }

    public class UnoCard
    {
        public CardColor Color { get; set; }
        public CardType Type { get; set; }
        public int Number { get; set; }
        public int Points { get; set; }

        public UnoCard(CardColor color, CardType type, int number = -1)
        {
            Color = color;
            Type = type;
            Number = (type == CardType.Number) ? number : -1;
            Points = type == CardType.Number ? number : (type == CardType.Wild || type == CardType.WildDrawFour ? 50 : 20);
        }

        public string GetDisplayText()
        {
            if (Type == CardType.Number) return Number.ToString();
            if (Type == CardType.Skip) return "跳过";
            if (Type == CardType.Reverse) return "反转";
            if (Type == CardType.DrawTwo) return "抽两张 +2";
            if (Type == CardType.Wild) return "万能牌";
            if (Type == CardType.WildDrawFour) return "万能牌 +4";
            return "";
        }

        public string GetImageFileName()
        {
            if (Type == CardType.Number) return $"{Color}_{Number}.png";
            if (Type == CardType.Skip) return $"{Color}_Skip.png";
            if (Type == CardType.Reverse) return $"{Color}_Reverse.png";
            if (Type == CardType.DrawTwo) return $"{Color}_DrawTwo.png";
            if (Type == CardType.Wild) return "Wild.png";
            if (Type == CardType.WildDrawFour) return "WildDrawFour.png";
            return "";
        }

        public override string ToString() => GetDisplayText();
    }
}