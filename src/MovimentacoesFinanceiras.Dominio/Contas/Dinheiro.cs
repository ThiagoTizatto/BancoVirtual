using System.Globalization;

namespace MovimentacoesFinanceiras.Dominio.Contas;

public sealed class Dinheiro : IEquatable<Dinheiro>
{
    public decimal Quantia { get; }

    private Dinheiro(decimal quantia) => Quantia = quantia;

    public static Dinheiro De(decimal quantia)
    {
        if (quantia <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.", nameof(quantia));
        return new Dinheiro(quantia);
    }

    public bool Equals(Dinheiro? other) => other is not null && Quantia == other.Quantia;
    public override bool Equals(object? obj) => obj is Dinheiro d && Equals(d);
    public override int GetHashCode() => Quantia.GetHashCode();
    public override string ToString() => $"R$ {Quantia.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}";

    public static bool operator ==(Dinheiro? left, Dinheiro? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Dinheiro? left, Dinheiro? right) => !(left == right);
}
