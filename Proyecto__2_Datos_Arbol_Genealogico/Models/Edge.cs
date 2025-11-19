using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto__2_Datos_Arbol_Genealogico.Models
{
    public class Edge<T>
    {
        public Node<T> From { get; set; }
        public Node<T> To { get; set; }
        public double Weight { get; set; } = 1.0;

        public Edge(Node<T> from, Node<T> to, double weight = 1.0)
        {
            From = from;
            To = to;
            Weight = weight;
        }
    }
}
