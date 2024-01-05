using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Library.Configuration;
using Xunit;

namespace RabbitMQ.Library.Test
{
    public class ConfigurationManagerTest
    {
        private readonly string _exampleEncryptedConfig;

        public ConfigurationManagerTest()
        {
            var config = new RabbitMqConfiguration()
            {
                Amqp =
                {
                    Hostname = "localhost",
                    Port = 5672,
                    Password = "guest",
                    Username = "guest",
                    VirtualHost = "/",
                    IsAmqps = false
                },
                Web =
                {
                    Hostname = "localhost",
                    Port = 15672,
                    Password = "guest",
                    Username = "guest",
                    Ssl = false
                },
                Name = "default"
            };

            _exampleEncryptedConfig = JsonConvert.SerializeObject(config).Encrypt(ConfigurationManager.GetEncryptionKey());
        }

        [Fact]
        public void Should_Load_Configuration_Old_Format_Without_Error()
        {
            var config = $"{{\"default\": \"{_exampleEncryptedConfig}\"}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();
            manager.Get("default").Should().NotBeNull();
        }

        [Fact]
        public void Should_Migrate_Old_Configuration_Into_New_Format()
        {
            var config = $"{{\"default\": \"{_exampleEncryptedConfig}\"}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();

            manager.WrittenConfig.Should().NotBeNullOrWhiteSpace();
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(manager.WrittenConfig);
            deserialized.Should().ContainKey(nameof(Configuration.Configuration.TextEditorPath));
        }

        [Fact]
        public void Should_Load_Configuration_Current_Format_Without_Error()
        {
            var config = $"{{\"TextEditorPath\":\"unit-test\", \"ConfigurationCollection\":{{\"default\": \"{_exampleEncryptedConfig}\"}}}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();
            manager.Get("default").Should().NotBeNull();
            manager.GetProperty(nameof(Configuration.Configuration.TextEditorPath)).Should().Be("unit-test");
        }

        [Fact]
        public void Should_Store_Property()
        {
            var config = $"{{\"TextEditorPath\":\"unit-test\", \"ConfigurationCollection\":{{\"default\": \"{_exampleEncryptedConfig}\"}}}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();
            manager.SetProperty(nameof(Configuration.Configuration.TextEditorPath), "test");

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(manager.WrittenConfig);
            var key = nameof(Configuration.Configuration.TextEditorPath);
            deserialized.Should().ContainKey(key).And.Subject[key].ToString().Should().Be("test");
        }

        [Fact]
        public void Should_Store_Configuration()
        {
            var config = "{\"TextEditorPath\":\"unit-test\", \"ConfigurationCollection\":{}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();

            manager.AddConfiguration(new RabbitMqConfiguration() {Name = "unit-test-config"});

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(manager.WrittenConfig);
            var key = nameof(Configuration.Configuration.ConfigurationCollection);
            deserialized.Should().NotBeNull();
            deserialized.Should().ContainKey(key);
            var value = (deserialized[key] as JObject)?.ToString();
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(value ?? "{}");
            configDict.Should().ContainKey("unit-test-config");
        }
    }
}
