using System;
using System.Collections.Generic;
using System.Linq;

namespace Proyecto__2_Datos_Arbol_Genealogico.Models
{
    public class FamilyTree
    {
        public event Action? Changed;

        public void TriggerChanged()
        {
            Changed?.Invoke();
        }

        public Graph<Person> LocationGraph { get; } = new();

        private readonly Dictionary<Guid, List<Guid>> parentToChildren = new();
        private readonly Dictionary<Guid, List<Guid>> childToParents = new();

        public IEnumerable<Node<Person>> Nodes => LocationGraph.Nodes;

        public Node<Person> AddMember(Person p)
        {
            var n = LocationGraph.AddNode(p);
            Changed?.Invoke();
            return n;
        }

        public void AddPartner(Node<Person> a, Node<Person> b)
        {
            if (a.Value.PartnerId == b.Id) return;
            a.Value.PartnerId = b.Id;
            b.Value.PartnerId = a.Id;
            Changed?.Invoke();
        }

        public void AddChild(Node<Person> parent, Node<Person> child)
        {
            if (IsAncestor(candidateAncestor: child, candidateDescendant: parent))
                throw new InvalidOperationException("Asignación inválida: crearía un ciclo.");

            if (!childToParents.TryGetValue(child.Id, out var parents))
            {
                parents = new List<Guid>();
                childToParents[child.Id] = parents;
            }
            if (!parents.Contains(parent.Id))
            {
                if (parents.Count >= 2)
                    throw new InvalidOperationException("Un hijo no puede tener más de dos padres.");
                parents.Add(parent.Id);
            }

            if (!parentToChildren.TryGetValue(parent.Id, out var kids))
            {
                kids = new List<Guid>();
                parentToChildren[parent.Id] = kids;
            }
            if (!kids.Contains(child.Id))
                kids.Add(child.Id);

            Changed?.Invoke();
        }

        public IEnumerable<Node<Person>> GetParents(Node<Person> child)
        {
            if (!childToParents.TryGetValue(child.Id, out var list) || list.Count == 0)
                return Enumerable.Empty<Node<Person>>();

            return list.Select(id => LocationGraph.GetNode(id))
                       .Where(n => n != null)!;
        }

        public IEnumerable<Node<Person>> GetChildren(Node<Person> parent)
        {
            IEnumerable<Node<Person>> own = Enumerable.Empty<Node<Person>>();
            if (parentToChildren.TryGetValue(parent.Id, out var ids1))
                own = ids1.Select(id => LocationGraph.GetNode(id)).Where(n => n != null)!;

            IEnumerable<Node<Person>> partnerKids = Enumerable.Empty<Node<Person>>();
            if (parent.Value.PartnerId is Guid pid && parentToChildren.TryGetValue(pid, out var ids2))
                partnerKids = ids2.Select(id => LocationGraph.GetNode(id)).Where(n => n != null)!;

            return own.Concat(partnerKids).GroupBy(n => n.Id).Select(g => g.First());
        }
        public IEnumerable<Node<Person>> GetRoots()
        {
            bool HasParents(Guid id) =>
                childToParents.TryGetValue(id, out var lst) && lst.Count > 0;

            var candidates = new List<Node<Person>>();
            foreach (var n in LocationGraph.Nodes)
            {
                var partnerId = n.Value.PartnerId;
                bool nHasParents = HasParents(n.Id);
                bool partnerHasParents = partnerId.HasValue && HasParents(partnerId.Value);

                if (nHasParents || partnerHasParents) continue;

                candidates.Add(n);
            }

            var taken = new HashSet<Guid>();
            var result = new List<Node<Person>>();

            foreach (var r in candidates)
            {
                if (taken.Contains(r.Id)) continue;

                if (r.Value.PartnerId is Guid pid &&
                    candidates.Any(c => c.Id == pid))
                {
                    var repId = r.Id.CompareTo(pid) < 0 ? r.Id : pid;
                    var rep = LocationGraph.GetNode(repId);
                    if (rep != null && !taken.Contains(repId))
                    {
                        result.Add(rep);
                        taken.Add(r.Id);
                        taken.Add(pid);
                        taken.Add(repId);
                    }
                }
                else
                {
                    result.Add(r);
                    taken.Add(r.Id);
                }
            }

            return result;
        }

        private bool IsAncestor(Node<Person> candidateAncestor, Node<Person> candidateDescendant)
        {
            var seen = new HashSet<Guid>();
            var q = new Queue<Node<Person>>();
            q.Enqueue(candidateDescendant);
            seen.Add(candidateDescendant.Id);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                foreach (var p in GetParents(cur))
                {
                    if (!seen.Add(p.Id)) continue;
                    if (p.Id == candidateAncestor.Id) return true;
                    q.Enqueue(p);
                }
            }
            return false;
        }

        public void BuildLocationEdges()
        {
            LocationGraph.ClearAllEdges();
            var nodes = LocationGraph.Nodes.ToList();

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var p1 = nodes[i].Value;
                    var p2 = nodes[j].Value;

                    double d = Utils.GeoUtils.HaversineDistanceKm(
                        p1.Latitude, p1.Longitude,
                        p2.Latitude, p2.Longitude
                    );

                    LocationGraph.AddEdge(nodes[i], nodes[j], d);
                }
            }

            Changed?.Invoke();
        }
    }
}







