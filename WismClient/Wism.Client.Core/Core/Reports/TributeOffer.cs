namespace Wism.Client.Core.Reports
{
    /// <summary>
    ///     Represents a tribute demand made by a captor to a defeated player after
    ///     capturing one of their cities (Warlords manual §Tribute).
    /// </summary>
    public class TributeOffer
    {
        public TributeOffer(string captorClan, string loserClan, string capturedCity, int amount)
        {
            this.CaptorClan = captorClan;
            this.LoserClan = loserClan;
            this.CapturedCity = capturedCity;
            this.Amount = amount;
        }

        /// <summary>Clan short-name of the player who captured the city.</summary>
        public string CaptorClan { get; }

        /// <summary>Clan short-name of the player who lost the city.</summary>
        public string LoserClan { get; }

        /// <summary>Short-name of the city that was captured.</summary>
        public string CapturedCity { get; }

        /// <summary>Gold demanded as tribute.</summary>
        public int Amount { get; }
    }
}
