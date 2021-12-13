using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RabbitMQ.Library.Configuration;

namespace RabbitMQ.Windows.UI.Forms
{
    public partial class MainForm : Form
    {
        private readonly ILogger<MainForm> _logger;
        private readonly ConfigurationManager _configManager;
        public string[] ConfigurationNames { get; set; }
        public RabbitMqConfiguration CurrentConfig { get; set; }
        
        public MainForm(ILogger<MainForm> logger, ConfigurationManager configManager)
        {
            _logger = logger;
            _configManager = configManager;
            InitializeComponent();

            var formBindingSource = new BindingSource();
            formBindingSource.DataSource = this;

            
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeConfigDropdown();
        }

        private void InitializeConfigDropdown()
        {
            // Fill configuration names into combobox
            var configs = _configManager.GetConfigurationKeys();
            ConfigurationNames = configs;
            _configCombobox.Items.AddRange(configs);
            if (configs.Contains("default"))
            {
                _configCombobox.SelectedIndex = configs.ToList().IndexOf("default");
            }
            else
            {
                _configCombobox.SelectedIndex = 0;
            }
        }

        private void _configCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Switch current selected configuration
            var configs = _configManager.GetConfigurationKeys();
            // Set configuration object globally
            CurrentConfig = _configManager.Get(configs[_configCombobox.SelectedIndex]);
        }
    }
}
