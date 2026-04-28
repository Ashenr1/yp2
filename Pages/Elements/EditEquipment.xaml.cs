using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using UP02.Context;
using UP02.Helpers;
using UP02.Interfaces;
using UP02.Models;

namespace UP02.Pages.Elements
{
    public partial class EditEquipment : Page, IRecordSuccess
    {
        int? EquipmentID = null;
        public event EventHandler RecordDelete;
        public event EventHandler RecordSuccess;
        int? CurrentAudiences = null;
        int? CurrentResponsibleUser = null;
        byte[]? imageBytes;

        public EditEquipment(Equipment equipment = null)
        {
            InitializeComponent();

            using var databaseContext = new DatabaseContext();
            try
            {
                var users = databaseContext.Users.ToList();
                var statuses = databaseContext.Statuses.ToList();
                var directions = databaseContext.Directions.ToList();
                var models = databaseContext.EquipmentModels.ToList();
                var audiences = databaseContext.Audiences.ToList();
                var types = databaseContext.TypesEquipment.ToList();

                users.Insert(0, new Users { UserID = -1, LastName = "", FirstName = "Отсутствует", MiddleName = "" });
                statuses.Insert(0, new Statuses { StatusID = -1, Name = "Отсутствует" });
                directions.Insert(0, new Directions { DirectionID = -1, Name = "Отсутствует" });
                models.Insert(0, new EquipmentModels { ModelID = -1, Name = "Отсутствует" });
                audiences.Insert(0, new Audiences { AudienceID = -1, Name = "Отсутствует" });
                types.Insert(0, new TypesEquipment { TypeEquipmentID = -1, Name = "Отсутствует" });

                ResponsibleUserCB.ItemsSource = users;
                TempResponsibleUserCB.ItemsSource = users;
                StatusCB.ItemsSource = statuses;
                DirectionCB.ItemsSource = directions;
                ModelCB.ItemsSource = models;
                AudienceCB.ItemsSource = audiences;
                TypesEquipmentCB.ItemsSource = types;

                ResponsibleUserCB.DisplayMemberPath = "FullName";
                ResponsibleUserCB.SelectedValuePath = "UserID";
                TempResponsibleUserCB.DisplayMemberPath = "FullName";
                TempResponsibleUserCB.SelectedValuePath = "UserID";
                StatusCB.DisplayMemberPath = "Name";
                StatusCB.SelectedValuePath = "StatusID";
                DirectionCB.DisplayMemberPath = "Name";
                DirectionCB.SelectedValuePath = "DirectionID";
                ModelCB.DisplayMemberPath = "Name";
                ModelCB.SelectedValuePath = "ModelID";
                AudienceCB.DisplayMemberPath = "Name";
                AudienceCB.SelectedValuePath = "AudienceID";
                TypesEquipmentCB.DisplayMemberPath = "Name";
                TypesEquipmentCB.SelectedValuePath = "TypeEquipmentID";

                ResponsibleUserCB.SelectedValue = -1;
                TempResponsibleUserCB.SelectedValue = -1;
                DirectionCB.SelectedValue = -1;
                StatusCB.SelectedValue = -1;
                ModelCB.SelectedValue = -1;
                AudienceCB.SelectedValue = -1;
                TypesEquipmentCB.SelectedValue = -1;

                if (equipment != null)
                {
                    EquipmentID = equipment.EquipmentID;
                    TextBoxName.Text = equipment.Name;
                    TextBoxComment.Text = equipment.Comment;
                    TextBoxInventoryNumber.Text = equipment.InventoryNumber;
                    TextBoxCost.Text = equipment.Cost?.ToString() ?? "";
                    imageBytes = equipment.Photo;

                    ResponsibleUserCB.SelectedValue = equipment.ResponsibleUserID ?? -1;
                    TempResponsibleUserCB.SelectedValue = equipment.TempResponsibleUserID ?? -1;
                    DirectionCB.SelectedValue = equipment.DirectionID ?? -1;
                    StatusCB.SelectedValue = equipment.StatusID ?? -1;
                    ModelCB.SelectedValue = equipment.ModelID ?? -1;
                    TypesEquipmentCB.SelectedValue = equipment.TypeEquipmentID ?? -1;
                    AudienceCB.SelectedValue = equipment.AudienceID ?? -1;

                    CurrentAudiences = equipment.AudienceID;
                    CurrentResponsibleUser = equipment.ResponsibleUserID;
                }
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }
        }

