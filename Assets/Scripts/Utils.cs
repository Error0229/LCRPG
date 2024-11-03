using System;

public enum Side
{
    White,
    Black
}

public enum Role
{
    Attacker,
    Defender,
    None
}


public enum PieceType
{
    King,
    Queen,
    Bishop,
    Knight,
    Rook,
    Pawn
}

public static class StringExtensions
{
    public static PieceType ToPieceType(this string pieceName)
    {
        if (Enum.TryParse(pieceName, true, out PieceType result)) return result;
        throw new ArgumentException($"Invalid piece name: {pieceName}");
    }
}


public static class RoleExtensions
{
    public static Role Switch(this Role role)
    {
        return role == Role.Attacker ? Role.Defender : Role.Attacker;
    }
}

public static class SideExtensions
{
    public static string ToAbbreviation(this Side side)
    {
        return side == Side.Black ? "B" : "W";
    }
}