namespace jac.mp.Gossip.Configuration
{
    public class GossipConfiguration
    {
        public const int FailTimeoutDefaultValue = 5;
        public const int RemoveTimeoutDefaultValue = 15;
        public const int RequestsPerUpdateDefaultValue = 2;
        public const int RandomSeedDefaultValue = -1;

        /// <summary>
        /// Default configuration.
        /// </summary>
        public static GossipConfiguration DefaultConfiguration
        {
            get
            {
                return new GossipConfiguration()
                  {
                      FailTimeout = FailTimeoutDefaultValue,
                      RemoveTimeout = RemoveTimeoutDefaultValue,
                      RequestsPerUpdate = RequestsPerUpdateDefaultValue,
                      RandomSeed = RandomSeedDefaultValue
                  };
            }
        }

        public int FailTimeout { get; set; }

        public int RemoveTimeout { get; set; }

        public int RequestsPerUpdate { get; set; }

        public int RandomSeed { get; set; }

        private GossipConfiguration() { }

        internal GossipConfiguration(GossipConfigurationSection config)
        {
            FailTimeout = config.FailTimeout;
            RemoveTimeout = config.RemoveTimeout;
            RequestsPerUpdate = config.RequestsPerUpdate;
            RandomSeed = config.RandomSeed;
        }

        public GossipConfiguration Clone()
        {
            return MemberwiseClone() as GossipConfiguration;
        }
    }
}
