using System.Globalization;
using System.Threading;

namespace StarterBot
{
    internal class Program
    {
        private static void Main()
        {
            // Set application culture.
            SetApplicationCulture();

            // Start bot logic. (blocking until done)
            Bot.Start(TheMoleStrategy.PlayTurn);
        }

        private static void SetApplicationCulture()
        {
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}