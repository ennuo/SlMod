namespace SlLib.Enums;

[Flags]
public enum CollisionFlags
{
    // there's a lot more, have to get them at some point,
    // but these are the basic two you really need for simple stuff.
    Land = 1,
    Wall = 4
}