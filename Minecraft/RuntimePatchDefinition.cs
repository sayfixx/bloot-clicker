using System;

namespace Autoclicker.Minecraft
{
    internal sealed class RuntimePatchDefinition : IEquatable<RuntimePatchDefinition>
    {
        public RuntimePatchDefinition(string id, string signature, int patchOffset, PatchApplyKind applyKind, string replacementHex = "", int length = 0, float floatValue = 0f)
        {
            Id = id;
            Signature = signature;
            PatchOffset = patchOffset;
            ApplyKind = applyKind;
            ReplacementHex = replacementHex;
            Length = length;
            FloatValue = floatValue;
        }

        public string Id { get; set; }

        public string Signature { get; set; }

        public int PatchOffset { get; set; }

        public PatchApplyKind ApplyKind { get; set; }

        public string ReplacementHex { get; set; }

        public int Length { get; set; }

        public float FloatValue { get; set; }

        public override string ToString()
        {
            return $"RuntimePatchDefinition {{ Id = {Id}, Signature = {Signature}, PatchOffset = {PatchOffset}, ApplyKind = {ApplyKind}, ReplacementHex = {ReplacementHex}, Length = {Length}, FloatValue = {FloatValue} }}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuntimePatchDefinition);
        }

        public bool Equals(RuntimePatchDefinition other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id
                && Signature == other.Signature
                && PatchOffset == other.PatchOffset
                && ApplyKind == other.ApplyKind
                && ReplacementHex == other.ReplacementHex
                && Length == other.Length
                && FloatValue == other.FloatValue;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(RuntimePatchDefinition).GetHashCode();
                hash = hash * -1521134295 + (Id?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + (Signature?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + PatchOffset.GetHashCode();
                hash = hash * -1521134295 + ApplyKind.GetHashCode();
                hash = hash * -1521134295 + (ReplacementHex?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + Length.GetHashCode();
                hash = hash * -1521134295 + FloatValue.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(RuntimePatchDefinition left, RuntimePatchDefinition right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(RuntimePatchDefinition left, RuntimePatchDefinition right)
        {
            return !(left == right);
        }

        public void Deconstruct(out string id, out string signature, out int patchOffset, out PatchApplyKind applyKind, out string replacementHex, out int length, out float floatValue)
        {
            id = Id;
            signature = Signature;
            patchOffset = PatchOffset;
            applyKind = ApplyKind;
            replacementHex = ReplacementHex;
            length = Length;
            floatValue = FloatValue;
        }
    }
}
