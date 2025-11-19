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

    public class Graph<T>
    {
        private readonly Dictionary<Guid, Node<T>> nodes = new Dictionary<Guid, Node<T>>();
        private readonly Dictionary<Guid, List<Edge<T>>> adj = new Dictionary<Guid, List<Edge<T>>>();

        public IEnumerable<Node<T>> Nodes => nodes.Values;

        public Node<T> AddNode(T value)
        {
            var node = new Node<T>(value);
            nodes[node.Id] = node;
            adj[node.Id] = new List<Edge<T>>();
            return node;
        }

        public bool RemoveNode(Guid nodeId)
        {
            if (!nodes.ContainsKey(nodeId)) return false;
            nodes.Remove(nodeId);
            adj.Remove(nodeId);
            foreach (var list in adj.Values)
            {
                list.RemoveAll(e => e.From.Id == nodeId || e.To.Id == nodeId);
            }
            return true;
        }

        public Edge<T> AddEdge(Node<T> from, Node<T> to, double weight = 1.0, bool undirected = true)
        {
            if (!nodes.ContainsKey(from.Id) || !nodes.ContainsKey(to.Id))
                throw new InvalidOperationException("Node not present in graph.");

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
            if (!adj.ContainsKey(node.Id)) return Enumerable.Empty<Edge<T>>();
            return adj[node.Id];
        }

        public Node<T> FindNode(Func<Node<T>, bool> predicate)
        {
            return nodes.Values.FirstOrDefault(predicate);
        }

        public Node<T> FindNodeByValuePredicate(Func<T, bool> predicate)
        {
            return nodes.Values.FirstOrDefault(n => predicate(n.Value));
        }

        public Dictionary<Guid, double> Dijkstra(Node<T> source)
        {
            var dist = nodes.Keys.ToDictionary(k => k, k => double.PositiveInfinity);
            var prev = new Dictionary<Guid, Guid?>();
            var Q = new HashSet<Guid>(nodes.Keys);
            dist[source.Id] = 0;

            while (Q.Count > 0)
            {
                Guid u = Q.OrderBy(id => dist[id]).First();
                Q.Remove(u);

                foreach (var e in adj[u])
                {
                    var alt = dist[u] + e.Weight;
                    if (alt < dist[e.To.Id])
                    {
                        dist[e.To.Id] = alt;
                        prev[e.To.Id] = u;
                    }
                }
            }
            return dist;
        }
    }
}
