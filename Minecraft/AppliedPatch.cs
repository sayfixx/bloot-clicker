using System;

namespace Autoclicker.Minecraft
{
    internal sealed class AppliedPatch : IEquatable<AppliedPatch>
    {
        public AppliedPatch(int processId, RuntimePatchDefinition definition, IntPtr address, byte[] originalBytes)
        {
            ProcessId = processId;
            Definition = definition;
            Address = address;
            OriginalBytes = originalBytes;
        }

        public int ProcessId { get; set; }

        public RuntimePatchDefinition Definition { get; set; }

        public IntPtr Address { get; set; }

        public byte[] OriginalBytes { get; set; }

        public override string ToString()
        {
            return $"AppliedPatch {{ ProcessId = {ProcessId}, Definition = {Definition}, Address = {Address}, OriginalBytes = {OriginalBytes} }}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AppliedPatch);
        }

        public bool Equals(AppliedPatch other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProcessId == other.ProcessId
                && Equals(Definition, other.Definition)
                && Address == other.Address
                && Equals(OriginalBytes, other.OriginalBytes);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(AppliedPatch).GetHashCode();
                hash = hash * -1521134295 + ProcessId.GetHashCode();
                hash = hash * -1521134295 + (Definition?.GetHashCode() ?? 0);
                hash = hash * -1521134295 + Address.GetHashCode();
                hash = hash * -1521134295 + (OriginalBytes?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(AppliedPatch left, AppliedPatch right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(AppliedPatch left, AppliedPatch right)
        {
            return !(left == right);
        }

        public void Deconstruct(out int processId, out RuntimePatchDefinition definition, out IntPtr address, out byte[] originalBytes)
        {
            processId = ProcessId;
            definition = Definition;
            address = Address;
            originalBytes = OriginalBytes;
        }
    }
}
