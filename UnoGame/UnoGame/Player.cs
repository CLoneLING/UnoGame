using System.Collections.Generic;

namespace UnoGame
{
    public class Player
    {
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public List<UnoCard> HandCards { get; set; } = new List<UnoCard>();
        public bool HasCalledUno { get; set; } = false;
        public int TotalScore { get; set; } = 0;
    }
}