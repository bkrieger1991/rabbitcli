using RabbitMQ.Library.Configuration;

namespace RabbitMQ.Library.Test
{
    public class MockConfigurationManager : ConfigurationManager
    {
        public string WrittenConfig { get; set; }
        private readonly string _configToLoad;

        public MockConfigurationManager(string configToLoad)
        {
            _configToLoad = configToLoad;
        }

        protected override string ReadConfiguration()
        {
            return _configToLoad;
        }

        protected override void WriteConfiguration(string data)
        {
            WrittenConfig = data;
        }

        protected override void EnsureConfigFileExists()
        {
            // Do nothing...
        }
    }
}