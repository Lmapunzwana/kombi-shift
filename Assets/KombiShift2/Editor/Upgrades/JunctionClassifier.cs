using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class JunctionClassifier
{
    const string NetworkPath = "Assets/Resources/CityRoadNetwork.json";
    const string SettingsPath = "Assets/Resources/CityJunctionClassifierSettings.json";
    const string OverridesPath = "Assets/Resources/CityJunctionOverrides.json";
    const string OutputPath = "Assets/Resources/CityJunctions.json";
    const string AuditPath = "Docs/Validation/CityInfrastructure/junction-classification.txt";

    [MenuItem("Kombi Shift/Classify Existing City Junctions")]
    public static void Run()
    {
        var networkAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(NetworkPath);
        if (networkAsset == null) throw new FileNotFoundException("City road network is missing", NetworkPath);

        var source = JsonUtility.FromJson<NetworkData>(networkAsset.text);
        if (source == null || source.nodes == null || source.nodes.Length == 0)
            throw new InvalidDataException("CityRoadNetwork.json contains no nodes.");

        var settings = LoadOrCreateSettings();
        var overrides = LoadOrCreateOverrides();
        ValidateLinks(source.nodes);

        var componentNodes = Enumerable.Range(0, source.nodes.Length)
            .Where(i => source.nodes[i].component == 0).ToArray();
        var candidates = componentNodes.Where(i => source.nodes[i].junction || source.nodes[i].roundabout).ToArray();
        var reverse = BuildReverseLinks(source.nodes);
        var clusters = BuildClusters(source.nodes, componentNodes, reverse, settings);
        var records = new List<JunctionRecord>();

        foreach (var cluster in clusters.OrderBy(c => c.Min()))
        {
            string id = (cluster.Any(i => source.nodes[i].roundabout) ? "roundabout-" : "junction-") + cluster.Min();
            var centre = WeightedCentre(source.nodes, cluster);
            var connections = BoundaryConnections(source.nodes, reverse, cluster, centre);
            var arms = GroupArms(source.nodes, connections, centre, settings.armMergeDegrees);
            MarkMajorArms(arms);

            string reason;
            string type = ClassifyType(source.nodes, cluster, arms, settings, out reason);
            string control = ClassifyControl(type, arms, settings);
            float widest = arms.Count == 0 ? cluster.Max(i => source.nodes[i].width) : arms.Max(a => a.width);
            float radius = Mathf.Max(settings.minimumRadius, widest * settings.radiusWidthFactor + arms.Count * settings.radiusPerArm);
            var record = new JunctionRecord
            {
                id = id,
                type = type,
                control = control,
                confidence = type == "Unclassified" ? "Low" : Confidence(source.nodes, cluster, arms),
                reason = reason,
                centre = centre,
                radius = Round(radius),
                widestArm = Round(widest),
                nodeIds = cluster.OrderBy(x => x).ToArray(),
                arms = arms.OrderBy(a => a.bearing).Select((a, index) => ToRecord(a, index, centre, control, settings)).ToArray(),
                phasePlan = Array.Empty<string>(),
                overrideApplied = false
            };
            ApplyOverride(record, overrides.overrides);
            records.Add(record);
        }

        var asymmetric = FindAsymmetricLinks(source.nodes, componentNodes);
        var document = new JunctionDocument
        {
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            source = NetworkPath,
            component = 0,
            settings = settings,
            junctions = records.ToArray()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Assets/Resources");
        File.WriteAllText(OutputPath, JsonUtility.ToJson(document, true) + "\n");
        Directory.CreateDirectory(Path.GetDirectoryName(AuditPath) ?? "Docs/Validation/CityInfrastructure");
        File.WriteAllText(AuditPath, BuildAudit(source.nodes, componentNodes, candidates, records, asymmetric, settings));
        AssetDatabase.Refresh();
        Debug.Log($"KOMBI_JUNCTION_CLASSIFICATION_COMPLETE junctions={records.Count} unclassified={records.Count(r => r.type == "Unclassified")} asymmetricLinks={asymmetric.Count}");
    }

    static ClassifierSettings LoadOrCreateSettings()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaults = new ClassifierSettings();
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(defaults, true) + "\n");
            AssetDatabase.Refresh();
            return defaults;
        }
        var settings = JsonUtility.FromJson<ClassifierSettings>(File.ReadAllText(SettingsPath));
        if (settings == null) throw new InvalidDataException(SettingsPath + " is invalid.");
        if (settings.clusterWidthFactor <= 0 || settings.armMergeDegrees <= 0 || settings.signalMinimumWidth <= 0)
            throw new InvalidDataException(SettingsPath + " contains invalid non-positive thresholds.");
        return settings;
    }

    static OverrideDocument LoadOrCreateOverrides()
    {
        if (!File.Exists(OverridesPath))
        {
            var empty = new OverrideDocument { overrides = Array.Empty<JunctionOverride>() };
            File.WriteAllText(OverridesPath, JsonUtility.ToJson(empty, true) + "\n");
            AssetDatabase.Refresh();
            return empty;
        }
        var document = JsonUtility.FromJson<OverrideDocument>(File.ReadAllText(OverridesPath));
        if (document == null) throw new InvalidDataException(OverridesPath + " is invalid.");
        document.overrides ??= Array.Empty<JunctionOverride>();
        var duplicate = document.overrides.Where(x => x != null).GroupBy(x => x.junctionId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null) throw new InvalidDataException("Duplicate junction override: " + duplicate.Key);
        return document;
    }

    static void ValidateLinks(Node[] nodes)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].links ??= Array.Empty<int>();
            foreach (int link in nodes[i].links)
                if (link < 0 || link >= nodes.Length) throw new InvalidDataException($"Node {i} links to missing node {link}.");
        }
    }

    static List<int[]> ClusterCandidates(Node[] nodes, int[] candidates, float widthFactor)
    {
        var parent = candidates.ToDictionary(i => i, i => i);
        int Find(int value)
        {
            int root = value;
            while (parent[root] != root) root = parent[root];
            while (parent[value] != value) { int next = parent[value]; parent[value] = root; value = next; }
            return root;
        }
        void Union(int a, int b)
        {
            a = Find(a); b = Find(b);
            if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b);
        }
        for (int a = 0; a < candidates.Length; a++)
        for (int b = a + 1; b < candidates.Length; b++)
        {
            int first = candidates[a], second = candidates[b];
            float threshold = widthFactor * Mathf.Max(nodes[first].width, nodes[second].width);
            if (HorizontalDistance(nodes[first].position, nodes[second].position) <= threshold) Union(first, second);
        }
        return candidates.GroupBy(Find).Select(g => g.OrderBy(x => x).ToArray()).ToList();
    }

    static List<int[]> BuildClusters(Node[] nodes, int[] componentNodes, List<int>[] reverse, ClassifierSettings settings)
    {
        var result = new List<int[]>();
        var used = new HashSet<int>();
        int[] flags = componentNodes.Where(i => nodes[i].roundabout).ToArray();
        foreach (var flagGroup in SpatialGroups(nodes, flags, settings.roundaboutGroupingDistance))
        {
            if (flagGroup.Length < settings.roundaboutMinimumFlags) continue;
            FitCircle(nodes, flagGroup, out Vector3 centre, out float radius, out float residual);
            if (radius <= 0 || residual > radius * settings.roundaboutMaximumResidualFactor) continue;
            float elevation = flagGroup.Average(i => nodes[i].position.y);
            var radial = new HashSet<int>(componentNodes.Where(i =>
                Mathf.Abs(HorizontalDistance(nodes[i].position, centre) - radius) <= settings.roundaboutRadialTolerance &&
                Mathf.Abs(nodes[i].position.y - elevation) <= settings.interchangeHeightDifference));
            var expanded = new HashSet<int>();
            var queue = new Queue<int>(flagGroup.Where(radial.Contains));
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(); if (!expanded.Add(current)) continue;
                foreach (int next in nodes[current].links.Concat(reverse[current])) if (radial.Contains(next) && !expanded.Contains(next)) queue.Enqueue(next);
            }
            if (expanded.Count < flagGroup.Length) continue;
            foreach (int index in expanded) used.Add(index);
            result.Add(expanded.OrderBy(x => x).ToArray());
        }
        int[] ordinary = componentNodes.Where(i => nodes[i].junction && !used.Contains(i) && !nodes[i].roundabout).ToArray();
        result.AddRange(ClusterCandidates(nodes, ordinary, settings.clusterWidthFactor));
        return result;
    }

    static List<int[]> SpatialGroups(Node[] nodes, int[] candidates, float distance)
    {
        var remaining = new HashSet<int>(candidates); var groups = new List<int[]>();
        while (remaining.Count > 0)
        {
            int seed = remaining.First(); remaining.Remove(seed); var group = new List<int> { seed }; var queue = new Queue<int>(); queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int candidate in remaining.Where(i => HorizontalDistance(nodes[current].position, nodes[i].position) <= distance).ToArray())
                { remaining.Remove(candidate); group.Add(candidate); queue.Enqueue(candidate); }
            }
            groups.Add(group.OrderBy(x => x).ToArray());
        }
        return groups;
    }

    static void FitCircle(Node[] nodes, int[] indices, out Vector3 centre, out float radius, out float residual)
    {
        double xx = 0, xz = 0, zz = 0, x = 0, z = 0, one = indices.Length;
        double xb = 0, zb = 0, b = 0;
        foreach (int index in indices)
        {
            double px = nodes[index].position.x, pz = nodes[index].position.z, value = px * px + pz * pz;
            xx += px * px; xz += px * pz; zz += pz * pz; x += px; z += pz; xb += px * value; zb += pz * value; b += value;
        }
        if (!Solve3(xx, xz, x, xz, zz, z, x, z, one, xb, zb, b, out double a, out double c, out double d))
        { centre = Vector3.zero; radius = residual = -1; return; }
        float y = indices.Average(i => nodes[i].position.y);
        centre = new Vector3((float)(a * .5), y, (float)(c * .5));
        radius = Mathf.Sqrt(Mathf.Max(0, (float)(d + a * a * .25 + c * c * .25)));
        Vector3 fittedCentre = centre; float fittedRadius = radius;
        residual = Mathf.Sqrt(indices.Average(i => Mathf.Pow(HorizontalDistance(nodes[i].position, fittedCentre) - fittedRadius, 2)));
    }

    static bool Solve3(double a00, double a01, double a02, double a10, double a11, double a12, double a20, double a21, double a22,
        double b0, double b1, double b2, out double x, out double y, out double z)
    {
        double determinant = a00 * (a11 * a22 - a12 * a21) - a01 * (a10 * a22 - a12 * a20) + a02 * (a10 * a21 - a11 * a20);
        if (Math.Abs(determinant) < 1e-9) { x = y = z = 0; return false; }
        x = (b0 * (a11 * a22 - a12 * a21) - a01 * (b1 * a22 - a12 * b2) + a02 * (b1 * a21 - a11 * b2)) / determinant;
        y = (a00 * (b1 * a22 - a12 * b2) - b0 * (a10 * a22 - a12 * a20) + a02 * (a10 * b2 - b1 * a20)) / determinant;
        z = (a00 * (a11 * b2 - b1 * a21) - a01 * (a10 * b2 - b1 * a20) + b0 * (a10 * a21 - a11 * a20)) / determinant;
        return true;
    }

    static List<int>[] BuildReverseLinks(Node[] nodes)
    {
        var reverse = Enumerable.Range(0, nodes.Length).Select(_ => new List<int>()).ToArray();
        for (int i = 0; i < nodes.Length; i++) foreach (int link in nodes[i].links) reverse[link].Add(i);
        return reverse;
    }

    static Vector3 WeightedCentre(Node[] nodes, int[] cluster)
    {
        float total = 0; Vector3 result = Vector3.zero;
        foreach (int index in cluster) { float weight = Mathf.Max(1, nodes[index].width); result += nodes[index].position * weight; total += weight; }
        return result / total;
    }

    static List<BoundaryConnection> BoundaryConnections(Node[] nodes, List<int>[] reverse, int[] cluster, Vector3 centre)
    {
        var inside = new HashSet<int>(cluster);
        var byExternal = new Dictionary<int, BoundaryConnection>();
        foreach (int inner in cluster)
        {
            foreach (int external in nodes[inner].links)
            {
                if (inside.Contains(external) || nodes[external].component != 0) continue;
                if (!byExternal.TryGetValue(external, out var connection)) byExternal[external] = connection = NewConnection(nodes, inner, external, centre);
                connection.outgoing = true; connection.innerNodes.Add(inner);
            }
            foreach (int external in reverse[inner])
            {
                if (inside.Contains(external) || nodes[external].component != 0) continue;
                if (!byExternal.TryGetValue(external, out var connection)) byExternal[external] = connection = NewConnection(nodes, inner, external, centre);
                connection.incoming = true; connection.innerNodes.Add(inner);
            }
        }
        return byExternal.Values.ToList();
    }

    static BoundaryConnection NewConnection(Node[] nodes, int inner, int external, Vector3 centre)
    {
        Vector3 direction = nodes[external].position - centre; direction.y = 0;
        return new BoundaryConnection
        {
            externalNode = external,
            bearing = Bearing(direction),
            width = Mathf.Max(nodes[inner].width, nodes[external].width),
            speedLimit = Mathf.Max(nodes[inner].speedLimit, nodes[external].speedLimit),
            elevation = nodes[external].position.y,
            innerNodes = new HashSet<int> { inner }
        };
    }

    static List<Arm> GroupArms(Node[] nodes, List<BoundaryConnection> connections, Vector3 centre, float mergeDegrees)
    {
        var groups = connections.OrderBy(c => c.bearing).Select(c => new Arm(c)).ToList();
        bool merged;
        do
        {
            merged = false; float best = float.MaxValue; int bestA = -1, bestB = -1;
            for (int a = 0; a < groups.Count; a++)
            for (int b = a + 1; b < groups.Count; b++)
            {
                float delta = Mathf.Abs(Mathf.DeltaAngle(groups[a].bearing, groups[b].bearing));
                if (delta <= mergeDegrees && delta < best) { best = delta; bestA = a; bestB = b; }
            }
            if (bestA >= 0) { groups[bestA].Merge(groups[bestB]); groups.RemoveAt(bestB); merged = true; }
        } while (merged);

        foreach (var arm in groups)
        {
            arm.nodeLookup = nodes;
            arm.connections.Sort((a, b) => HorizontalDistance(nodes[a.externalNode].position, centre).CompareTo(HorizontalDistance(nodes[b.externalNode].position, centre)));
            arm.width = arm.connections.Max(c => c.width);
            arm.speedLimit = arm.connections.Max(c => c.speedLimit);
            arm.incoming = arm.connections.Any(c => c.incoming);
            arm.outgoing = arm.connections.Any(c => c.outgoing);
            arm.elevation = arm.connections.Average(c => c.elevation);
            arm.externalNodes = arm.connections.Select(c => c.externalNode).Distinct().OrderBy(x => x).ToArray();
            arm.innerNodes = arm.connections.SelectMany(c => c.innerNodes).Distinct().OrderBy(x => x).ToArray();
            arm.approachNode = arm.connections.Where(c => c.incoming).Select(c => c.externalNode).DefaultIfEmpty(-1).First();
            arm.exitNode = arm.connections.Where(c => c.outgoing).Select(c => c.externalNode).DefaultIfEmpty(-1).First();
        }
        return groups;
    }

    static void MarkMajorArms(List<Arm> arms)
    {
        foreach (var arm in arms) arm.major = false;
        if (arms.Count == 0) return;
        if (arms.Count == 1) { arms[0].major = true; return; }
        float best = float.MinValue; Arm first = null, second = null;
        for (int a = 0; a < arms.Count; a++)
        for (int b = a + 1; b < arms.Count; b++)
        {
            float opposite = 1f - Mathf.Abs(180f - Mathf.Abs(Mathf.DeltaAngle(arms[a].bearing, arms[b].bearing))) / 180f;
            float score = opposite * 100f + arms[a].width + arms[b].width + (arms[a].speedLimit + arms[b].speedLimit) * .1f;
            if (score > best) { best = score; first = arms[a]; second = arms[b]; }
        }
        first.major = true; second.major = true;
    }

    static string ClassifyType(Node[] nodes, int[] cluster, List<Arm> arms, ClassifierSettings settings, out string reason)
    {
        if (cluster.Any(i => nodes[i].roundabout))
        {
            if (RoundaboutClosed(nodes, cluster)) { reason = "Roundabout-flagged nodes form a closed internal ring."; return "Roundabout"; }
            reason = "Roundabout flags do not form a closed ring; human override required."; return "Unclassified";
        }
        float elevationRange = arms.Count == 0 ? 0 : arms.Max(a => a.elevation) - arms.Min(a => a.elevation);
        if (elevationRange >= settings.interchangeHeightDifference)
        { reason = $"Arm elevation range {elevationRange:F1} m meets the interchange threshold."; return "Interchange"; }
        if (arms.Count < 2) { reason = "Fewer than two distinct arms."; return "Unclassified"; }
        if (arms.Count == 2) { reason = "Two distinct arms are treated as a merge/slip node, never a signalised junction."; return "Merge/Slip"; }
        float smallestGap = SmallestBearingGap(arms);
        if (arms.Count == 3 && smallestGap < settings.mergeAcuteAngle)
        { reason = $"Three-arm Y/merge geometry has an acute {smallestGap:F1} degree arm separation."; return "Merge/Slip"; }
        if (arms.Count == 3) { reason = "Three distinct approach arms."; return "T"; }
        if (arms.Count == 4) { reason = "Four distinct approach arms."; return "Cross"; }
        if (arms.Count >= 5) { reason = arms.Count + " distinct approach arms."; return "Complex"; }
        reason = "Topology did not match a supported rule."; return "Unclassified";
    }

    static bool RoundaboutClosed(Node[] nodes, int[] cluster)
    {
        var ring = new HashSet<int>(cluster);
        return ring.Count(i => nodes[i].roundabout) >= 3 && ring.All(i => ring.Count(j => i != j && (nodes[i].links.Contains(j) || nodes[j].links.Contains(i))) >= 2);
    }

    static string ClassifyControl(string type, List<Arm> arms, ClassifierSettings settings)
    {
        if (type == "Unclassified") return "Uncontrolled";
        if (type == "Roundabout") return "RoundaboutYield";
        if (type == "Merge/Slip") return "Merge";
        if (type == "Interchange") return "GiveWay";
        if (type == "Complex") return "GiveWay";
        var major = arms.Where(a => a.major).OrderByDescending(a => a.width).Take(2).ToArray();
        if ((type == "Cross" || type == "T") && major.Length == 2 &&
            major.All(a => a.width >= settings.signalMinimumWidth && a.speedLimit >= settings.signalMinimumSpeed)) return "Signalised";
        var minor = arms.Where(a => !a.major).ToArray();
        if ((type == "Cross" || type == "T") && minor.Any() && minor.All(a => a.width < settings.priorityMinorMaximumWidth)) return "Priority";
        return "GiveWay";
    }

    static ArmRecord ToRecord(Arm arm, int index, Vector3 centre, string junctionControl, ClassifierSettings settings)
    {
        int approachNode = arm.approachNode >= 0 ? arm.approachNode : arm.exitNode;
        int exitNode = arm.exitNode >= 0 ? arm.exitNode : arm.approachNode;
        Vector3 approachExternal = approachNode >= 0 ? arm.nodeLookup[approachNode].position : centre;
        Vector3 exitExternal = exitNode >= 0 ? arm.nodeLookup[exitNode].position : approachExternal;
        Vector3 toward = centre - approachExternal; toward.y = 0; toward = toward.sqrMagnitude > .01f ? toward.normalized : Vector3.forward;
        Vector3 approachLeft = -Vector3.Cross(Vector3.up, toward);
        Vector3 exitDirection = exitExternal - centre; exitDirection.y = 0; exitDirection = exitDirection.sqrMagnitude > .01f ? exitDirection.normalized : -toward;
        Vector3 exitLeft = -Vector3.Cross(Vector3.up, exitDirection);
        float laneOffset = Mathf.Min(settings.maximumLaneOffset, Mathf.Max(settings.minimumLaneOffset, arm.width * settings.laneOffsetWidthFactor));
        string armControl = "None";
        if (junctionControl == "Signalised") armControl = "Signal";
        else if (junctionControl == "RoundaboutYield") armControl = "Yield";
        else if (junctionControl == "Merge") armControl = arm.major ? "None" : "Merge";
        else if (!arm.major) armControl = arm.speedLimit > settings.stopMinorSpeedThreshold ? "Stop" : "GiveWay";
        return new ArmRecord
        {
            id = "arm-" + index,
            bearing = Round(arm.bearing), width = Round(arm.width), speedLimit = Round(arm.speedLimit),
            elevation = Round(arm.elevation), major = arm.major, control = armControl,
            incoming = arm.incoming, outgoing = arm.outgoing,
            nodeIds = arm.externalNodes, junctionNodeIds = arm.innerNodes,
            approachNode = arm.approachNode, exitNode = arm.exitNode,
            approachLanePoint = RoundVector(approachExternal + approachLeft * laneOffset),
            exitLanePoint = RoundVector(exitExternal + exitLeft * laneOffset)
        };
    }

    static string Confidence(Node[] nodes, int[] cluster, List<Arm> arms)
    {
        if (arms.Count < 2 || arms.Any(a => !a.incoming && !a.outgoing)) return "Low";
        if (cluster.Any(i => nodes[i].links.Length < 2) || arms.Any(a => !a.incoming || !a.outgoing)) return "Medium";
        return "High";
    }

    static void ApplyOverride(JunctionRecord record, JunctionOverride[] overrides)
    {
        var forced = overrides.FirstOrDefault(x => x != null && x.junctionId == record.id);
        if (forced == null) return;
        if (!string.IsNullOrWhiteSpace(forced.type)) record.type = forced.type;
        if (!string.IsNullOrWhiteSpace(forced.control)) record.control = forced.control;
        if (forced.phasePlan != null && forced.phasePlan.Length > 0) record.phasePlan = forced.phasePlan;
        record.overrideApplied = true;
        record.reason += " Human override applied.";
    }

    static List<AsymmetricLink> FindAsymmetricLinks(Node[] nodes, int[] componentNodes)
    {
        var result = new List<AsymmetricLink>();
        foreach (int from in componentNodes)
        foreach (int to in nodes[from].links)
            if (nodes[to].component == 0 && !nodes[to].links.Contains(from)) result.Add(new AsymmetricLink { from = from, to = to });
        return result;
    }

    static string BuildAudit(Node[] nodes, int[] componentNodes, int[] candidates, List<JunctionRecord> records, List<AsymmetricLink> asymmetric, ClassifierSettings settings)
    {
        var text = new StringBuilder();
        text.AppendLine("KOMBI SHIFT 2 — CITY JUNCTION CLASSIFICATION AUDIT");
        text.AppendLine("Generated UTC: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        text.AppendLine("Source: " + NetworkPath + "; component: 0");
        text.AppendLine($"Component nodes: {componentNodes.Length}; candidate junction/roundabout nodes: {candidates.Length}; clustered junctions: {records.Count}");
        text.AppendLine($"Asymmetric directional links: {asymmetric.Count}");
        text.AppendLine();
        text.AppendLine("COUNTS BY TYPE");
        foreach (var group in records.GroupBy(r => r.type).OrderBy(g => g.Key)) text.AppendLine($"{group.Key}: {group.Count()}");
        text.AppendLine(); text.AppendLine("COUNTS BY CONTROL");
        foreach (var group in records.GroupBy(r => r.control).OrderBy(g => g.Key)) text.AppendLine($"{group.Key}: {group.Count()}");
        text.AppendLine(); text.AppendLine("COUNTS BY CONFIDENCE");
        foreach (var group in records.GroupBy(r => r.confidence).OrderBy(g => g.Key)) text.AppendLine($"{group.Key}: {group.Count()}");

        text.AppendLine(); text.AppendLine("JUNCTIONS WITH FEWER THAN 3 OR MORE THAN 5 ARMS");
        AppendRecords(text, records.Where(r => r.arms.Length < 3 || r.arms.Length > 5));
        text.AppendLine(); text.AppendLine("UNCLASSIFIED JUNCTIONS (LEFT UNCONTROLLED)");
        AppendRecords(text, records.Where(r => r.type == "Unclassified"));
        text.AppendLine(); text.AppendLine("ARMS WITH UNREACHABLE OR MISSING DIRECTED EXITS");
        var unreachable = records.SelectMany(r => r.arms.Where(a => !a.outgoing).Select(a => $"{r.id}/{a.id}: approachNode={a.approachNode}, exitNode={a.exitNode}"));
        AppendLines(text, unreachable);
        text.AppendLine(); text.AppendLine("JUNCTIONS WHOSE DERIVED RADIUS EXCEEDS THEIR WIDEST ARM");
        AppendLines(text, records.Where(r => r.radius > r.widestArm).Select(r => $"{r.id}: radius={r.radius:F1}, widestArm={r.widestArm:F1}, arms={r.arms.Length}"));
        text.AppendLine(); text.AppendLine("ASYMMETRIC LINKS (A LINKS TO B, B DOES NOT LINK TO A)");
        AppendLines(text, asymmetric.Select(x => x.from + " -> " + x.to));
        text.AppendLine(); text.AppendLine("ALL JUNCTIONS");
        foreach (var r in records.OrderBy(r => r.id))
            text.AppendLine($"{r.id}: type={r.type}, control={r.control}, confidence={r.confidence}, arms={r.arms.Length}, nodes=[{string.Join(",", r.nodeIds)}], radius={r.radius:F1}; {r.reason}");
        text.AppendLine();
        text.AppendLine("Thresholds are stored in " + SettingsPath + ". Human corrections belong in " + OverridesPath + ".");
        text.AppendLine("Classification only: no scene signals, signs, traffic behaviour, or rival behaviour were modified.");
        return text.ToString();
    }

    static void AppendRecords(StringBuilder text, IEnumerable<JunctionRecord> records) => AppendLines(text, records.Select(r => $"{r.id}: type={r.type}, control={r.control}, arms={r.arms.Length}, nodes=[{string.Join(",", r.nodeIds)}]"));
    static void AppendLines(StringBuilder text, IEnumerable<string> lines)
    {
        var values = lines.ToArray(); if (values.Length == 0) text.AppendLine("None."); else foreach (string line in values) text.AppendLine(line);
    }
    static float SmallestBearingGap(List<Arm> arms)
    {
        float result = 360;
        for (int a = 0; a < arms.Count; a++) for (int b = a + 1; b < arms.Count; b++) result = Mathf.Min(result, Mathf.Abs(Mathf.DeltaAngle(arms[a].bearing, arms[b].bearing)));
        return result;
    }
    static float Bearing(Vector3 direction) { float value = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg; return value < 0 ? value + 360 : value; }
    static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    static float Round(float value) => (float)Math.Round(value, 3);
    static Vector3 RoundVector(Vector3 value) => new(Round(value.x), Round(value.y), Round(value.z));

    sealed class BoundaryConnection
    {
        public int externalNode; public float bearing, width, speedLimit, elevation; public bool incoming, outgoing; public HashSet<int> innerNodes;
    }
    sealed class Arm
    {
        public readonly List<BoundaryConnection> connections = new();
        public Node[] nodeLookup; public float bearing, width, speedLimit, elevation; public bool incoming, outgoing, major;
        public int[] externalNodes, innerNodes; public int approachNode, exitNode;
        public Arm(BoundaryConnection connection) { connections.Add(connection); bearing = connection.bearing; }
        public void Merge(Arm other)
        {
            connections.AddRange(other.connections);
            float x = connections.Sum(c => Mathf.Sin(c.bearing * Mathf.Deg2Rad));
            float z = connections.Sum(c => Mathf.Cos(c.bearing * Mathf.Deg2Rad));
            bearing = Bearing(new Vector3(x, 0, z));
        }
    }

    [Serializable] sealed class NetworkData { public Node[] nodes; public int components; public string provenance; }
    [Serializable] sealed class Node
    {
        public Vector3 position; public int[] links; public float width = 7, speedLimit = 40; public int component; public bool junction, allowPassing, roundabout;
    }
    [Serializable] public sealed class ClassifierSettings
    {
        public float clusterWidthFactor = .6f;
        public float roundaboutGroupingDistance = 120f;
        public int roundaboutMinimumFlags = 3;
        public float roundaboutRadialTolerance = 7f;
        public float roundaboutMaximumResidualFactor = .15f;
        public float armMergeDegrees = 20f;
        public float mergeAcuteAngle = 45f;
        public float interchangeHeightDifference = 4f;
        public float signalMinimumWidth = 16f;
        public float signalMinimumSpeed = 50f;
        public float priorityMinorMaximumWidth = 10f;
        public float stopMinorSpeedThreshold = 40f;
        public float minimumRadius = 8f;
        public float radiusWidthFactor = .65f;
        public float radiusPerArm = .75f;
        public float laneOffsetWidthFactor = .22f;
        public float minimumLaneOffset = 1.55f;
        public float maximumLaneOffset = 4.65f;
    }
    [Serializable] sealed class JunctionDocument
    {
        public int schemaVersion, component; public string generatedUtc, source; public ClassifierSettings settings; public JunctionRecord[] junctions;
    }
    [Serializable] sealed class JunctionRecord
    {
        public string id, type, control, confidence, reason; public Vector3 centre; public float radius, widestArm; public int[] nodeIds; public ArmRecord[] arms; public string[] phasePlan; public bool overrideApplied;
    }
    [Serializable] sealed class ArmRecord
    {
        public string id, control; public float bearing, width, speedLimit, elevation; public bool major, incoming, outgoing;
        public int[] nodeIds, junctionNodeIds; public int approachNode, exitNode; public Vector3 approachLanePoint, exitLanePoint;
    }
    [Serializable] sealed class OverrideDocument { public JunctionOverride[] overrides; }
    [Serializable] sealed class JunctionOverride { public string junctionId, type, control; public string[] phasePlan; }
    sealed class AsymmetricLink { public int from, to; }
}
