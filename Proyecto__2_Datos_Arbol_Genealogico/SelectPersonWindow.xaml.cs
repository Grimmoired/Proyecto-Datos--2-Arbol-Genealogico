using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Proyecto__2_Datos_Arbol_Genealogico.Models;

namespace Proyecto__2_Datos_Arbol_Genealogico
{
    public partial class SelectPersonWindow : Window
    {
        public Node<Person> ChosenNode { get; private set; }

        public SelectPersonWindow(FamilyTree tree, Node<Person> exclude = null)
        {
            InitializeComponent(); 
            var list = tree.LocationGraph.Nodes
                            .Where(n => exclude == null || n.Id != exclude.Id)
                            .ToList();

            foreach (var n in list)
            {
                var itm = new ListBoxItem
                {
                    Content = $"{n.Value.FullName} ({n.Value.Cedula})",
                    Tag = n
                };
                ListBoxPersons.Items.Add(itm);
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxPersons.SelectedItem is ListBoxItem it)
            {
                ChosenNode = it.Tag as Node<Person>;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Seleccione una persona.", "Aviso",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
