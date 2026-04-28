using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP02.Context;
using UP02.Elements;
using UP02.Helpers;
using UP02.Models;
using UP02.Pages.Elements;

namespace UP02.Pages.Main
{
    /// <summary>
    /// Логика взаимодействия для PageNetworkSettings.xaml
    /// </summary>
    public partial class PageNetworkSettings : Page
    {
        List<NetworkSettings> OriginalRecords = new List<NetworkSettings>();
        List<NetworkSettings> CurrentList = new List<NetworkSettings>();

        /// <summary>
        /// Конструктор страницы, инициализирует компоненты и загружает данные о настройках сети из базы данных.
        /// </summary>
        public PageNetworkSettings()
        {
            InitializeComponent();
            LoadData();
        }

        /// <summary>
        /// Загружает данные из базы данных
        /// </summary>
        private void LoadData()
        {
            using var databaseContext = new DatabaseContext();
            try
            {
                OriginalRecords = databaseContext.NetworkSettings
                                            .Include(a => a.Equipment)
                                            .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            List<Equipment> equipment = OriginalRecords
                .Where(u => u != null && u.Equipment != null)
                .Select(a => a.Equipment)
                .Distinct()
                .ToList();
            equipment.Insert(0, new Equipment { Name = "Отсутствует", EquipmentID = -1 });

            EquipmentsCB.ItemsSource = equipment;
            EquipmentsCB.DisplayMemberPath = "Name";
            EquipmentsCB.SelectedValuePath = "EquipmentID";
            EquipmentsCB.SelectedValue = -1;

            CurrentList = OriginalRecords;
            RefreshPanel();
        }

        /// <summary>
        /// Обновляет панель с сетевыми настройками
        /// </summary>
        private void RefreshPanel()
        {
            ContentPanel.Children.Clear();

            foreach (var setting in CurrentList)
            {
                var item = new ItemNetworkSettings(setting);
                item.RecordDelete += (s, e) =>
                {
                    OriginalRecords.Remove(setting);
                    SortRecord();
                };
                item.RecordUpdate += (s, e) => SortRecord();
                ContentPanel.Children.Add(item);
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке "Добавить новую запись". Открывает страницу для редактирования настроек сети.
        /// </summary>
        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            var editPage = new EditNetworkSettings();
            editPage.RecordSuccess += CreateNewRecordSuccess;
            MainWindow.mainFrame.Navigate(editPage);
        }

        /// <summary>
        /// Обработчик успешного создания новой записи настроек сети. Добавляет запись в список и выполняет сортировку.
        /// </summary>
        private void CreateNewRecordSuccess(object sender, EventArgs e)
        {
            var networkSettings = sender as NetworkSettings;
            if (networkSettings == null)
                return;

            OriginalRecords.Add(networkSettings);
            SortRecord();
        }

        /// <summary>
        /// Обработчик успешного обновления настроек сети
        /// </summary>
        private void UpdateRecordSuccess(object sender, EventArgs e)
        {
            var updatedSettings = sender as NetworkSettings;
            if (updatedSettings == null)
                return;

            var index = OriginalRecords.FindIndex(x => x.NetworkID == updatedSettings.NetworkID);
            if (index != -1)
            {
                OriginalRecords[index] = updatedSettings;
            }
            SortRecord();
        }

        /// <summary>
        /// Выполняет сортировку и фильтрацию списка настроек сети по выбранному оборудованию и поисковому запросу.
        /// </summary>
        private void SortRecord()
        {
            CurrentList = OriginalRecords.ToList();

            int? selectedEquipment = EquipmentsCB.SelectedValue as int?;
            if (selectedEquipment.HasValue && selectedEquipment.Value != -1)
            {
                CurrentList = CurrentList.Where(x => x.EquipmentID == selectedEquipment.Value).ToList();
            }

            string searchQuery = SearchField.Text.Trim();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                CurrentList = CurrentList
                    .Where(x => (x.IPAddress != null && x.IPAddress.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.SubnetMask != null && x.SubnetMask.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.Gateway != null && x.Gateway.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.DNSServers != null && x.DNSServers.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.Equipment != null && x.Equipment.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    .ToList();
            }

            RefreshPanel();
        }

        /// <summary>
        /// Обработчик изменения выбора в комбобоксе для сортировки. Перезапускает сортировку записей.
        /// </summary>
        private void SortCB_Changed(object sender, SelectionChangedEventArgs e)
        {
            SortRecord();
        }

        /// <summary>
        /// Обработчик клика по кнопке "Поиск". Запускает процесс сортировки и фильтрации списка настроек сети.
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SortRecord();
        }
    }
}