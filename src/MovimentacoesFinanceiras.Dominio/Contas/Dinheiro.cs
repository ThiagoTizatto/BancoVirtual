using System.Globalization;

namespace MovimentacoesFinanceiras.Dominio.Contas;

public sealed class Dinheiro : IEquatable<Dinheiro>, IComparable<Dinheiro>
{
    public decimal Quantia { get; }

    private Dinheiro(decimal quantia) => Quantia = quantia;

    public static Dinheiro De(decimal quantia)
    {
        if (quantia <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.", nameof(quantia));
        return new Dinheiro(quantia);
    }

    public static Dinheiro Zero { get; } = new(0m);

    internal static Dinheiro Reconstituir(decimal quantia) => new(quantia);

    // Friendly names exigidos por CA2225 para os operadores + e -
    public Dinheiro Add(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return new Dinheiro(Quantia + outro.Quantia);
    }

    public Dinheiro Subtract(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return new Dinheiro(Quantia - outro.Quantia);
    }

    // Aliases em portugues para manter a linguagem ubiqua do dominio
    public Dinheiro Somar(Dinheiro outro) => Add(outro);

    public Dinheiro Subtrair(Dinheiro outro) => Subtract(outro);

    public static Dinheiro operator +(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Add(right);
    }

    public static Dinheiro operator -(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Subtract(right);
    }

    public static bool operator >(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Quantia > right.Quantia;
    }

    public static bool operator <(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Quantia < right.Quantia;
    }

    public static bool operator >=(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Quantia >= right.Quantia;
    }

    public static bool operator <=(Dinheiro left, Dinheiro right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Quantia <= right.Quantia;
    }

    public int CompareTo(Dinheiro? other) => other is null ? 1 : Quantia.CompareTo(other.Quantia);

    public bool Equals(Dinheiro? other) => other is not null && Quantia == other.Quantia;
    public override bool Equals(object? obj) => obj is Dinheiro d && Equals(d);
    public override int GetHashCode() => Quantia.GetHashCode();
    public override string ToString() => $"R$ {Quantia.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}";

    public static bool operator ==(Dinheiro? left, Dinheiro? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Dinheiro? left, Dinheiro? right) => !(left == right);
}
