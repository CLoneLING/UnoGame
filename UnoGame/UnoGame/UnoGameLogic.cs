using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame
{
    public class UnoGameLogic
    {
        public List<Player> Players { get; private set; }
        public UnoCard CurrentTopCard { get; set; }
        public int CurrentPlayerIndex { get; set; } = 0;
        public bool IsClockwise { get; set; } = true;
        public int DrawStack { get; set; } = 0;
        public CardColor? SelectedWildColor { get; set; } = null; // 保留字段但不再使用

        private UnoDeck drawPile;
        private List<UnoCard> discardPile;

        public UnoGameLogic(List<Player> players)
        {
            Players = players;
            drawPile = new UnoDeck();
            discardPile = new List<UnoCard>();
            StartGame();
        }

        private void StartGame()
        {
            for (int i = 0; i < 7; i++)
                foreach (var p in Players)
                    p.HandCards.Add(drawPile.DrawCard());

            int maxAttempts = 100;
            int attempts = 0;
            do
            {
                CurrentTopCard = drawPile.DrawCard();
                if (CurrentTopCard == null) throw new Exception("牌堆为空");
                discardPile.Add(CurrentTopCard);
                attempts++;
                if (attempts > maxAttempts) break;
            } while (CurrentTopCard.Type != CardType.Number);
        }

        public bool IsValidPlay(UnoCard playedCard, CardColor? declaredColor = null)
        {
            // 罚牌累积时只能出 +2 或 +4
            if (DrawStack > 0)
            {
                return playedCard.Type == CardType.DrawTwo || playedCard.Type == CardType.WildDrawFour;
            }

            // 万能牌本身总是合法
            if (playedCard.Type == CardType.Wild || playedCard.Type == CardType.WildDrawFour)
                return true;

            // 如果当前顶牌是万能牌（颜色为None），则任何颜色的牌都允许出（包括所有数字牌和功能牌）
            if (CurrentTopCard.Type == CardType.Wild || CurrentTopCard.Type == CardType.WildDrawFour)
            {
                // 万能牌后可以出任意颜色的牌，但+2/+4不能凭空出（它们本身也是功能牌，但颜色匹配总是true）
                // 实际上任何牌都允许，因为颜色已通配
                return true;
            }

            // 正常匹配规则
            if (playedCard.Color == CurrentTopCard.Color)
                return true;
            if (playedCard.Type == CurrentTopCard.Type && playedCard.Type != CardType.Number)
                return true;
            if (playedCard.Type == CardType.Number && CurrentTopCard.Type == CardType.Number && playedCard.Number == CurrentTopCard.Number)
                return true;
            return false;
        }

        public bool CanJumpIn(UnoCard card)
        {
            if (DrawStack > 0) return false;
            if (card.Type == CardType.Wild || card.Type == CardType.WildDrawFour)
                return false;

            // 如果当前顶牌是万能牌，抢出规则：必须与万能牌相同（但万能牌没有颜色和数字，所以不能抢出）
            // 实际上万能牌不能抢出，因为上面已经排除了Wild类型，所以这里直接使用原逻辑
            if (card.Color == CurrentTopCard.Color && card.Type == CurrentTopCard.Type)
            {
                if (card.Type == CardType.Number)
                    return card.Number == CurrentTopCard.Number;
                else
                    return true;
            }
            return false;
        }

        public string GetAllowedPlayHint()
        {
            if (DrawStack > 0)
                return "必须先出 +2 或 +4 转移罚牌，或者点击「抽牌」接受惩罚";
            List<string> hints = new List<string>();
            if (CurrentTopCard.Type == CardType.Wild || CurrentTopCard.Type == CardType.WildDrawFour)
                return "万能牌后，任何颜色的牌都可以出";
            if (CurrentTopCard.Color != CardColor._)
                hints.Add($"颜色 {CurrentTopCard.Color}");
            if (CurrentTopCard.Type == CardType.Number)
                hints.Add($"数字 {CurrentTopCard.Number}");
            else if (CurrentTopCard.Type != CardType.Wild && CurrentTopCard.Type != CardType.WildDrawFour)
                hints.Add($"符号 {CurrentTopCard.GetDisplayText()}");
            hints.Add("万能牌 / 万能+4");
            return string.Join(" 或 ", hints);
        }

        public void PlayCard(Player player, int cardIndex, CardColor? declaredColor = null)
        {
            var card = player.HandCards[cardIndex];
            if (!IsValidPlay(card, declaredColor))
                throw new InvalidOperationException($"你不能出这张牌！，必须出{GetAllowedPlayHint()}");

            player.HandCards.RemoveAt(cardIndex);
            discardPile.Add(card);
            CurrentTopCard = card;
            // 万能牌的颜色保持为None（不做任何改变）
            player.HasCalledUno = false;

            bool turnChanged = false;
            switch (card.Type)
            {
                case CardType.Skip:
                    CurrentPlayerIndex = GetNextPlayerIndex(2);
                    turnChanged = true;
                    break;
                case CardType.Reverse:
                    IsClockwise = !IsClockwise;
                    if (Players.Count == 2)
                        CurrentPlayerIndex = GetNextPlayerIndex(1);
                    else
                        CurrentPlayerIndex = GetNextPlayerIndex(0);
                    turnChanged = true;
                    break;
                case CardType.DrawTwo:
                    DrawStack += 2;
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
                case CardType.WildDrawFour:
                    DrawStack += 4;
                    // 颜色不再设置，保持None
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
                case CardType.Wild:
                    // 不变色，只作为通配
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
                default:
                    break;
            }
            if (!turnChanged)
                CurrentPlayerIndex = GetNextPlayerIndex(1);
        }

        public void JumpInPlay(Player player, int cardIndex, CardColor? declaredColor = null)
        {
            if (DrawStack > 0)
                throw new InvalidOperationException("当前有罚牌累积，不能抢出");
            var card = player.HandCards[cardIndex];
            if (!CanJumpIn(card))
                throw new InvalidOperationException("不能抢出，牌与顶部牌不完全相同");

            CurrentPlayerIndex = Players.IndexOf(player);
            player.HandCards.RemoveAt(cardIndex);
            discardPile.Add(card);
            CurrentTopCard = card;
            player.HasCalledUno = false;

            bool turnChanged = false;
            switch (card.Type)
            {
                case CardType.Skip:
                    CurrentPlayerIndex = GetNextPlayerIndex(2);
                    turnChanged = true;
                    break;
                case CardType.Reverse:
                    IsClockwise = !IsClockwise;
                    if (Players.Count == 2)
                        CurrentPlayerIndex = GetNextPlayerIndex(1);
                    else
                        CurrentPlayerIndex = GetNextPlayerIndex(0);
                    turnChanged = true;
                    break;
                case CardType.DrawTwo:
                    DrawStack += 2;
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
                case CardType.WildDrawFour:
                    DrawStack += 4;
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
                case CardType.Wild:
                    CurrentPlayerIndex = GetNextPlayerIndex(1);
                    turnChanged = true;
                    break;
            }
            if (!turnChanged)
                CurrentPlayerIndex = GetNextPlayerIndex(1);
        }

        public void DrawCard(Player player)
        {
            if (DrawStack > 0)
            {
                for (int i = 0; i < DrawStack; i++)
                {
                    if (drawPile.Remaining == 0) ReshuffleDiscard();
                    player.HandCards.Add(drawPile.DrawCard());
                }
                DrawStack = 0;
                CurrentPlayerIndex = GetNextPlayerIndex(1);
            }
            else
            {
                if (drawPile.Remaining == 0) ReshuffleDiscard();
                var card = drawPile.DrawCard();
                player.HandCards.Add(card);
                CurrentPlayerIndex = GetNextPlayerIndex(1);
            }
        }

        public UnoCard DrawCardFromPile()
        {
            if (drawPile.Remaining == 0) ReshuffleDiscard();
            return drawPile.DrawCard();
        }

        private void ReshuffleDiscard()
        {
            var top = discardPile.Last();
            discardPile.RemoveAt(discardPile.Count - 1);
            foreach (var card in discardPile)
                drawPile.AddCard(card);
            drawPile.Shuffle();
            discardPile.Clear();
            discardPile.Add(top);
        }

        private int GetNextPlayerIndex(int offset)
        {
            int next = CurrentPlayerIndex + (IsClockwise ? offset : -offset);
            next %= Players.Count;
            if (next < 0) next += Players.Count;
            return next;
        }

        public int GetPlayerIndex(Player p) => Players.IndexOf(p);
    }
}