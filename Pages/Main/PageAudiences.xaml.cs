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
    /// Логика взаимодействия для PageAudiences.xaml
    /// </summary>
    public partial class PageAudiences : Page
    {
        /// <summary>
        /// Оригинальный список всех аудиторий
        /// </summary>
        List<Audiences> OriginalRecords = new List<Audiences>();

        /// <summary>
        /// Текущий отфильтрованный список аудиторий
        /// </summary>
        List<Audiences> CurrentList = new List<Audiences>();

        /// <summary>
        /// Инициализирует новый экземпляр страницы аудиторий и загружает данные
        /// </summary>
        public PageAudiences()
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
            List<Users> users;

            try
            {
                OriginalRecords = databaseContext.Audiences
                    .Include(a => a.ResponsibleUser)
                    .Include(a => a.TempResponsibleUser)
                    .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            users = OriginalRecords
                .SelectMany(a => new[] { a.ResponsibleUser, a.TempResponsibleUser })
                .Where(u => u != null)
                .Distinct()
                .ToList();

            users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });

            ResponsibleUserCB.ItemsSource = users;
            TempResponsibleUserCB.ItemsSource = users;

            ResponsibleUserCB.DisplayMemberPath = "FullName";
            ResponsibleUserCB.SelectedValuePath = "UserID";
            TempResponsibleUserCB.DisplayMemberPath = "FullName";
            TempResponsibleUserCB.SelectedValuePath = "UserID";

            ResponsibleUserCB.SelectedValue = -1;
            TempResponsibleUserCB.SelectedValue = -1;

            CurrentList = OriginalRecords;
            RefreshPanel();
        }

        /// <summary>
        /// Обновляет панель с аудиториями
        /// </summary>
        private void RefreshPanel()
        {
            ContentPanel.Children.Clear();

            foreach (var audience in CurrentList)
            {
                var item = new ItemAudiences(audience);
                item.RecordDelete += (s, e) =>
                {
                    OriginalRecords.Remove(audience);
                    SortRecord();
                };
                item.RecordUpdate += (s, e) => SortRecord();
                ContentPanel.Children.Add(item);
            }
        }

        public void RefreshAllData()
        {
            OriginalRecords.Clear();
            CurrentList.Clear();

            using var databaseContext = new DatabaseContext();
            try
            {
                var freshData = databaseContext.Audiences
                    .Include(a => a.ResponsibleUser)
                    .Include(a => a.TempResponsibleUser)
                    .ToList();

                OriginalRecords = freshData;
                CurrentList = freshData;
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            UpdateFilters();
            RefreshPanel();
        }

        private void UpdateFilters()
        {
            var users = OriginalRecords
                .SelectMany(a => new[] { a.ResponsibleUser, a.TempResponsibleUser })
                .Where(u => u != null)
                .Distinct()
                .ToList();

            users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });

            var savedResponsible = ResponsibleUserCB.SelectedValue;
            var savedTempResponsible = TempResponsibleUserCB.SelectedValue;

            ResponsibleUserCB.ItemsSource = users;
            TempResponsibleUserCB.ItemsSource = users;

            ResponsibleUserCB.SelectedValue = savedResponsible;
            TempResponsibleUserCB.SelectedValue = savedTempResponsible;
        }

        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            var editPage = new EditAudiences();
            editPage.RecordSuccess += (s, ev) => RefreshAllData();
            MainWindow.mainFrame.Navigate(editPage);
        }


        /// <summary>
        /// Обрабатывает успешное создание новой аудитории
        /// </summary>
        private void CreateNewRecordSuccess(object sender, EventArgs e)
        {
            var audience = sender as Audiences;
            if (audience == null)
                return;

            OriginalRecords.Add(audience);
            SortRecord();
        }

        /// <summary>
        /// Обрабатывает успешное обновление аудитории
        /// </summary>
        private void UpdateRecordSuccess(object sender, EventArgs e)
        {
            var updatedAudience = sender as Audiences;
            if (updatedAudience == null)
                return;

            var index = OriginalRecords.FindIndex(x => x.AudienceID == updatedAudience.AudienceID);
            if (index != -1)
            {
                OriginalRecords[index] = updatedAudience;
            }
            SortRecord();
        }

        /// <summary>
        /// Выполняет сортировку и фильтрацию списка аудиторий
        /// </summary>
        private void SortRecord()
        {
            CurrentList = OriginalRecords.ToList();

            int? selectedResponsible = ResponsibleUserCB.SelectedValue as int?;
            if (selectedResponsible.HasValue && selectedResponsible.Value != -1)
            {
                CurrentList = CurrentList.Where(x => x.ResponsibleUserID == selectedResponsible.Value).ToList();
            }

            int? selectedTempResponsible = TempResponsibleUserCB.SelectedValue as int?;
            if (selectedTempResponsible.HasValue && selectedTempResponsible.Value != -1)
            {
                CurrentList = CurrentList.Where(x => x.TempResponsibleUserID == selectedTempResponsible.Value).ToList();
            }

            string searchQuery = SearchField.Text.Trim();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                CurrentList = CurrentList
                    .Where(x => (x.Name != null && x.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.ShortName != null && x.ShortName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.ResponsibleUser != null && x.ResponsibleUser.FullName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                (x.TempResponsibleUser != null && x.TempResponsibleUser.FullName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    .ToList();
            }

            RefreshPanel();
        }
        public void RefreshDataFromDB()
        {
            LoadData();
        }
        /// <summary>
        /// Обрабатывает изменение выбора в комбобоксах фильтрации
        /// </summary>
        private void SortCB_Changed(object sender, SelectionChangedEventArgs e)
        {
            SortRecord();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки поиска
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SortRecord();
        }
    }
}