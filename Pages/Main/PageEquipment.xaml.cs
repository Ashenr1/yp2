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
    public partial class PageEquipment : Page
    {
        List<Equipment> OriginalRecords = new List<Equipment>();
        List<Equipment> CurrentList = new List<Equipment>();

        public PageEquipment()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using var databaseContext = new DatabaseContext();
            try
            {
                OriginalRecords = databaseContext.Equipment
                    .Include(e => e.ResponsibleUser)
                    .Include(e => e.TempResponsibleUser)
                    .Include(e => e.Direction)
                    .Include(e => e.Status)
                    .Include(e => e.Model)
                    .Include(e => e.Audience)
                    .Include(e => e.TypeEquipment)
                    .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            // Заполнение фильтров
            var users = OriginalRecords.SelectMany(e => new[] { e.ResponsibleUser, e.TempResponsibleUser }).Where(u => u != null).Distinct().ToList();
            var models = OriginalRecords.Select(e => e.Model).Where(m => m != null).Distinct().ToList();
            var statuses = OriginalRecords.Select(e => e.Status).Where(s => s != null).Distinct().ToList();
            var directions = OriginalRecords.Select(e => e.Direction).Where(d => d != null).Distinct().ToList();
            var audiences = OriginalRecords.Select(e => e.Audience).Where(a => a != null).Distinct().ToList();
            var types = OriginalRecords.Select(e => e.TypeEquipment).Where(t => t != null).Distinct().ToList();

            users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });
            models.Insert(0, new EquipmentModels { ModelID = -1, Name = "Отсутствует" });
            statuses.Insert(0, new Statuses { StatusID = -1, Name = "Отсутствует" });
            directions.Insert(0, new Directions { DirectionID = -1, Name = "Отсутствует" });
            audiences.Insert(0, new Audiences { AudienceID = -1, Name = "Отсутствует" });
            types.Insert(0, new TypesEquipment { TypeEquipmentID = -1, Name = "Отсутствует" });

            ResponsibleUserCB.ItemsSource = users;
            TempResponsibleUserCB.ItemsSource = users;
            EquipmentModelsCB.ItemsSource = models;
            EquipmentStatusesCB.ItemsSource = statuses;
            EquipmentDirectionsCB.ItemsSource = directions;
            EquipmentAudiencesCB.ItemsSource = audiences;
            TypesEquipmentCB.ItemsSource = types;

            ResponsibleUserCB.DisplayMemberPath = "FullName";
            ResponsibleUserCB.SelectedValuePath = "UserID";
            TempResponsibleUserCB.DisplayMemberPath = "FullName";
            TempResponsibleUserCB.SelectedValuePath = "UserID";
            EquipmentModelsCB.DisplayMemberPath = "Name";
            EquipmentModelsCB.SelectedValuePath = "ModelID";
            EquipmentStatusesCB.DisplayMemberPath = "Name";
            EquipmentStatusesCB.SelectedValuePath = "StatusID";
            EquipmentDirectionsCB.DisplayMemberPath = "Name";
            EquipmentDirectionsCB.SelectedValuePath = "DirectionID";
            EquipmentAudiencesCB.DisplayMemberPath = "Name";
            EquipmentAudiencesCB.SelectedValuePath = "AudienceID";
            TypesEquipmentCB.DisplayMemberPath = "Name";
            TypesEquipmentCB.SelectedValuePath = "TypeEquipmentID";

            ResponsibleUserCB.SelectedValue = -1;
            TempResponsibleUserCB.SelectedValue = -1;
            EquipmentModelsCB.SelectedValue = -1;
            EquipmentStatusesCB.SelectedValue = -1;
            EquipmentDirectionsCB.SelectedValue = -1;
            EquipmentAudiencesCB.SelectedValue = -1;
            TypesEquipmentCB.SelectedValue = -1;

            CurrentList = OriginalRecords.ToList();
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            ContentPanel.Children.Clear();
            foreach (var equipment in CurrentList)
            {
                var item = new ItemEquipment(equipment);
                item.RecordDelete += (s, e) =>
                {
                    OriginalRecords.Remove(equipment);
                    SortRecord();
                };
                item.RecordUpdate += (s, e) => SortRecord();
                ContentPanel.Children.Add(item);
            }
        }

        /// <summary>
        /// ПРИНУДИТЕЛЬНО ОБНОВЛЯЕТ ДАННЫЕ ИЗ БД
        /// </summary>
        public void RefreshDataFromDB()
        {
            LoadData();
        }

        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            var editPage = new EditEquipment();
            editPage.RecordSuccess += (s, ev) => RefreshAllData();
            MainWindow.mainFrame.Navigate(editPage);
        }

        private void SortRecord()
        {
            CurrentList = OriginalRecords.ToList();

            int? selectedResponsible = ResponsibleUserCB.SelectedValue as int?;
            if (selectedResponsible.HasValue && selectedResponsible.Value != -1)
                CurrentList = CurrentList.Where(x => x.ResponsibleUserID == selectedResponsible.Value).ToList();

            int? selectedTempResponsible = TempResponsibleUserCB.SelectedValue as int?;
            if (selectedTempResponsible.HasValue && selectedTempResponsible.Value != -1)
                CurrentList = CurrentList.Where(x => x.TempResponsibleUserID == selectedTempResponsible.Value).ToList();

            int? selectedModel = EquipmentModelsCB.SelectedValue as int?;
            if (selectedModel.HasValue && selectedModel.Value != -1)
                CurrentList = CurrentList.Where(x => x.ModelID == selectedModel.Value).ToList();

            int? selectedStatus = EquipmentStatusesCB.SelectedValue as int?;
            if (selectedStatus.HasValue && selectedStatus.Value != -1)
                CurrentList = CurrentList.Where(x => x.StatusID == selectedStatus.Value).ToList();

            int? selectedDirection = EquipmentDirectionsCB.SelectedValue as int?;
            if (selectedDirection.HasValue && selectedDirection.Value != -1)
                CurrentList = CurrentList.Where(x => x.DirectionID == selectedDirection.Value).ToList();

            int? selectedAudience = EquipmentAudiencesCB.SelectedValue as int?;
            if (selectedAudience.HasValue && selectedAudience.Value != -1)
                CurrentList = CurrentList.Where(x => x.AudienceID == selectedAudience.Value).ToList();

            int? selectedType = TypesEquipmentCB.SelectedValue as int?;
            if (selectedType.HasValue && selectedType.Value != -1)
                CurrentList = CurrentList.Where(x => x.TypeEquipmentID == selectedType.Value).ToList();

            string searchQuery = SearchField.Text.Trim();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                CurrentList = CurrentList.Where(x =>
                    (x.Name != null && x.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.InventoryNumber != null && x.InventoryNumber.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.ResponsibleUser != null && x.ResponsibleUser.FullName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.TempResponsibleUser != null && x.TempResponsibleUser.FullName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.Direction != null && x.Direction.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.Status != null && x.Status.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.Model != null && x.Model.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.Audience != null && x.Audience.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (x.TypeEquipment != null && x.TypeEquipment.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(x.Comment) && x.Comment.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0)
                ).ToList();
            }

            RefreshPanel();
        }
        /// <summary>
        /// Полностью перезагружает все данные из базы и обновляет экран
        /// </summary>
        public void RefreshAllData()
        {
            // Очищаем старые данные
            OriginalRecords.Clear();
            CurrentList.Clear();

            // Загружаем свежие данные из базы
            using var databaseContext = new DatabaseContext();
            try
            {
                var freshData = databaseContext.Equipment
                    .Include(e => e.ResponsibleUser)
                    .Include(e => e.TempResponsibleUser)
                    .Include(e => e.Direction)
                    .Include(e => e.Status)
                    .Include(e => e.Model)
                    .Include(e => e.Audience)
                    .Include(e => e.TypeEquipment)
                    .ToList();

                OriginalRecords = freshData;
                CurrentList = freshData;
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            // Обновляем фильтры
            UpdateFilters();

            // Перерисовываем панель
            RefreshPanel();
        }

        /// <summary>
        /// Обновляет выпадающие списки фильтров
        /// </summary>
        private void UpdateFilters()
        {
            var users = OriginalRecords.SelectMany(e => new[] { e.ResponsibleUser, e.TempResponsibleUser }).Where(u => u != null).Distinct().ToList();
            var models = OriginalRecords.Select(e => e.Model).Where(m => m != null).Distinct().ToList();
            var statuses = OriginalRecords.Select(e => e.Status).Where(s => s != null).Distinct().ToList();
            var directions = OriginalRecords.Select(e => e.Direction).Where(d => d != null).Distinct().ToList();
            var audiences = OriginalRecords.Select(e => e.Audience).Where(a => a != null).Distinct().ToList();
            var types = OriginalRecords.Select(e => e.TypeEquipment).Where(t => t != null).Distinct().ToList();

            users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });
            models.Insert(0, new EquipmentModels { ModelID = -1, Name = "Отсутствует" });
            statuses.Insert(0, new Statuses { StatusID = -1, Name = "Отсутствует" });
            directions.Insert(0, new Directions { DirectionID = -1, Name = "Отсутствует" });
            audiences.Insert(0, new Audiences { AudienceID = -1, Name = "Отсутствует" });
            types.Insert(0, new TypesEquipment { TypeEquipmentID = -1, Name = "Отсутствует" });

            // Сохраняем выбранные значения перед обновлением
            var savedResponsible = ResponsibleUserCB.SelectedValue;
            var savedTempResponsible = TempResponsibleUserCB.SelectedValue;
            var savedModel = EquipmentModelsCB.SelectedValue;
            var savedStatus = EquipmentStatusesCB.SelectedValue;
            var savedDirection = EquipmentDirectionsCB.SelectedValue;
            var savedAudience = EquipmentAudiencesCB.SelectedValue;
            var savedType = TypesEquipmentCB.SelectedValue;

            ResponsibleUserCB.ItemsSource = users;
            TempResponsibleUserCB.ItemsSource = users;
            EquipmentModelsCB.ItemsSource = models;
            EquipmentStatusesCB.ItemsSource = statuses;
            EquipmentDirectionsCB.ItemsSource = directions;
            EquipmentAudiencesCB.ItemsSource = audiences;
            TypesEquipmentCB.ItemsSource = types;

            // Восстанавливаем выбранные значения
            ResponsibleUserCB.SelectedValue = savedResponsible;
            TempResponsibleUserCB.SelectedValue = savedTempResponsible;
            EquipmentModelsCB.SelectedValue = savedModel;
            EquipmentStatusesCB.SelectedValue = savedStatus;
            EquipmentDirectionsCB.SelectedValue = savedDirection;
            EquipmentAudiencesCB.SelectedValue = savedAudience;
            TypesEquipmentCB.SelectedValue = savedType;
        }
        private void SortCB_Changed(object sender, SelectionChangedEventArgs e) => SortRecord();
        private void Search_Click(object sender, RoutedEventArgs e) => SortRecord();
    }
}