        private bool ValidateAllFields()
        {
            bool incorrect = false;

            incorrect |= UIHelper.ValidateField(TextBoxName.Text, 255, "Название", isRequired: true);
            incorrect |= UIHelper.ValidateField(TextBoxInventoryNumber.Text, 50, "Инвентарный номер", regexPattern: "^[0-9]+$", isRequired: true);

            if (!string.IsNullOrWhiteSpace(TextBoxCost.Text) && !decimal.TryParse(TextBoxCost.Text, out _))
            {
                MessageBox.Show("Поле \"Стоимость\" должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                incorrect = true;
            }

            var responsibleId = ResponsibleUserCB.SelectedValue as int?;
            var tempId = TempResponsibleUserCB.SelectedValue as int?;
            if ((!responsibleId.HasValue || responsibleId.Value == -1) && (!tempId.HasValue || tempId.Value == -1))
            {
                MessageBox.Show("Не выбран ни один ответственный пользователь.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                incorrect = true;
            }

            return incorrect;
        }

        private int? GetSelectedId(ComboBox comboBox)
        {
            int? id = comboBox.SelectedValue as int?;
            return (id.HasValue && id.Value != -1) ? id.Value : (int?)null;
        }

        // ЭТОТ МЕТОД ДОЛЖЕН БЫТЬ!!!
        private void SaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (ValidateAllFields())
                return;

            using var databaseContext = new DatabaseContext();
            try
            {
                Equipment equipmentToSave = null;

                if (EquipmentID.HasValue)
                {
                    equipmentToSave = databaseContext.Equipment.FirstOrDefault(eq => eq.EquipmentID == EquipmentID.Value);
                }

                if (equipmentToSave == null && EquipmentID.HasValue)
                {
                    MessageBox.Show(
                        "Запись в базе данных не найдена.",
                        "Запись не найдена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    RecordDelete?.Invoke(null, EventArgs.Empty);
                    return;
                }

                if (equipmentToSave == null)
                {
                    equipmentToSave = new Equipment();
                }

                equipmentToSave.Name = TextBoxName.Text;
                equipmentToSave.Comment = TextBoxComment.Text;
                equipmentToSave.InventoryNumber = TextBoxInventoryNumber.Text;
                equipmentToSave.Cost = decimal.TryParse(TextBoxCost.Text, out decimal cost) ? cost : null;
                equipmentToSave.Photo = imageBytes;

                equipmentToSave.ResponsibleUserID = GetSelectedId(ResponsibleUserCB);
                equipmentToSave.TempResponsibleUserID = GetSelectedId(TempResponsibleUserCB);
                equipmentToSave.DirectionID = GetSelectedId(DirectionCB);
                equipmentToSave.StatusID = GetSelectedId(StatusCB);
                equipmentToSave.ModelID = GetSelectedId(ModelCB);
                equipmentToSave.AudienceID = GetSelectedId(AudienceCB);
                equipmentToSave.TypeEquipmentID = GetSelectedId(TypesEquipmentCB);

                if (!EquipmentID.HasValue)
                {
                    databaseContext.Equipment.Add(equipmentToSave);
                }

                databaseContext.SaveChanges();

                RecordSuccess?.Invoke(equipmentToSave, EventArgs.Empty);

                MainWindow.mainFrame.GoBack();
            }
            catch (Exception ex)
            {
                UIHelper.ErrorConnection(ex.Message);
                return;
            }
        }

        private void UndoСhangesClick(object sender, RoutedEventArgs e)
        {
            MainWindow.mainFrame.GoBack();
        }

        private void SelectAndSaveImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                imageBytes = File.ReadAllBytes(openFileDialog.FileName);
            }
        }
    }
}