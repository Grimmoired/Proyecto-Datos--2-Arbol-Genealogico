using System;
using System.Collections.Generic;
using System.Linq;

namespace Proyecto__2_Datos_Arbol_Genealogico.Models
{
    public class Node<T>
    {
        public T Value { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();

        public Node(T value)
        {
            Value = value;
        }
    }

    public class Edge<T>
    {
        public Node<T> From { get; }
        public Node<T> To { get; }
        public double Weight { get; }

        public Edge(Node<T> from, Node<T> to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }
    }

    public class Graph<T>
    {
        private readonly Dictionary<Guid, Node<T>> nodes = new();
        private readonly Dictionary<Guid, List<Edge<T>>> adj = new();

        public IEnumerable<Node<T>> Nodes => nodes.Values;

        public Node<T> AddNode(T value)
        {
            var node = new Node<T>(value);
            nodes[node.Id] = node;
            adj[node.Id] = new List<Edge<T>>();
            return node;
        }

        public Node<T>? GetNode(Guid id)
        {
            nodes.TryGetValue(id, out var n);
            return n;
        }

        public bool RemoveNode(Guid id)
        {
            if (!nodes.ContainsKey(id)) return false;
            nodes.Remove(id);
            adj.Remove(id);
            foreach (var list in adj.Values)
                list.RemoveAll(e => e.To.Id == id || e.From.Id == id);
            return true;
        }

        public Edge<T> AddEdge(Node<T> from, Node<T> to, double weight = 1.0, bool undirected = true)
        {
            if (!nodes.ContainsKey(from.Id) || !nodes.ContainsKey(to.Id))
                throw new InvalidOperationException("Intento de conectar nodos no existentes.");

            var e = new Edge<T>(from, to, weight);
            adj[from.Id].Add(e);

            if (undirected)
            {
                var e2 = new Edge<T>(to, from, weight);
                adj[to.Id].Add(e2);
            }

            return e;
        }

        public IEnumerable<Edge<T>> GetNeighbors(Node<T> node)
        {
            return adj.TryGetValue(node.Id, out var list) ? list : Enumerable.Empty<Edge<T>>();
        }

        public void ClearAllEdges()
        {
            foreach (var key in adj.Keys.ToList())
                adj[key].Clear();
        }

        // ★★★ TU DIJKSTRA ORIGINAL RESTAURADO ★★★
        public Dictionary<Guid, double> Dijkstra(Node<T> source)
        {
            var dist = nodes.Keys.ToDictionary(k => k, k => double.PositiveInfinity);
            dist[source.Id] = 0;

            var visited = new HashSet<Guid>();
            var pq = new PriorityQueue<Guid, double>();
            pq.Enqueue(source.Id, 0);

            while (pq.TryDequeue(out Guid u, out _))
            {
                if (!visited.Add(u)) continue;

                foreach (var e in adj[u])
                {
                    var alt = dist[u] + e.Weight;
                    if (alt < dist[e.To.Id])
                    {
                        dist[e.To.Id] = alt;
                        pq.Enqueue(e.To.Id, alt);
                    }
                }
            }

            return dist;
        }
    }
}

