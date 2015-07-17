using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip.Configuration
{
    public class GossipConfigurationSection: ConfigurationSection
    {
        private const string FailTimeoutPropertyName = "FailTimeout";
        private const int FailTimeoutDefaultValue = 5;

        private const string RemoveTimeoutPropertyName = "RemoveTimeout";
        private const int RemoveTimeoutDefaultValue = 15;

        private const string PingsPerIterationPropertyName = "PingsPerIteration";
        private const int PingsPerIterationDefaultValue = 2;

        private const string RandomSeedPropertyName = "RandomSeed";
        private const int RandomSeedDefaultValue = -1;

        [ConfigurationProperty(FailTimeoutPropertyName, DefaultValue = FailTimeoutDefaultValue, IsRequired = false, IsKey = false)]
        public int FailTimeout
        {
            get { return (int)this[FailTimeoutPropertyName]; }
            set { this[FailTimeoutPropertyName] = value; }
        }

        [ConfigurationProperty(RemoveTimeoutPropertyName, DefaultValue = RemoveTimeoutDefaultValue, IsRequired = false, IsKey = false)]
        public int RemoveTimeout
        {
            get { return (int)this[RemoveTimeoutPropertyName]; }
            set { this[RemoveTimeoutPropertyName] = value; }
        }

        [ConfigurationProperty(PingsPerIterationPropertyName, DefaultValue = PingsPerIterationDefaultValue, IsRequired = false, IsKey = false)]
        public int PingsPerIteration
        {
            get { return (int)this[PingsPerIterationPropertyName]; }
            set { this[PingsPerIterationPropertyName] = value; }
        }

        [ConfigurationProperty(RandomSeedPropertyName, DefaultValue = RandomSeedDefaultValue, IsRequired = false, IsKey = false)]
        public int RandomSeed
        {
            get { return (int)this[RandomSeedPropertyName]; }
            set { this[RandomSeedPropertyName] = value; }
        }
    }
}
