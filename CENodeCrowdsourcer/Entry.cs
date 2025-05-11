using System.Numerics;

namespace CENodeCrowdsourcer;

public class Entry
{
    public Vector3 Position;

    public override bool Equals(object? obj)
    {
        return obj is Entry other && Position.Equals(other.Position);
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}
