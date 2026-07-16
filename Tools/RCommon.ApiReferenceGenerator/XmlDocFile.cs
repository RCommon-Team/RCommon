using System.Xml.Linq;

namespace RCommon.ApiReferenceGenerator;

internal sealed class XmlDocMember
{
    public string? Summary { get; set; }
    public string? Returns { get; set; }
    public List<(string Name, string Text)> Params { get; } = new();
    public List<string> Exceptions { get; } = new();
}

/// <summary>
/// Parses a compiler-generated .xml doc-comment sidecar into a lookup keyed by the same member ID
/// format the compiler emits (e.g. <c>M:Namespace.Type.Member(ParamType)</c>).
/// </summary>
internal static class XmlDocFile
{
    public static Dictionary<string, XmlDocMember> Load(string xmlPath)
    {
        var result = new Dictionary<string, XmlDocMember>();
        var doc = XDocument.Load(xmlPath);
        var members = doc.Root?.Element("members")?.Elements("member");
        if (members == null)
        {
            return result;
        }

        foreach (var memberElement in members)
        {
            var name = memberElement.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var entry = new XmlDocMember
            {
                Summary = CleanElement(memberElement.Element("summary")),
                Returns = CleanElement(memberElement.Element("returns")),
            };

            foreach (var paramElement in memberElement.Elements("param"))
            {
                var paramName = paramElement.Attribute("name")?.Value ?? string.Empty;
                entry.Params.Add((paramName, CleanElement(paramElement) ?? string.Empty));
            }

            foreach (var exceptionElement in memberElement.Elements("exception"))
            {
                var cref = exceptionElement.Attribute("cref")?.Value ?? string.Empty;
                // cref values look like "T:System.ArgumentNullException" -- strip the "T:" prefix.
                var typeName = cref.StartsWith("T:", StringComparison.Ordinal) ? cref[2..] : cref;
                entry.Exceptions.Add(typeName);
            }

            result[name] = entry;
        }

        return result;
    }

    /// <summary>
    /// Renders an XML doc-comment element (e.g. &lt;summary&gt;) to plain text, resolving inline
    /// &lt;see cref="..."/&gt;, &lt;paramref name="..."/&gt;, &lt;typeparamref name="..."/&gt;, and
    /// &lt;c&gt;...&lt;/c&gt; elements to their referenced name/text instead of dropping them -- a
    /// plain <c>.Value</c> read discards every cref/name attribute, since those elements have no text
    /// content of their own.
    /// </summary>
    private static string? CleanElement(XElement? element)
    {
        if (element == null)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    sb.Append(text.Value);
                    break;
                case XElement el when el.Name.LocalName is "see" or "seealso":
                    sb.Append(CrefToName(el.Attribute("cref")?.Value) ?? el.Value);
                    break;
                case XElement el when el.Name.LocalName is "paramref" or "typeparamref":
                    sb.Append(el.Attribute("name")?.Value ?? el.Value);
                    break;
                case XElement el:
                    // <c>, <para>, or anything else -- fall back to its own text content.
                    sb.Append(el.Value);
                    break;
            }
        }

        var lines = sb.ToString().Split('\n').Select(l => l.Trim());
        var joined = string.Join(' ', lines.Where(l => l.Length > 0));
        return joined.Length > 0 ? joined : null;
    }

    /// <summary>
    /// Converts a cref attribute value (e.g. <c>T:Namespace.Type</c>, <c>M:Namespace.Type.Member</c>)
    /// to a short, readable name -- the trailing member/type name rather than the full member-ID.
    /// </summary>
    private static string? CrefToName(string? cref)
    {
        if (string.IsNullOrEmpty(cref))
        {
            return null;
        }

        // Strip the "T:"/"M:"/"P:"/"F:"/"E:" prefix, then take the last dotted segment so
        // "M:RCommon.Security.Claims.TenantScope.Bypass" reads as "TenantScope.Bypass".
        var withoutKind = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
        var parenIndex = withoutKind.IndexOf('(');
        if (parenIndex >= 0)
        {
            withoutKind = withoutKind[..parenIndex];
        }

        var segments = withoutKind.Split('.');
        return segments.Length >= 2 ? segments[^2] + "." + segments[^1] : withoutKind;
    }
}
