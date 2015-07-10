using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip.Configuration
{
    public class GossipConfigurationSection : ConfigurationSection
    {
        public const string ConfigurationSectionName = "GossipConfiguration";

        private const string FailTimeoutPropertyName = "FailTimeout";
        private const string RemoveTimeoutPropertyName = "RemoveTimeout";
        private const string RequestsPerUpdatePropertyName = "RequestsPerUpdate";
        private const string RandomSeedPropertyName = "RandomSeed";

        [ConfigurationProperty(FailTimeoutPropertyName, DefaultValue = GossipConfiguration.FailTimeoutDefaultValue, IsRequired = false, IsKey = false)]
        public int FailTimeout
        {
            get { return (int)this[FailTimeoutPropertyName]; }
            set { this[FailTimeoutPropertyName] = value; }
        }

        [ConfigurationProperty(RemoveTimeoutPropertyName, DefaultValue = GossipConfiguration.RemoveTimeoutDefaultValue, IsRequired = false, IsKey = false)]
        public int RemoveTimeout
        {
            get { return (int)this[RemoveTimeoutPropertyName]; }
            set { this[RemoveTimeoutPropertyName] = value; }
        }

        [ConfigurationProperty(RequestsPerUpdatePropertyName, DefaultValue = GossipConfiguration.RequestsPerUpdateDefaultValue, IsRequired = false, IsKey = false)]
        public int RequestsPerUpdate
        {
            get { return (int)this[RequestsPerUpdatePropertyName]; }
            set { this[RequestsPerUpdatePropertyName] = value; }
        }

        [ConfigurationProperty(RandomSeedPropertyName, DefaultValue = GossipConfiguration.RandomSeedDefaultValue, IsRequired = false, IsKey = false)]
        public int RandomSeed
        {
            get { return (int)this[RandomSeedPropertyName]; }
            set { this[RandomSeedPropertyName] = value; }
        }
    }
}
