namespace StarterBot.Models
{
    public class Settings
    {
        public int Seed { get; set; }

        public int Players { get; set; }
        public int PlayerId { get; set; }

        /// <summary>
        /// NOTE: only when Players == 2 !!!
        /// </summary>
        public int Enemy => PlayerId == 0 ? 1 : 0;
    }
}