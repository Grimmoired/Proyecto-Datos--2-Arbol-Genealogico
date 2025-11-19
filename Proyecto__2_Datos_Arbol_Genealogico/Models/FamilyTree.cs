using Proyecto__2_Datos_Arbol_Genealogico.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proyecto__2_Datos_Arbol_Genealogico.Models
{
    public class FamilyTree
    {
        public Graph<Person> LocationGraph { get; } = new Graph<Person>();
        private readonly Dictionary<Guid, List<Guid>> parentToChildren = new Dictionary<Guid, List<Guid>>();
        private readonly Dictionary<Guid, Guid?> childToParent = new Dictionary<Guid, Guid?>();

        public Node<Person> AddMember(Person p)
        {
            var node = LocationGraph.AddNode(p);
            parentToChildren[node.Id] = new List<Guid>();
            childToParent[node.Id] = null;
            return node;
        }

        public bool SetParentChild(Node<Person> parent, Node<Person> child)
        {
            if (parent == null || child == null) return false;
            if (IsAncestor(child.Id, parent.Id)) return false;

            childToParent[child.Id] = parent.Id;
            if (!parentToChildren.ContainsKey(parent.Id)) parentToChildren[parent.Id] = new List<Guid>();
            if (!parentToChildren[parent.Id].Contains(child.Id))
                parentToChildren[parent.Id].Add(child.Id);
            return true;
        }

        public bool IsAncestor(Guid ancestorId, Guid descendantId)
        {
            Guid? current = childToParent.GetValueOrDefault(descendantId);
            while (current != null)
            {
                if (current == ancestorId) return true;
                current = childToParent.GetValueOrDefault(current.Value);
            }
            return false;
        }

        public IEnumerable<Node<Person>> GetChildren(Node<Person> parent)
        {
            if (!parentToChildren.ContainsKey(parent.Id)) yield break;
            foreach (var cid in parentToChildren[parent.Id])
            {
                var node = LocationGraph.Nodes.FirstOrDefault(n => n.Id == cid);
                if (node != null) yield return node;
            }
        }

        public Node<Person> GetParent(Node<Person> child)
        {
            var pid = childToParent.GetValueOrDefault(child.Id);
            if (pid == null) return null;
            return LocationGraph.Nodes.FirstOrDefault(n => n.Id == pid.Value);
        }

        public void BuildLocationEdges()
        {
            var all = LocationGraph.Nodes.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    var p1 = all[i].Value;
                    var p2 = all[j].Value;
                    double distKm = Utils.GeoUtils.HaversineDistanceKm(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
                    LocationGraph.AddEdge(all[i], all[j], distKm, undirected: true);
                }
            }
        }
    }
}
