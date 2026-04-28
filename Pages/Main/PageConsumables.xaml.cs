using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using UP02.Context;
using UP02.Elements;
using UP02.Helpers;
using UP02.Models;
using UP02.Pages.Elements;

namespace UP02.Pages.Main
{
    public partial class PageConsumables : Page
    {
        List<Consumables> OriginalRecords = new List<Consumables>();
        List<Consumables> CurrentList = new List<Consumables>();

        public PageConsumables()
        {
            try
            {
                InitializeComponent();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке страницы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                using var databaseContext = new DatabaseContext();
                OriginalRecords = databaseContext.Consumables
                            .Include(a => a.ResponsibleUser)
                            .Include(a => a.TempResponsibleUser)
                            .Include(a => a.TypeConsumables)
                            .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            try
            {
                // Заполняем фильтры с проверкой на пустые списки
                List<Users> users = new List<Users>();
                List<TypesConsumables> types = new List<TypesConsumables>();

                if (OriginalRecords != null && OriginalRecords.Any())
                {
                    users = OriginalRecords
                        .Where(u => u != null && u.TempResponsibleUser != null)
                        .Select(e => e.TempResponsibleUser)
                        .Distinct()
                        .ToList();

                    types = OriginalRecords
                        .Where(u => u != null && u.TypeConsumables != null)
                        .Select(e => e.TypeConsumables)
                        .Distinct()
                        .ToList();
                }

                // Добавляем "Отсутствует" в начало списков
                users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });
                types.Insert(0, new TypesConsumables { TypeConsumablesID = -1, Type = "Отсутствует" });

                ResponsibleUserCB.ItemsSource = users;
                TempResponsibleUserCB.ItemsSource = users;
                ResponsibleUserCB.DisplayMemberPath = "FullName";
                TempResponsibleUserCB.DisplayMemberPath = "FullName";
                ResponsibleUserCB.SelectedValuePath = "UserID";
                TempResponsibleUserCB.SelectedValuePath = "UserID";

                TypeConsumablesCB.ItemsSource = types;
                TypeConsumablesCB.DisplayMemberPath = "Type";
                TypeConsumablesCB.SelectedValuePath = "TypeConsumablesID";

                ResponsibleUserCB.SelectedValue = -1;
                TempResponsibleUserCB.SelectedValue = -1;
                TypeConsumablesCB.SelectedValue = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CurrentList = OriginalRecords?.ToList() ?? new List<Consumables>();
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            try
            {
                ContentPanel.Children.Clear();

                if (CurrentList == null) return;

                foreach (var consumable in CurrentList)
                {
                    var item = new ItemConsumables(consumable);
                    item.RecordDelete += (s, e) =>
                    {
                        OriginalRecords.Remove(consumable);
                        SortRecord();
                    };
                    item.RecordUpdate += (s, e) => SortRecord();
                    ContentPanel.Children.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении панели: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewRecord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editPage = new EditConsumables();
                editPage.RecordSuccess += CreateNewRecordSuccess;
                MainWindow.mainFrame.Navigate(editPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNewRecordSuccess(object sender, EventArgs e)
        {
            try
            {
                var consumable = sender as Consumables;
                if (consumable == null) return;

                OriginalRecords.Add(consumable);
                SortRecord();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRecordSuccess(object sender, EventArgs e)
        {
            try
            {
                var consumable = sender as Consumables;
                if (consumable == null) return;

                var index = OriginalRecords.FindIndex(x => x.ConsumableID == consumable.ConsumableID);
                if (index != -1)
                {
                    OriginalRecords[index] = consumable;
                }
                SortRecord();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SortRecord()
        {
            try
            {
                CurrentList = OriginalRecords?.ToList() ?? new List<Consumables>();

                int? selectedTempResponsible = TempResponsibleUserCB.SelectedValue as int?;
                if (selectedTempResponsible.HasValue && selectedTempResponsible.Value != -1)
                {
                    CurrentList = CurrentList.Where(x => x.TempResponsibleUserID == selectedTempResponsible.Value).ToList();
                }

                int? selectedResponsible = ResponsibleUserCB.SelectedValue as int?;
                if (selectedResponsible.HasValue && selectedResponsible.Value != -1)
                {
                    CurrentList = CurrentList.Where(x => x.ResponsibleUserID == selectedResponsible.Value).ToList();
                }

                int? selectedTypeConsumablesCB = TypeConsumablesCB.SelectedValue as int?;
                if (selectedTypeConsumablesCB.HasValue && selectedTypeConsumablesCB.Value != -1)
                {
                    CurrentList = CurrentList.Where(x => x.TypeConsumablesID == selectedTypeConsumablesCB.Value).ToList();
                }

                string searchQuery = SearchField.Text.Trim();
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    CurrentList = CurrentList
                        .Where(x => (x.Name != null && x.Name.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                    (x.Description != null && x.Description.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                    (x.TempResponsibleUser != null && x.TempResponsibleUser.FullName.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                                    (x.TypeConsumables != null && x.TypeConsumables.Type.IndexOf(searchQuery, StringComparison.CurrentCultureIgnoreCase) >= 0))
                        .ToList();
                }

                if (AfterReceiptDate.SelectedDate.HasValue)
                {
                    DateTime afterReceiptDate = AfterReceiptDate.SelectedDate.Value.Date;
                    CurrentList = CurrentList.Where(x => x.ReceiptDate.HasValue && x.ReceiptDate.Value.Date >= afterReceiptDate).ToList();
                }

                if (BeforeReceiptDate.SelectedDate.HasValue)
                {
                    DateTime beforeReceiptDate = BeforeReceiptDate.SelectedDate.Value.Date;
                    CurrentList = CurrentList
                        .Where(x => x.ReceiptDate.HasValue && x.ReceiptDate.Value.Date <= beforeReceiptDate).ToList();
                }

                RefreshPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SortCB_Changed(object sender, SelectionChangedEventArgs e) => SortRecord();
        private void Search_Click(object sender, RoutedEventArgs e) => SortRecord();
        private void AfterReceiptDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => SortRecord();
        private void AfterReceiptDate_TextInput(object sender, TextCompositionEventArgs e) => SortRecord();
        private void AfterReceiptDate_LostFocus(object sender, RoutedEventArgs e) => SortRecord();
        private void BeforeReceiptDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => SortRecord();
        private void BeforeReceiptDate_TextInput(object sender, TextCompositionEventArgs e) => SortRecord();
        private void BeforeReceiptDate_LostFocus(object sender, RoutedEventArgs e) => SortRecord();
    }
}