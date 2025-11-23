using Proyecto__2_Datos_Arbol_Genealogico.Models;
using Proyecto__2_Datos_Arbol_Genealogico.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IOPath = System.IO.Path;

namespace Proyecto__2_Datos_Arbol_Genealogico
{
    public partial class MapWindow : Window
    {
        private readonly FamilyTree tree;
        private double imagePixelW, imagePixelH;
        private readonly List<UIElement> overlays = new();

        public MapWindow(FamilyTree familyTree)
        {
            InitializeComponent();
            tree = familyTree;
            LoadMapImage();
            RenderNodes();
        }

        private void LoadMapImage()
        {
            string path = IOPath.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Images", "mapa.jpg"
            );

            if (!File.Exists(path))
            {
                MessageBox.Show($"No se encontró el mapa en:\n{path}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using var stream = new MemoryStream(bytes);

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.EndInit();
                bmp.Freeze();

                imagePixelW = bmp.PixelWidth;
                imagePixelH = bmp.PixelHeight;

                MapImage.Source = bmp;
                MapImage.Width = imagePixelW;
                MapImage.Height = imagePixelH;
                MapCanvas.Width = imagePixelW;
                MapCanvas.Height = imagePixelH;

                MapCanvas.Background = Brushes.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el mapa:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void RenderNodes()
        {
            ClearOverlays();

            foreach (var node in tree.LocationGraph.Nodes)
                AddNodeOverlay(node);
        }

        private void AddNodeOverlay(Node<Person> node)
        {
            var p = node.Value;
            var (px, py) = GeoUtils.LatLonToPixel(p.Latitude, p.Longitude, imagePixelW, imagePixelH);

            var img = new Image
            {
                Width = 56,
                Height = 56,
                Tag = node,
                ToolTip = $"{p.FullName}\n{p.Cedula}\n{p.Latitude},{p.Longitude}",
                Clip = new EllipseGeometry(new Point(28, 28), 28, 28)
            };

            if (p.Photo != null)
            {
                var crop = ImageHelpers.CreateSquareCrop(p.Photo, 56);
                img.Source = ImageHelpers.Resize(crop, 56, 56);
            }
            else
            {
                img.Source = new DrawingImage(
                    new GeometryDrawing(
                        Brushes.SlateGray, null,
                        new EllipseGeometry(new Point(28, 28), 28, 28)
                    )
                );
            }

            Canvas.SetLeft(img, px - img.Width / 2);
            Canvas.SetTop(img, py - img.Height / 2);
            img.MouseLeftButtonUp += Img_MouseLeftButtonUp;

            MapCanvas.Children.Add(img);
            overlays.Add(img);
        }

        private void Img_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image img && img.Tag is Node<Person> node)
                HighlightDistancesFrom(node);
        }

        private void HighlightDistancesFrom(Node<Person> node)
        {
            ClearLines();
            TxtSelected.Text = $"Seleccionado: {node.Value.FullName} ({node.Value.Cedula})";
            ListDistances.Items.Clear();

            var source = node.Value;

            foreach (var other in tree.LocationGraph.Nodes.Where(n => n.Id != node.Id))
            {
                double d = GeoUtils.HaversineDistanceKm(
                    source.Latitude, source.Longitude,
                    other.Value.Latitude, other.Value.Longitude);

                ListDistances.Items.Add($"{other.Value.FullName} : {d:F2} km");
                DrawConnectionLine(source, other.Value, d);
            }
        }

        private void DrawConnectionLine(Person p1, Person p2, double km)
        {
            var (x1, y1) = GeoUtils.LatLonToPixel(p1.Latitude, p1.Longitude, imagePixelW, imagePixelH);
            var (x2, y2) = GeoUtils.LatLonToPixel(p2.Latitude, p2.Longitude, imagePixelW, imagePixelH);

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            MapCanvas.Children.Add(line);
            overlays.Add(line);

            var tb = new TextBlock
            {
                Text = $"{km:F1} km",
                Background = Brushes.Black,
                FontSize = 12
            };

            Canvas.SetLeft(tb, (x1 + x2) / 2);
            Canvas.SetTop(tb, (y1 + y2) / 2);
            MapCanvas.Children.Add(tb);
            overlays.Add(tb);
        }

        private void ClearOverlays()
        {
            foreach (var el in overlays)
                MapCanvas.Children.Remove(el);
            overlays.Clear();
        }

        private void ClearLines()
        {
            ClearOverlays();
            RenderNodes();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearLines();
            ListDistances.Items.Clear();
            TxtSelected.Text = "Seleccione un miembro en el mapa.";
        }
    }
}




