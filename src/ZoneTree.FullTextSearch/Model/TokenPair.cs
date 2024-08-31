using System.Runtime.InteropServices;

namespace ZoneTree.FullTextSearch;

/// <summary>
/// Represents a pair of tokens, where each token is linked to its previous token.
/// </summary>
/// <typeparam name="TToken">The type of the token, constrained to unmanaged types.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct TokenPair<TToken> : IEquatable<TokenPair<TToken>> where TToken : unmanaged
{
    /// <summary>
    /// The current token in the pair.
    /// </summary>
    public TToken Token;

    /// <summary>
    /// The token that precedes the current token in the sequence.
    /// </summary>
    public TToken PreviousToken;

    public override bool Equals(object obj)
    {
        return obj is TokenPair<TToken> pair && Equals(pair);
    }

    public bool Equals(TokenPair<TToken> other)
    {
        return EqualityComparer<TToken>.Default.Equals(Token, other.Token) &&
               EqualityComparer<TToken>.Default.Equals(PreviousToken, other.PreviousToken);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Token, PreviousToken);
    }

    public static bool operator ==(TokenPair<TToken> left, TokenPair<TToken> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TokenPair<TToken> left, TokenPair<TToken> right)
    {
        return !(left == right);
    }
}
