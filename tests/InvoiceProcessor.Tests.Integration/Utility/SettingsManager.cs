using Microsoft.Extensions.Configuration;
using System;

namespace InvoiceProcessor.Tests.Integration.Utility
{
    public class SettingsManager
    {
        private static SettingsManager _instance = new SettingsManager();

        public static SettingsManager Instance {
            get { return _instance; }
            private set { _instance = value; }
        }

        internal IConfiguration Configuration { get; }


        public SettingsManager() : this(null)
        { }

        public SettingsManager(IConfiguration config = null)
        {
            Configuration = config ?? DefaultConfiguration();
        }

        private IConfiguration DefaultConfiguration()
        {
            return CreateDefaultConfigurationBuilder().Build();
        }

        internal static IConfigurationBuilder CreateDefaultConfigurationBuilder()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Add(new CustomEnvironmentVariablesConfigurationSource());
        }

        public virtual string GetSetting(string settingKey)
        {
            return string.IsNullOrEmpty(settingKey)
                ? null
                : Configuration[settingKey];
        }

        public virtual void SetSetting(string settingKey, string settingValue)
        {
            if (!string.IsNullOrEmpty(settingKey)) {
                Environment.SetEnvironmentVariable(settingKey, settingValue);
            }
        }
    }
}
