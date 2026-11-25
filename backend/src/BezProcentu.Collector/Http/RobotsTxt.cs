namespace BezProcentu.Collector.Http;

/// <summary>Minimal robots.txt evaluator: groups for "*" and our own agent token, Allow/Disallow with * and $.</summary>
public sealed class RobotsTxt
{
    private readonly List<(bool Allow, string Pattern)> _rules = [];

    public static RobotsTxt AllowAll { get; } = new();

    public static RobotsTxt Parse(string content, string agentToken)
    {
        var specific = new List<(bool, string)>();
        var generic = new List<(bool, string)>();
        List<string> currentAgents = [];
        var lastWasAgent = false;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Split('#')[0].Trim();
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim().ToLowerInvariant();
            var value = line[(colon + 1)..].Trim();

            if (key == "user-agent")
            {
                if (!lastWasAgent)
                {
                    currentAgents = [];
                }

                currentAgents.Add(value.ToLowerInvariant());
                lastWasAgent = true;
                continue;
            }

            lastWasAgent = false;
            if (key is not ("allow" or "disallow"))
            {
                continue;
            }

            if (key == "disallow" && value.Length == 0)
            {
                continue; // empty Disallow means allow everything
            }

            var rule = (key == "allow", value);
            if (currentAgents.Contains(agentToken.ToLowerInvariant()))
            {
                specific.Add(rule);
            }
            else if (currentAgents.Contains("*"))
            {
                generic.Add(rule);
            }
        }

        var result = new RobotsTxt();
        result._rules.AddRange(specific.Count > 0 ? specific : generic);
        return result;
    }

    /// <summary>Longest matching rule wins; Allow wins ties (RFC 9309).</summary>
    public bool IsAllowed(Uri uri)
    {
        var path = uri.PathAndQuery;
        var best = -1;
        var allowed = true;
        foreach (var (allow, pattern) in _rules)
        {
            if (Matches(path, pattern) && (pattern.Length > best || (pattern.Length == best && allow)))
            {
                best = pattern.Length;
                allowed = allow;
            }
        }

        return allowed;
    }

    private static bool Matches(string path, string pattern)
    {
        var anchored = pattern.EndsWith('$');
        var p = anchored ? pattern[..^1] : pattern;
        return Match(path, 0, p, 0, anchored);
    }

    private static bool Match(string s, int si, string p, int pi, bool anchored)
    {
        while (pi < p.Length)
        {
            if (p[pi] == '*')
            {
                for (var k = si; k <= s.Length; k++)
                {
                    if (Match(s, k, p, pi + 1, anchored))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (si >= s.Length || s[si] != p[pi])
            {
                return false;
            }

            si++;
            pi++;
        }

        return !anchored || si == s.Length;
    }
}
