    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Proyecto__2_Datos_Arbol_Genealogico.Models;

    namespace Proyecto__2_Datos_Arbol_Genealogico.Views
    {
        public partial class FamilyTreeView : UserControl
        {
            public FamilyTreeView() { InitializeComponent(); }

            public event Action<Node<Person>>? NodeClicked;

            private const double NodeW = 110, NodeH = 130, GapY = 130, GapX = 35, CoupleGap = 30;

            private FamilyTree _tree = null!;
            private readonly Dictionary<Guid, Point> pos = new();

            public void Render(FamilyTree tree)
            {
                _tree = tree;
                TreeCanvas.Children.Clear();
                pos.Clear();

                var placed = new HashSet<Guid>();
                double x = 40, y = 40;

                foreach (var r in tree.GetRoots())
                {
                    if (placed.Contains(r.Id)) continue;
                    x += Layout(r, x, y, placed) + 100;
                }

                double maxX = pos.Values.DefaultIfEmpty(new Point(0, 0)).Max(p => p.X);
                double maxY = pos.Values.DefaultIfEmpty(new Point(0, 0)).Max(p => p.Y);
                TreeCanvas.Width = maxX + 200;
                TreeCanvas.Height = maxY + 200;
            }

            private double Layout(Node<Person> n, double x, double y, HashSet<Guid> placed)
            {
                if (!placed.Add(n.Id)) return NodeW;

                Node<Person>? p = null;
                if (n.Value.PartnerId is Guid pid)
                    p = _tree.Nodes.FirstOrDefault(z => z.Id == pid);

                var ch = _tree.GetChildren(n).ToList();

                if (p != null)
                    ch = ch.Concat(_tree.GetChildren(p)).GroupBy(c => c.Id).Select(g => g.First()).ToList();

                var widths = ch.Select(c => Width(c, new HashSet<Guid>())).ToList();
                double kids = widths.Sum() + (ch.Count - 1) * GapX;
                double couple = (p != null) ? NodeW * 2 + CoupleGap : NodeW;
                double used = Math.Max(kids, couple);

                double nx = x + (used - couple) / 2;
                Place(n, nx, y);

                if (p != null && placed.Add(p.Id))
                {
                    Place(p, nx + NodeW + CoupleGap, y);
                    LinkCouple(n, p);
                }

                double cx = x;
                foreach (var (c, w) in ch.Zip(widths))
                {
                    Layout(c, cx, y + NodeH + GapY, placed);
                    LinkParents(n, p, c);
                    cx += w + GapX;
                }
                return used;
            }

            private double Width(Node<Person> n, HashSet<Guid> v)
            {
                if (!v.Add(n.Id)) return NodeW;

                Node<Person>? p = null;
                if (n.Value.PartnerId is Guid pid)
                    p = _tree.Nodes.FirstOrDefault(z => z.Id == pid);

                var ch = _tree.GetChildren(n).ToList();
                if (p != null)
                    ch = ch.Concat(_tree.GetChildren(p)).GroupBy(c => c.Id).Select(g => g.First()).ToList();

                if (ch.Count == 0) return (p != null ? NodeW * 2 + CoupleGap : NodeW);

                double w = 0;
                foreach (var c in ch)
                    w += Width(c, new HashSet<Guid>()) + GapX;
                return Math.Max(w - GapX, (p != null ? NodeW * 2 + CoupleGap : NodeW));
            }

            private void Place(Node<Person> n, double x, double y)
            {
                pos[n.Id] = new Point(x, y);
                var card = Build(n);
                Canvas.SetLeft(card, x);
                Canvas.SetTop(card, y);
                TreeCanvas.Children.Add(card);
            }

        private UIElement Build(Node<Person> n)
        {
            var p = n.Value;
            var img = new Image
            {
                Width = 72,
                Height = 72,
                Stretch = Stretch.UniformToFill
            };
            if (p.Photo != null)
                img.Source = p.Photo;

            var stack = new StackPanel
            {
                Width = NodeW,
                Background = null
            };

            stack.Children.Add(img);

            stack.Children.Add(new TextBlock
            {
                Text = p.FullName,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            });

            stack.Children.Add(new TextBlock
            {
                Text = p.Cedula,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180))
            });

            var b = new Border
            {
                Child = stack,
                Width = NodeW,
                Height = NodeH,
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 110, 220)), // Azul oscuro suave
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),  // Fondo oscuro
                Tag = n,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 0, 0),
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Opacity = 0.6
                }
            };

            b.MouseLeftButtonUp += (s, e) => NodeClicked?.Invoke(n);
            return b;
        }

        private void Line(Point a, Point b) =>
                TreeCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = Brushes.White, StrokeThickness = 1.5 });

            private void LinkCouple(Node<Person> a, Node<Person> b)
            {
                var pa = pos[a.Id];
                var pb = pos[b.Id];
                Line(new Point(pa.X + NodeW, pa.Y + NodeH / 2), new Point(pb.X, pb.Y + NodeH / 2));
            }

            private void LinkParents(Node<Person> a, Node<Person>? b, Node<Person> c)
            {
                var cp = pos[c.Id];
                if (b == null)
                    Line(new Point(pos[a.Id].X + NodeW / 2, pos[a.Id].Y + NodeH), new Point(cp.X + NodeW / 2, cp.Y));
                else
                {
                    var A = pos[a.Id]; var B = pos[b.Id];
                    var pa = new Point(A.X + NodeW / 2, A.Y + NodeH);
                    var pb = new Point(B.X + NodeW / 2, B.Y + NodeH);
                    var mid = new Point((pa.X + pb.X) / 2, Math.Max(pa.Y, pb.Y) + 6);
                    Line(pa, mid); Line(pb, mid); Line(mid, new Point(cp.X + NodeW / 2, cp.Y));
                }
            }
        }
    }






