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

namespace UP02.Pages.Main
{
    /// <summary>
    /// Логика взаимодействия для PageEquipmentConsumables.xaml
    /// </summary>
    public partial class PageEquipmentConsumables : Page
    {
        int? CurrentEquipmentID = null;
        public event EventHandler RecordDelete;
        public event EventHandler RecordSuccess;
        List<Equipment> Equipments = new List<Equipment>();
        List<Consumables> Consumables = new List<Consumables>();
        List<Consumables> CurrentConsumables = new List<Consumables>();
        List<Consumables> OriginalListConsumables = new List<Consumables>();

        /// <summary>
        /// Конструктор страницы, инициализирует компоненты и загружает данные об оборудовании и расходных материалах из базы данных.
        /// </summary>
        public PageEquipmentConsumables()
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
                Equipments = databaseContext.Equipment.ToList();
                Consumables = databaseContext.Consumables
                    .Include(a => a.TempResponsibleUser)
                    .Include(a => a.TypeConsumables)
                    .ToList();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }

            EquipmentsComboBox.ItemsSource = Equipments;
            ConsumablesComboBox.ItemsSource = Consumables;

            EquipmentsComboBox.DisplayMemberPath = "Name";
            ConsumablesComboBox.DisplayMemberPath = "Name";

            EquipmentsComboBox.SelectedValuePath = "EquipmentID";
            ConsumablesComboBox.SelectedValuePath = "ConsumableID";
        }

        /// <summary>
        /// Обработчик изменения выбранного оборудования в комбинированном списке. Обновляет данные о расходных материалах.
        /// </summary>
        private void EquipmentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateData();

        /// <summary>
        /// Обновляет данные о расходных материалах, привязывая их к выбранному оборудованию.
        /// </summary>
        void UpdateData()
        {
            ConsumablesParent.Children.Clear();
            var equipment = EquipmentsComboBox.SelectedItem as Equipment;

            CurrentConsumables.Clear();

            if (equipment == null)
            {
                return;
            }

            CurrentEquipmentID = equipment.EquipmentID;

            List<Consumables> consumables;

            try
            {
                using var databaseContext = new DatabaseContext();
                var equipmentConsumables = databaseContext.EquipmentConsumables
                    .Where(x => x.EquipmentID == equipment.EquipmentID)
                    .ToList();

                var consumableIds = equipmentConsumables
                    .Select(ec => ec.ConsumableID)
                    .Distinct()
                    .ToList();

                consumables = databaseContext.Consumables
                    .Where(x => consumableIds.Contains(x.ConsumableID))
                    .Include(a => a.TempResponsibleUser)
                    .Include(a => a.TypeConsumables)
                    .ToList();
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к базе данных. Проверьте соединение и повторите попытку.",
                                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);

                MainWindow.OpenPage(new PageAuthorization());
                return;
            }

            CurrentConsumables = new List<Consumables>(consumables);
            OriginalListConsumables = new List<Consumables>(consumables);

            RefreshPanel();
        }

        /// <summary>
        /// Обновляет панель с расходными материалами
        /// </summary>
        private void RefreshPanel()
        {
            ConsumablesParent.Children.Clear();

            foreach (var consumable in CurrentConsumables)
            {
                var item = new ItemConsumables(consumable, true);
                item.RecordDelete += Item_RecordDelete;
                ConsumablesParent.Children.Add(item);
            }
        }

        /// <summary>
        /// Обработчик изменения выбранного расходного материала в комбинированном списке.
        /// Добавляет выбранный расходный материал в список и отображает его на экране.
        /// </summary>
        private void ConsumablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EquipmentsComboBox.SelectedItem != null)
            {
                var consumable = ConsumablesComboBox.SelectedItem as Consumables;
                if (consumable != null && !CurrentConsumables.Any(c => c.ConsumableID == consumable.ConsumableID))
                {
                    CurrentConsumables.Add(consumable);
                    RefreshPanel();
                }
            }

            ConsumablesComboBox.SelectionChanged -= ConsumablesComboBox_SelectionChanged;
            ConsumablesComboBox.SelectedItem = null;
            ConsumablesComboBox.SelectionChanged += ConsumablesComboBox_SelectionChanged;
        }

        /// <summary>
        /// Обработчик клика по кнопке "Сохранить изменения". Сохраняет изменения в базе данных по добавленным и удаленным расходным материалам.
        /// </summary>
        private void SaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (!CurrentEquipmentID.HasValue)
            {
                MessageBox.Show("Выберите оборудование.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var consumableDelete = OriginalListConsumables
                .Where(x => !CurrentConsumables.Any(f => f.ConsumableID == x.ConsumableID))
                .ToList();

            var consumableCreate = CurrentConsumables
                .Where(x => !OriginalListConsumables.Any(f => f.ConsumableID == x.ConsumableID))
                .ToList();

            var consumableEquipmentCreate = consumableCreate
                .Select(item => new EquipmentConsumables()
                {
                    ConsumableID = item.ConsumableID,
                    EquipmentID = CurrentEquipmentID.Value
                })
                .ToList();

            try
            {
                using var databaseContext = new DatabaseContext();

                if (consumableEquipmentCreate.Any())
                {
                    databaseContext.EquipmentConsumables.AddRange(consumableEquipmentCreate);
                }

                if (consumableDelete.Any())
                {
                    var consumableIdsToDelete = consumableDelete.Select(f => f.ConsumableID).ToList();
                    var equipmentConsumablesDelete = databaseContext.EquipmentConsumables
                        .Where(x => x.EquipmentID == CurrentEquipmentID && consumableIdsToDelete.Contains(x.ConsumableID));
                    databaseContext.EquipmentConsumables.RemoveRange(equipmentConsumablesDelete);
                }

                databaseContext.SaveChanges();
                MessageBox.Show("Изменения сохранены успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к базе данных. Проверьте соединение и повторите попытку.",
                                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);

                MainWindow.OpenPage(new PageAuthorization());
                return;
            }

            UpdateData();
        }

        /// <summary>
        /// Обработчик клика по кнопке "Отменить изменения". Перезагружает данные о расходных материалах.
        /// </summary>
        private void UndoСhangesClick(object sender, RoutedEventArgs e)
        {
            UpdateData();
        }

        /// <summary>
        /// Обработчик удаления расходного материала из списка. Удаляет элемент из интерфейса и списка текущих расходных материалов.
        /// </summary>
        private void Item_RecordDelete(object sender, EventArgs e)
        {
            if (sender is ItemConsumables item && ConsumablesParent.Children.Contains(item))
            {
                ConsumablesParent.Children.Remove(item);
                CurrentConsumables.Remove(item.Consumable);
            }
        }
    }
}