using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Configs
{
    /// <summary>
    /// Класс для чтения из файла и конвертации конфига
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        public ConfigManager()
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            _configInstance = new Lazy<Config>(LoadConfig);
        }
        readonly Lazy<Config> _configInstance;
        readonly ILogger _logger;

        public static string ProgramFolder => Directory.GetCurrentDirectory(); //Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Загрузка конфига
        /// </summary>
        /// <returns>Конфиг</returns>
        private Config LoadConfig()
        {
            try
            {
                var configText = File.ReadAllText(Path.Combine(ProgramFolder, "config","Config.json"));

                JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();
                SerializerSettings.Converters.Add(new StringEnumConverter());
                SerializerSettings.Error = Serializer_Error;

                return JsonConvert.DeserializeObject<Config>(configText, SerializerSettings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Can't initialize config");
                return null;
            }
        }

        public void Serializer_Error(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            _logger.Error("Can't initialize config:" + e.ErrorContext.Error.Message);
            e.ErrorContext.Handled = true;
        }


        public Config GetConfig() => _configInstance.Value;
    }
}
