using System;

public readonly struct ContentId : IEquatable<ContentId>
{
    public const string DefaultNamespace = "cubeworld";

    public string Namespace { get; }
    public string Name { get; }

    public ContentId(string ns, string name)
    {
        Namespace = string.IsNullOrWhiteSpace(ns) ? DefaultNamespace : ns;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public static ContentId Cubeworld(string name) => new(DefaultNamespace, name);

    public static bool TryParse(string value, out ContentId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separator = value.IndexOf(':');
        if (separator < 0)
        {
            id = Cubeworld(value.Trim());
            return true;
        }

        var ns = value.Substring(0, separator).Trim();
        var name = value.Substring(separator + 1).Trim();
        if (name.Length == 0)
        {
            return false;
        }

        id = new ContentId(ns, name);
        return true;
    }

    public bool Equals(ContentId other) =>
        string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is ContentId other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Namespace, Name);

    public override string ToString() => $"{Namespace}:{Name}";
}
