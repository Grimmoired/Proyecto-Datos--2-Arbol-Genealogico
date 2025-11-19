using Microsoft.Win32;
using Proyecto__2_Datos_Arbol_Genealogico;
using Proyecto__2_Datos_Arbol_Genealogico.Models;
using Proyecto__2_Datos_Arbol_Genealogico.Utils;
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
        private Node<Person> lastAddedNode = null;

        public MainWindow()
        {
            InitializeComponent();
            DpDob.SelectedDate = DateTime.Now.AddYears(-30);
            RefreshTreeView();
        }

        private void BtnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp";
            if (dlg.ShowDialog() == true)
            {
                loadedPhoto = Person.LoadImageFromPath(dlg.FileName);
                PreviewPhoto.Source = loadedPhoto;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var p = new Person();
                p.FirstName = TxtFirstName.Text.Trim();
                p.LastName = TxtLastName.Text.Trim();
                p.Cedula = TxtCedula.Text.Trim();

                if (!double.TryParse(TxtLat.Text?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lat))
                {
                    MessageBox.Show("Latitud inválida.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(TxtLon.Text?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double lon))
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
                {
                    p.DateOfDeath = DpDod.SelectedDate.Value;
                }
                p.Photo = loadedPhoto;

                var node = familyTree.AddMember(p);
                lastAddedNode = node;

                // Clear form
                TxtFirstName.Text = TxtLastName.Text = TxtCedula.Text = "";
                TxtLat.Text = TxtLon.Text = "";
                DpDob.SelectedDate = DateTime.Now.AddYears(-30);
                DpDod.SelectedDate = null;
                PreviewPhoto.Source = null;
                loadedPhoto = null;

                RefreshTreeView();
                MessageBox.Show("Miembro agregado.", "Ok", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void RefreshTreeView()
        {
            TreeViewFamily.Items.Clear();
            // Since we have only parent pointers, build tree roots (those without parent)
            var nodes = familyTree.LocationGraph.Nodes.ToList();
            var roots = nodes.Where(n => familyTree.GetParent(n) == null).ToList();
            foreach (var root in roots)
            {
                TreeViewItem rootItem = CreateTreeItem(root);
                TreeViewFamily.Items.Add(rootItem);
            }
            UpdateStats();
        }

        private TreeViewItem CreateTreeItem(Node<Person> node)
        {
            var item = new TreeViewItem { Header = $"{node.Value.FullName} ({node.Value.Cedula}) - {node.Value.Age} años" };
            item.Tag = node;
            // children
            foreach (var child in familyTree.GetChildren(node))
            {
                item.Items.Add(CreateTreeItem(child));
            }
            // Context menu to set parent-child quickly
            var ctx = new ContextMenu();
            var miSetAsParent = new MenuItem { Header = "Establecer este como padre de..." };
            miSetAsParent.Click += (s, e) => {
                // choose node from all nodes
                var selectWin = new SelectPersonWindow(familyTree, node); // we implement minimal below
                selectWin.Owner = this;
                if (selectWin.ShowDialog() == true)
                {
                    var chosen = selectWin.ChosenNode;
                    if (chosen != null)
                    {
                        bool ok = familyTree.SetParentChild(node, chosen);
                        if (!ok) MessageBox.Show("No se pudo establecer relación (posible ciclo).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        RefreshTreeView();
                    }
                }
            };
            ctx.Items.Add(miSetAsParent);
            item.ContextMenu = ctx;
            return item;
        }

        private void BtnOpenMap_Click(object sender, RoutedEventArgs e)
        {
            var map = new MapWindow(familyTree);
            map.Owner = this;
            map.Show();
        }

        private void BtnRebuildGraph_Click(object sender, RoutedEventArgs e)
        {
            familyTree.BuildLocationEdges();
            MessageBox.Show("Grafo de ubicaciones reconstruido (distancias calculadas).", "Ok", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // Simple JSON save/load
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
                // reset tree
                // recreate familyTree
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
                    // photo not embedded; optional: implement base64 later
                    familyTree.AddMember(p);
                }
                RefreshTreeView();
                MessageBox.Show("Cargado.");
            }
        }

        // small serializable DTO
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

        private void TxtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
