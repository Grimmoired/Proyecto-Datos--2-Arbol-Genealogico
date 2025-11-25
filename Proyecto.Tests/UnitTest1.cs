using System;
using System.Linq;
using Xunit;
using Proyecto__2_Datos_Arbol_Genealogico.Models;
using Proyecto__2_Datos_Arbol_Genealogico.Utils;

namespace Proyecto__2_Datos_Arbol_Genealogico.Tests
{
    public class ProjectTests
    {
        [Fact]
        public void Graph_AddNode_GetNode() // Comprueba que al añadir un nodo este se puede localizar por Id.
        {
            var g = new Graph<string>();
            var n = g.AddNode("hello");
            Assert.NotNull(n);
            var got = g.GetNode(n.Id);
            Assert.NotNull(got);
            Assert.Equal("hello", got.Value);
        }

        [Fact]
        public void Graph_AddEdge_GetNeighbors_Undirected() // Verifica que asignar pareja vincula la relación en ambos sentidos (es decir, crea las aristas en ambas direcciones)
        {
            var g = new Graph<string>();
            var a = g.AddNode("A");
            var b = g.AddNode("B");
            g.AddEdge(a, b, 2.5, undirected: true);

            var neighA = g.GetNeighbors(a).ToList();
            var neighB = g.GetNeighbors(b).ToList();

            Assert.Single(neighA);
            Assert.Single(neighB);

            Assert.Equal(b.Id, neighA[0].To.Id);
            Assert.Equal(a.Id, neighB[0].To.Id);

            Assert.Equal(2.5, neighA[0].Weight);
            Assert.Equal(2.5, neighB[0].Weight);
        }

        [Fact]
        public void Graph_Dijkstra_ShortestPathDistances() // Verifica que Dijkstra calcula correctamente las distancias más cortas posibles entre 2 nodos
        {
            var g = new Graph<string>();
            var n1 = g.AddNode("1");
            var n2 = g.AddNode("2");
            var n3 = g.AddNode("3");

            g.AddEdge(n1, n2, 1); 
            g.AddEdge(n2, n3, 2); 
            g.AddEdge(n1, n3, 10);

            var dist = g.Dijkstra(n1);

            Assert.InRange(dist[n2.Id], 0.9999, 1.0001);
            Assert.InRange(dist[n3.Id], 2.9999, 3.0001); 
        }

        [Fact]
        public void Person_AgeAndIsAlive() // Verifica que se les asigne el estado de "vivo" a las personas cuya fecha de nacimiento es coherente respecto a la fecha actual, y "muerto" a las que tienen fecha de muerte asignada y esta es previa a la fecha actual.
        {
            var p = new Person
            {
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            Assert.True(p.Age >= 0);
            Assert.True(p.IsAlive);

            p.DateOfDeath = p.DateOfBirth.AddYears(20);
            Assert.False(p.IsAlive);
            Assert.Equal(20, p.Age);
        }

        [Fact]
        public void GeoUtils_LatLonToPixel_Basics() // Verifica que la conversión de coordenadas geográficas a píxeles funciona correctamente en casos básicos.
        {
            double w = 360, h = 180;
            var (x0, y0) = GeoUtils.LatLonToPixel(0, 0, w, h);
            Assert.InRange(x0, w / 2 - 0.0001, w / 2 + 0.0001);
            Assert.InRange(y0, h / 2 - 0.0001, h / 2 + 0.0001);

            var (xLeft, _) = GeoUtils.LatLonToPixel(0, -180, w, h);
            var (xRight, _) = GeoUtils.LatLonToPixel(0, 180, w, h);

            Assert.InRange(xLeft, 0 - 0.0001, 0 + 0.0001);
            Assert.InRange(xRight, w - 0.0001, w + 0.0001);
        }

        [Fact]
        public void GeoUtils_Haversine_SymmetryAndZero() // Verifica que la implementacion de la función de distancia de Haversine es simétrica y que la distancia entre un punto y sí mismo es cero.
        {
            double lat1 = 9.857388352870416, lon1 = -83.90770043545028;
            double lat2 = 10.0, lon2 = -84.0;

            double d1 = GeoUtils.HaversineDistanceKm(lat1, lon1, lat2, lon2);
            double d2 = GeoUtils.HaversineDistanceKm(lat2, lon2, lat1, lon1);

            Assert.Equal(d1, d2, 6);
            Assert.Equal(0.0, GeoUtils.HaversineDistanceKm(lat1, lon1, lat1, lon1), 8);
        }

        [Fact]
        public void FamilyTree_Changed_Event_Fired_OnAddMember() // Verifica que el evento Changed se dispara al añadir un nuevo miembro al árbol familiar para actualizar la interfaz.
        {
            var tree = new FamilyTree();
            bool fired = false;
            tree.Changed += () => fired = true;

            var p = new Person { FirstName = "A" };
            tree.AddMember(p);

            Assert.True(fired);
        }

        [Fact]
        public void FamilyTree_AddChild_CycleDetection_Throws() // Verifa que se lance una excepción al intentar crear una relación que genere un ciclo en el árbol familiar.
        {
            var tree = new FamilyTree();

            var a = tree.AddMember(new Person { FirstName = "A" });
            var b = tree.AddMember(new Person { FirstName = "B" });
            var c = tree.AddMember(new Person { FirstName = "C" });

            tree.AddChild(a, b);
            tree.AddChild(b, c);

            var ex = Assert.Throws<InvalidOperationException>(() => tree.AddChild(c, a));
            Assert.Contains("ciclo", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FamilyTree_AddPartner_SetsPartnerIds() // Verifica que al asignar pareja se actualizan correctamente los IDs de pareja en ambos miembros.
        {
            var tree = new FamilyTree();
            var a = tree.AddMember(new Person { FirstName = "A" });
            var b = tree.AddMember(new Person { FirstName = "B" });

            tree.AddPartner(a, b);

            Assert.Equal(b.Id, a.Value.PartnerId);
            Assert.Equal(a.Id, b.Value.PartnerId);
        }

        [Fact]
        public void FamilyTree_BuildLocationEdges_CreatesEdges() // Verifica que el metodo BuildLocationEdges crea correctamente las aristas entre los nodos basándose en la proximidad geográfica.
        {
            var tree = new FamilyTree();
            var p1 = new Person { FirstName = "P1", Latitude = 0, Longitude = 0 };
            var p2 = new Person { FirstName = "P2", Latitude = 0, Longitude = 1 };

            var n1 = tree.AddMember(p1);
            var n2 = tree.AddMember(p2);

            tree.BuildLocationEdges();

            var neighbors1 = tree.LocationGraph.GetNeighbors(n1).ToList();
            var neighbors2 = tree.LocationGraph.GetNeighbors(n2).ToList();

            Assert.NotEmpty(neighbors1);
            Assert.NotEmpty(neighbors2);
            Assert.Contains(neighbors1, e => e.To.Id == n2.Id);
            Assert.Contains(neighbors2, e => e.To.Id == n1.Id);
        }
    }
}
