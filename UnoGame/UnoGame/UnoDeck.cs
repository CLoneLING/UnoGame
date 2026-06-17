using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame
{
    public class UnoDeck
    {
        private List<UnoCard> cards = new List<UnoCard>();
        private Random rand = new Random();

        public UnoDeck()
        {
            InitializeDeck();
            Shuffle();
        }

        private void InitializeDeck()
        {
            foreach (CardColor color in new[] { CardColor.Red, CardColor.Yellow, CardColor.Green, CardColor.Blue })
            {
                for (int num = 0; num <= 9; num++)
                {
                    int count = (num == 0) ? 1 : 2;
                    for (int i = 0; i < count; i++)
                        cards.Add(new UnoCard(color, CardType.Number, num));
                }
            }
            foreach (CardColor color in new[] { CardColor.Red, CardColor.Yellow, CardColor.Green, CardColor.Blue })
            {
                for (int i = 0; i < 2; i++)
                {
                    cards.Add(new UnoCard(color, CardType.Skip));
                    cards.Add(new UnoCard(color, CardType.Reverse));
                    cards.Add(new UnoCard(color, CardType.DrawTwo));
                }
            }
            for (int i = 0; i < 4; i++)
            {
                cards.Add(new UnoCard(CardColor._, CardType.Wild));
                cards.Add(new UnoCard(CardColor._, CardType.WildDrawFour));
            }
        }

        public void Shuffle() => cards = cards.OrderBy(x => rand.Next()).ToList();

        public UnoCard DrawCard()
        {
            if (cards.Count == 0) return null;
            var card = cards[0];
            cards.RemoveAt(0);
            return card;
        }

        public void AddCard(UnoCard card) => cards.Add(card);
        public int Remaining => cards.Count;
    }
}