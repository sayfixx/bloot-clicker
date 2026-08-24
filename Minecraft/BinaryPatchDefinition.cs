using System;

namespace Autoclicker.Minecraft
{
    internal sealed class BinaryPatchDefinition : IEquatable<BinaryPatchDefinition>
    {
        public BinaryPatchDefinition(string id, string signature, byte[] replacement)
        {
            Id = id;
            Signature = signature;
            Replacement = replacement;
        }

        public string Id { get; set; }

        public string Signature { get; set; }

        public byte[] Replacement { get; set; }

        public override string ToString()
        {
            return $"BinaryPatchDefinition {{ Id = {Id}, Signature = {Signature}, Replacement = {Replacement} }}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BinaryPatchDefinition);
        }

        public bool Equals(BinaryPatchDefinition other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id
                && Signature == other.Signature
                && Equals(Replacement, other.Replacement);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(BinaryPatchDefinition).GetHashCode();
                hash = hash * -1521134295 + (Id?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + (Signature?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + (Replacement?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(BinaryPatchDefinition left, BinaryPatchDefinition right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(BinaryPatchDefinition left, BinaryPatchDefinition right)
        {
            return !(left == right);
        }

        public void Deconstruct(out string id, out string signature, out byte[] replacement)
        {
            id = Id;
            signature = Signature;
            replacement = Replacement;
        }
    }
}
