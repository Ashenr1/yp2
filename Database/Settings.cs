using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using UP02.Models;

namespace UP02.Database
{
    static public class Settings
    {
        /// <summary>
        /// Имя файла настроек.
        /// </summary>
        private const string SettingsFile = "settings.json";

        /// <summary>
        /// IP-адрес сервера базы данных.
        /// </summary>
        public static string IPAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Порт подключения к базе данных.
        /// </summary>
        public static string Port { get; set; } = "3306";

        /// <summary>
        /// Логин пользователя базы данных.
        /// </summary>
        public static string Login { get; set; } = "root";

        /// <summary>
        /// Пароль пользователя базы данных.
        /// </summary>
        public static string Password { get; set; } = "";

        /// <summary>
        /// Имя базы данных.
        /// </summary>
        public static string DatabaseName { get; set; } = "EquipmentDB";

        /// <summary>
        /// Текущий авторизованный пользователь.
        /// </summary>
        public static Users CurrentUser { get; set; }

        /// <summary>
        /// Строка подключения к базе данных, сформированная на основе текущих настроек.
        /// </summary>
        public static string ConnectionString =>
            $"server={IPAddress};port={Port};user={Login};password={Password};database={DatabaseName};Connection Timeout=5;Pooling=false;";

        /// <summary>
        /// Сохраняет текущие настройки в файл.
        /// </summary>
        public static void SaveSettingsToFile()
        {
            try
            {
                var settings = new AppSettings
                {
                    IPAddress = IPAddress,
                    Port = Port,
                    Login = Login,
                    Password = Password,
                    DatabaseName = DatabaseName
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает настройки из файла.
        /// </summary>
        /// <returns>Возвращает true, если загрузка успешна, иначе false.</returns>
        public static bool LoadSettingsFromFile()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                    return false;

                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (settings != null)
                {
                    IPAddress = string.IsNullOrEmpty(settings.IPAddress) ? "127.0.0.1" : settings.IPAddress;
                    Port = string.IsNullOrEmpty(settings.Port) ? "3306" : settings.Port;
                    Login = string.IsNullOrEmpty(settings.Login) ? "root" : settings.Login;
                    Password = settings.Password ?? "";
                    DatabaseName = string.IsNullOrEmpty(settings.DatabaseName) ? "EquipmentDB" : settings.DatabaseName;

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Внутренний класс для хранения структуры настроек.
        /// </summary>
        private class AppSettings
        {
            /// <summary>
            /// IP-адрес сервера базы данных.
            /// </summary>
            public string IPAddress { get; set; } = "127.0.0.1";

            /// <summary>
            /// Порт подключения к базе данных.
            /// </summary>
            public string Port { get; set; } = "3306";

            /// <summary>
            /// Логин пользователя базы данных.
            /// </summary>
            public string Login { get; set; } = "root";

            /// <summary>
            /// Пароль пользователя базы данных.
            /// </summary>
            public string Password { get; set; } = "";

            /// <summary>
            /// Имя базы данных.
            /// </summary>
            public string DatabaseName { get; set; } = "EquipmentDB";
        }
    }
}