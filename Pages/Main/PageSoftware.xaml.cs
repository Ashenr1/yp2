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
    /// Логика взаимодействия для PageSoftware.xaml
    /// </summary>
    public partial class PageSoftware : Page
    {
        List<Software> OriginalRecords = new List<Software>();
        List<Software> CurrentList = new List<Software>();

        /// <summary>
        /// Конструктор страницы, инициализирует компоненты и загружает данные
        /// </summary>
        public PageSoftware()
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
                OriginalRecords = databaseContext.Software
                    .Include(s => s.Developer)
                    .Include(s => s.Equipment)
                    .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            // Заполняем фильтры
            var developers = OriginalRecords
                .Where(s => s.Developer != null)
                .Select(s => s.Developer)
                .Distinct()
                .ToList();
            developers.Insert(0, new SoftwareDevelopers { DeveloperID = -1, Name = "Отсутствует" });

            var equipments = OriginalRecords
                .Where(s => s.Equipment != null)
                .Select(s => s.Equipment)
                .Distinct()
                .ToList();
            equipments.Insert(0, new Equipment { EquipmentID = -1, Name = "Отсутствует" });

            DevelopersCB.ItemsSource = developers;
            DevelopersCB.DisplayMemberPath = "Name";
            DevelopersCB.SelectedValuePath = "DeveloperID";

            EquipmentsCB.ItemsSource = equipments;
            EquipmentsCB.DisplayMemberPath = "Name";
            EquipmentsCB.SelectedValuePath = "EquipmentID";

            DevelopersCB.SelectedValue = -1;
            EquipmentsCB.SelectedValue = -1;

            CurrentList = OriginalRecords;
            RefreshPanel();
        }

        /// <summary>
        /// Обновляет панель с ПО
        /// </summary>
        private void RefreshPanel()
        {
            ContentPanel.Children.Clear();

            foreach (var software in CurrentList)
            {
                var item = new ItemSoftware(software);
                item.RecordDelete += (s, e) =>
                {
                    OriginalRecords.Remove(software);
                    SortRecord();
                };
                item.RecordUpdate += (s, e) => SortRecord();
                ContentPanel.Children.Add(item);
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке добавления нового ПО
        /// </summary>
        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            var editPage = new EditSoftware();
            editPage.RecordSuccess += CreateNewRecordSuccess;
            MainWindow.mainFrame.Navigate(editPage);
        }

        /// <summary>
        /// Обработчик успешного создания нового ПО
        /// </summary>
        private void CreateNewRecordSuccess(object sender, EventArgs e)
        {
            var software = sender as Software;
            if (software == null)
                return;

            OriginalRecords.Add(software);
            SortRecord();
        }

        /// <summary>
        /// Обработчик успешного обновления ПО
        /// </summary>
        private void UpdateRecordSuccess(object sender, EventArgs e)
        {
            var updatedSoftware = sender as Software;
            if (updatedSoftware == null)
                return;

            var index = OriginalRecords.FindIndex(x => x.SoftwareID == updatedSoftware.SoftwareID);
            if (index != -1)
            {
                OriginalRecords[index] = updatedSoftware;
            }
            SortRecord();
        }

        /// <summary>
        /// Выполняет сортировку и фильтрацию списка ПО
        /// </summary>
        private void SortRecord()
        {
            CurrentList = OriginalRecords.ToList();

            int? selectedDeveloper = DevelopersCB.SelectedValue as int?;
            if (selectedDeveloper.HasValue && selectedDeveloper.Value != -1)
            {
                CurrentList = CurrentList.Where(x => x.DeveloperID == selectedDeveloper.Value).ToList();
            }

            int? selectedEquipment = EquipmentsCB.SelectedValue as int?;
            if (selectedEquipment.HasValue && selectedEquipment.Value != -1)
            {
                CurrentList = CurrentList.Where(x => x.EquipmentID == selectedEquipment.Value).ToList();
            }

            string searchQuery = SearchField.Text.Trim();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                CurrentList = CurrentList
                    .Where(x => (x.Name != null && x.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.Version != null && x.Version.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.Developer != null && x.Developer.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.Equipment != null && x.Equipment.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    .ToList();
            }

            RefreshPanel();
        }

        /// <summary>
        /// Обработчик изменения выбора в комбобоксах фильтрации
        /// </summary>
        private void SortCB_Changed(object sender, SelectionChangedEventArgs e)
        {
            SortRecord();
        }

        /// <summary>
        /// Обработчик нажатия кнопки поиска
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SortRecord();
        }
    }
}