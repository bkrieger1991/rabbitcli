using System;
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
        private readonly string _exampleEncryptedConfig = "opzZeXICsX/4BzbUHrXnUhIkhhS6R2hZzqiGTpkeqQwKQ85xxVnUPeWQdoX826GVdfd83Ih6mW1fG4iRZteDl0eEkTOuk7oxjHGltQSSvAfmUdaOb52/4LkEghSSl5amfWePHkLxCUuiGR7uOxM2w+EL4L+1KJKogyEPzE8AkObnluw8n/UDK1ePOHVrix1u3DIr4KLIo8MSS3DSJoYpTE6Opx4h/gDLkPZ59TxfD2in//wCjoepNsuY1pepoucOPDpBU7fTfXfvstkWijDO+w==";

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
            var config = "{{\"TextEditorPath\":\"unit-test\", \"ConfigurationCollection\":{{}}}}";
            var manager = new MockConfigurationManager(config);
            manager.Initialize();

            manager.AddConfiguration(new RabbitMqConfiguration() {Name = "unit-test-config"});

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(manager.WrittenConfig);
            var key = nameof(Configuration.Configuration.ConfigurationCollection);
            deserialized.Should().ContainKey(key);
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, string>>((deserialized[key] as JObject).ToString());
            configDict.Should().ContainKey("unit-test-config");
        }
    }
}
