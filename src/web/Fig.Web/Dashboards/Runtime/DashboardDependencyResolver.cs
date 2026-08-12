namespace Fig.Web.Dashboards.Runtime;

public class DashboardDependencyResolver
{
    public IReadOnlyList<string> ResolveOrder(IEnumerable<(string Id, IReadOnlyList<string> DependsOn)> transforms)
    {
        var nodes = transforms
            .Where(t => !string.IsNullOrWhiteSpace(t.Id))
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.First().DependsOn
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        var path = new List<string>();

        foreach (var id in nodes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            Visit(id);

        return ordered;

        void Visit(string id)
        {
            if (visited.Contains(id))
                return;

            if (!visiting.Add(id))
            {
                var cycleStart = path.FindIndex(p => string.Equals(p, id, StringComparison.OrdinalIgnoreCase));
                var cycle = cycleStart >= 0
                    ? string.Join(" → ", path.Skip(cycleStart).Append(id))
                    : id;
                throw new InvalidOperationException(
                    $"Circular dashboard transform dependency detected: {cycle}");
            }

            path.Add(id);

            if (nodes.TryGetValue(id, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (!nodes.ContainsKey(dep))
                    {
                        throw new InvalidOperationException(
                            $"Transform '{id}' depends on unknown transform '{dep}'.");
                    }

                    Visit(dep);
                }
            }

            path.RemoveAt(path.Count - 1);
            visiting.Remove(id);
            visited.Add(id);
            ordered.Add(id);
        }
    }

    public IReadOnlyList<string> ResolveOrder(
        IEnumerable<Fig.Contracts.Dashboards.DashboardTransformDataContract> transforms)
    {
        return ResolveOrder(transforms.Select(t => (t.Id, (IReadOnlyList<string>)t.DependsOn)));
    }
}
