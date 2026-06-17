namespace UnoGame
{
    public class Message
    {
        public string Type { get; set; }     // Join, StartGame, PlayCard, JumpIn, DrawCard, CallUno, SelectWildColor, GameState, Error, GameOver
        public string Content { get; set; }
        public string SenderId { get; set; }
    }
}