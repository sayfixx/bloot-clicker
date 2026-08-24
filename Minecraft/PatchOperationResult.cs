using System;

namespace Autoclicker.Minecraft
{
    internal sealed class PatchOperationResult : IEquatable<PatchOperationResult>
    {
        public PatchOperationResult(bool success, string statusText)
        {
            Success = success;
            StatusText = statusText;
        }

        public bool Success { get; set; }

        public string StatusText { get; set; }

        public override string ToString()
        {
            return $"PatchOperationResult {{ Success = {Success}, StatusText = {StatusText} }}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PatchOperationResult);
        }

        public bool Equals(PatchOperationResult other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Success == other.Success
                && StatusText == other.StatusText;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(PatchOperationResult).GetHashCode();
                hash = hash * -1521134295 + Success.GetHashCode();
                hash = hash * -1521134295 + (StatusText?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(PatchOperationResult left, PatchOperationResult right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PatchOperationResult left, PatchOperationResult right)
        {
            return !(left == right);
        }

        public void Deconstruct(out bool success, out string statusText)
        {
            success = Success;
            statusText = StatusText;
        }
    }
}
