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

namespace Proyecto__2_Datos_Arbol_Genealogico
{
    public partial class MapWindow : Window
    {
        private readonly FamilyTree tree;
        private double mapWidth = 2048, mapHeight = 1024; // defaults, will adapt to image
        private readonly List<UIElement> overlays = new List<UIElement>();

        public MapWindow(FamilyTree familyTree)
        {
            InitializeComponent();
            tree = familyTree;
            LoadMapImage();
            RenderNodes();
        }

        private void LoadMapImage()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "world_map.jpg");
            if (!File.Exists(path))
            {
                MessageBox.Show($"No se encontró la imagen de mapa en: {path}\nPor favor coloque una imagen equirectangular llamada world_map.jpg en Resources/Images.", "Imagen faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                // fallback: create plain background
                MapImage.Source = new BitmapImage(); // blank
                MapCanvas.Width = 1200;
                MapCanvas.Height = 600;
                return;
            }
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            MapImage.Source = bmp;
            mapWidth = bmp.PixelWidth;
            mapHeight = bmp.PixelHeight;
            MapImage.Width = mapWidth;
            MapImage.Height = mapHeight;
            MapCanvas.Width = mapWidth;
            MapCanvas.Height = mapHeight;
        }

        private void RenderNodes()
        {
            ClearOverlays();

            var nodes = tree.LocationGraph.Nodes.ToList();
            foreach (var node in nodes)
            {
                AddNodeOverlay(node);
            }
        }

        private void AddNodeOverlay(Node<Person> node)
        {
            var p = node.Value;
            var (x, y) = GeoUtils.LatLonToPixel(p.Latitude, p.Longitude, mapWidth, mapHeight);
            // create image control with circle mask
            var img = new Image
            {
                Width = 56,
                Height = 56,
                Tag = node,
                ToolTip = $"{p.FullName}\n{p.Cedula}\n{p.Latitude}, {p.Longitude}"
            };

            if (p.Photo != null)
            {
                var crop = ImageHelpers.CreateSquareCrop(p.Photo, 56);
                var resized = ImageHelpers.Resize(crop, 56, 56);
                img.Source = resized;
            }
            else
            {
                // generate placeholder (solid color) as a DrawingImage
                var drawing = new DrawingGroup();
                drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Colors.SlateGray), null, new EllipseGeometry(new Point(28, 28), 28, 28)));
                var dv = new DrawingImage(drawing);
                dv.Freeze();
                img.Source = dv;
            }

            Canvas.SetLeft(img, x - img.Width / 2);
            Canvas.SetTop(img, y - img.Height / 2);
            img.Cursor = System.Windows.Input.Cursors.Hand;
            img.MouseLeftButtonUp += Img_MouseLeftButtonUp;

            // add circular clip
            img.Clip = new EllipseGeometry(new Point(img.Width / 2, img.Height / 2), img.Width / 2, img.Height / 2);

            MapCanvas.Children.Add(img);
            overlays.Add(img);
        }

        private void Img_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image img && img.Tag is Node<Person> node)
            {
                HighlightDistancesFrom(node);
            }
        }

        private void HighlightDistancesFrom(Node<Person> node)
        {
            ClearLines();
            TxtSelected.Text = $"Seleccionado: {node.Value.FullName} ({node.Value.Cedula})";
            ListDistances.Items.Clear();

            var nodes = tree.LocationGraph.Nodes.ToList();
            var source = node.Value;
            var origin = overlays.OfType<Image>().FirstOrDefault(i => (i.Tag as Node<Person>)?.Id == node.Id);
            foreach (var other in nodes.Where(n => n.Id != node.Id))
            {
                double d = GeoUtils.HaversineDistanceKm(source.Latitude, source.Longitude, other.Value.Latitude, other.Value.Longitude);
                ListDistances.Items.Add($"{other.Value.FullName} : {d:F2} km");

                // draw line from origin to other position
                var (x1, y1) = GeoUtils.LatLonToPixel(source.Latitude, source.Longitude, mapWidth, mapHeight);
                var (x2, y2) = GeoUtils.LatLonToPixel(other.Value.Latitude, other.Value.Longitude, mapWidth, mapHeight);

                var line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 60, 140, 220)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                MapCanvas.Children.Add(line);
                overlays.Add(line);

                // label mid-point
                double mx = (x1 + x2) / 2;
                double my = (y1 + y2) / 2;
                var tb = new TextBlock
                {
                    Text = $"{d:F1} km",
                    Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    FontSize = 12
                };
                Canvas.SetLeft(tb, mx);
                Canvas.SetTop(tb, my);
                MapCanvas.Children.Add(tb);
                overlays.Add(tb);
            }
        }

        private void ClearOverlays()
        {
            foreach (var el in overlays) MapCanvas.Children.Remove(el);
            overlays.Clear();
        }

        private void ClearLines()
        {
            // remove everything except MapImage and node images
            var toRemove = MapCanvas.Children.OfType<UIElement>().Where(e => !(e is Image && e != MapImage)).ToList();
            // however we only added overlays to overlays list, so:
            ClearOverlays();
            // readd nodes
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
