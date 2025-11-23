using Microsoft.Win32;
using Proyecto__2_Datos_Arbol_Genealogico.Models;
using Proyecto__2_Datos_Arbol_Genealogico.Utils;
using Proyecto__2_Datos_Arbol_Genealogico.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Proyecto__2_Datos_Arbol_Genealogico
{
    public partial class MainWindow : Window
    {
        private readonly FamilyTree familyTree = new FamilyTree();
        private BitmapImage loadedPhoto = null;
        private Node<Person> _selected;
        private bool editMode = false;

        public MainWindow()
        {
            InitializeComponent();

            familyTree.Changed += () => Dispatcher.Invoke(() =>
            {
                FamilyTreeCtrl.Render(familyTree);
                UpdateStats();
            });

            DpDob.SelectedDate = DateTime.Now.AddYears(-30);

            FamilyTreeCtrl.NodeClicked += OnNodeClicked;

            FamilyTreeCtrl.Render(familyTree);
            UpdateStats();

        }

        private void BtnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                loadedPhoto = Person.LoadImageFromPath(dlg.FileName);
                PreviewPhoto.Source = loadedPhoto;
            }
        }

        private void BtnAddOrUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (editMode)
                UpdateSelectedMember();
            else
                AddNewMember();
        }

        private void AddNewMember()
        {
            try
            {
                var p = new Person
                {
                    FirstName = TxtFirstName.Text.Trim(),
                    LastName = TxtLastName.Text.Trim(),
                    Cedula = TxtCedula.Text.Trim()
                };

                if (!double.TryParse(TxtLat.Text?.Trim(), System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out double lat))
                {
                    MessageBox.Show("Latitud inválida.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(TxtLon.Text?.Trim(), System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out double lon))
                {
                    MessageBox.Show("Longitud inválida.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                p.Latitude = lat;
                p.Longitude = lon;

                if (!DpDob.SelectedDate.HasValue)
                {
                    MessageBox.Show("Seleccione fecha de nacimiento.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                p.DateOfBirth = DpDob.SelectedDate.Value;

                if (DpDod.SelectedDate.HasValue)
                    p.DateOfDeath = DpDod.SelectedDate.Value;

                p.Photo = loadedPhoto;

                var node = familyTree.AddMember(p);
                _selected = node;

                ClearForm();
                RefreshTreeView();
                MessageBox.Show("Miembro agregado.", "Ok", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void UpdateSelectedMember()
        {
            if (_selected == null)
            {
                MessageBox.Show("No hay miembro seleccionado.");
                return;
            }

            var p = _selected.Value;

            p.FirstName = TxtFirstName.Text.Trim();
            p.LastName = TxtLastName.Text.Trim();
            p.Cedula = TxtCedula.Text.Trim();

            if (double.TryParse(TxtLat.Text.Trim(), out double lat))
                p.Latitude = lat;
            if (double.TryParse(TxtLon.Text.Trim(), out double lon))
                p.Longitude = lon;

            if (DpDob.SelectedDate.HasValue)
                p.DateOfBirth = DpDob.SelectedDate.Value;

            p.DateOfDeath = DpDod.SelectedDate;

            if (loadedPhoto != null)
                p.Photo = loadedPhoto;

            familyTree.TriggerChanged();

            ClearForm();
            RefreshTreeView();
            SetNormalMode();

            MessageBox.Show("Información actualizada.");
        }

        private void ClearForm()
        {
            TxtFirstName.Text = "";
            TxtLastName.Text = "";
            TxtCedula.Text = "";
            TxtLat.Text = "";
            TxtLon.Text = "";
            DpDob.SelectedDate = DateTime.Now.AddYears(-30);
            DpDod.SelectedDate = null;
            PreviewPhoto.Source = null;
            loadedPhoto = null;
        }

        private void SetEditMode()
        {
            editMode = true;
            BtnAddOrUpdate.Content = "Actualizar información";
            BtnCancelEdit.Visibility = Visibility.Visible;
        }

        private void SetNormalMode()
        {
            editMode = false;
            BtnAddOrUpdate.Content = "Agregar miembro";
            BtnCancelEdit.Visibility = Visibility.Collapsed;
            _selected = null;
        }
        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            SetNormalMode();
        }

        private void BtnOpenMap_Click(object sender, RoutedEventArgs e)
        {
            var map = new MapWindow(familyTree) { Owner = this };
            map.Show();
        }

        private void RefreshTreeView()
        {
            FamilyTreeCtrl.Render(familyTree);
            UpdateStats();
        }

        private void OnNodeClicked(Node<Person> node)
        {
            _selected = node;
            var p = node.Value;

            // llenar panel derecho
            InfoName.Text = p.FullName;
            InfoCed.Text = p.Cedula;
            InfoAge.Text = p.IsAlive ? $"{p.Age} años" : $"† {p.Age} años";
            InfoPhoto.Source = p.Photo ?? null;

            // llenar formulario de edición
            TxtFirstName.Text = p.FirstName;
            TxtLastName.Text = p.LastName;
            TxtCedula.Text = p.Cedula;
            TxtLat.Text = p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtLon.Text = p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DpDob.SelectedDate = p.DateOfBirth;
            DpDod.SelectedDate = p.DateOfDeath;
            PreviewPhoto.Source = p.Photo;

            SetEditMode();
        }


        private void BtnReLayout_Click(object sender, RoutedEventArgs e)
        {
            FamilyTreeCtrl.Render(familyTree);
        }

        private void BtnAssignChild_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Seleccione un miembro en el árbol.");
                return;
            }

            var selWin = new SelectPersonWindow(familyTree, exclude: _selected) { Owner = this };
            if (selWin.ShowDialog() == true && selWin.ChosenNode != null)
            {
                try
                {
                    familyTree.AddChild(_selected, selWin.ChosenNode);
                    FamilyTreeCtrl.Render(familyTree);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnAssignPartner_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Seleccione un miembro en el árbol.");
                return;
            }

            var selWin = new SelectPersonWindow(familyTree, exclude: _selected) { Owner = this };
            if (selWin.ShowDialog() == true && selWin.ChosenNode != null)
            {
                familyTree.AddPartner(_selected, selWin.ChosenNode);
                FamilyTreeCtrl.Render(familyTree);
            }
        }

        private void BtnRebuildGraph_Click(object sender, RoutedEventArgs e)
        {
            familyTree.BuildLocationEdges();
            MessageBox.Show("Grafo de ubicaciones reconstruido (distancias calculadas).", "Ok",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateStats();
        }

        private void UpdateStats()
        {
            var nodes = familyTree.LocationGraph.Nodes.ToList();
            if (nodes.Count < 2)
            {
                TxtMaxPair.Text = "-";
                TxtMinPair.Text = "-";
                TxtAvg.Text = "-";
                return;
            }

            double max = double.MinValue, min = double.MaxValue, sum = 0;
            (Node<Person> aMax, Node<Person> bMax) = (null, null);
            (Node<Person> aMin, Node<Person> bMin) = (null, null);
            int count = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var p1 = nodes[i].Value;
                    var p2 = nodes[j].Value;
                    double d = GeoUtils.HaversineDistanceKm(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
                    sum += d;
                    count++;
                    if (d > max) { max = d; aMax = nodes[i]; bMax = nodes[j]; }
                    if (d < min) { min = d; aMin = nodes[i]; bMin = nodes[j]; }
                }
            }

            double avg = count > 0 ? sum / count : 0;
            TxtMaxPair.Text = $"{aMax.Value.FullName} ↔ {bMax.Value.FullName} : {max:F2} km";
            TxtMinPair.Text = $"{aMin.Value.FullName} ↔ {bMin.Value.FullName} : {min:F2} km";
            TxtAvg.Text = $"{avg:F2} km";
        }

        private void BtnSaveJson_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "JSON files|*.json" };
            if (dlg.ShowDialog() == true)
            {
                var list = familyTree.LocationGraph.Nodes.Select(n => new SerializablePerson(n)).ToList();
                var js = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dlg.FileName, js);
                MessageBox.Show("Guardado.");
            }
        }

        private void BtnLoadJson_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON files|*.json" };
            if (dlg.ShowDialog() == true)
            {
                string txt = File.ReadAllText(dlg.FileName);
                var list = JsonSerializer.Deserialize<List<SerializablePerson>>(txt);
                if (list == null) return;

                foreach (var n in familyTree.LocationGraph.Nodes.ToList())
                    familyTree.LocationGraph.RemoveNode(n.Id);

                foreach (var sp in list)
                {
                    var p = new Person
                    {
                        FirstName = sp.FirstName,
                        LastName = sp.LastName,
                        Cedula = sp.Cedula,
                        Latitude = sp.Latitude,
                        Longitude = sp.Longitude,
                        DateOfBirth = sp.DateOfBirth,
                        DateOfDeath = sp.DateOfDeath
                    };
                    familyTree.AddMember(p);
                }

                RefreshTreeView();
                MessageBox.Show("Cargado.");
            }
        }

        private class SerializablePerson
        {
            public string Cedula { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime DateOfBirth { get; set; }
            public DateTime? DateOfDeath { get; set; }

            public SerializablePerson() { }
            public SerializablePerson(Node<Person> n)
            {
                Cedula = n.Value.Cedula;
                FirstName = n.Value.FirstName;
                LastName = n.Value.LastName;
                Latitude = n.Value.Latitude;
                Longitude = n.Value.Longitude;
                DateOfBirth = n.Value.DateOfBirth;
                DateOfDeath = n.Value.DateOfDeath;
            }
        }
    }
}

