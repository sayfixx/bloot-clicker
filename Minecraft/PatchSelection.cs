using System;

namespace Autoclicker.Minecraft
{
    internal sealed class PatchSelection : IEquatable<PatchSelection>
    {
        public PatchSelection(bool noHurtCam, bool guiScale, bool teleportRotation, bool delayFix,
            bool minimalViewBobbing, bool itemUseDelay, bool thirdPersonNametag, bool playScreenFix = false)
        {
            NoHurtCam = noHurtCam;
            GuiScale = guiScale;
            TeleportRotation = teleportRotation;
            DelayFix = delayFix;
            MinimalViewBobbing = minimalViewBobbing;
            ItemUseDelay = itemUseDelay;
            ThirdPersonNametag = thirdPersonNametag;
            PlayScreenFix = playScreenFix;
        }

        public bool NoHurtCam { get; set; }
        public bool GuiScale { get; set; }
        public bool TeleportRotation { get; set; }
        public bool DelayFix { get; set; }
        public bool MinimalViewBobbing { get; set; }
        public bool ItemUseDelay { get; set; }
        public bool ThirdPersonNametag { get; set; }
        public bool PlayScreenFix { get; set; }

        public override string ToString()
        {
            return $"PatchSelection {{ NoHurtCam={NoHurtCam}, GuiScale={GuiScale}, TeleportRotation={TeleportRotation}, DelayFix={DelayFix}, MinimalViewBobbing={MinimalViewBobbing}, ItemUseDelay={ItemUseDelay}, ThirdPersonNametag={ThirdPersonNametag}, PlayScreenFix={PlayScreenFix} }}";
        }

        public override bool Equals(object obj) => Equals(obj as PatchSelection);

        public bool Equals(PatchSelection other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return NoHurtCam == other.NoHurtCam
                && GuiScale == other.GuiScale
                && TeleportRotation == other.TeleportRotation
                && DelayFix == other.DelayFix
                && MinimalViewBobbing == other.MinimalViewBobbing
                && ItemUseDelay == other.ItemUseDelay
                && ThirdPersonNametag == other.ThirdPersonNametag
                && PlayScreenFix == other.PlayScreenFix;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = typeof(PatchSelection).GetHashCode();
                hash = hash * -1521134295 + NoHurtCam.GetHashCode();
                hash = hash * -1521134295 + GuiScale.GetHashCode();
                hash = hash * -1521134295 + TeleportRotation.GetHashCode();
                hash = hash * -1521134295 + DelayFix.GetHashCode();
                hash = hash * -1521134295 + MinimalViewBobbing.GetHashCode();
                hash = hash * -1521134295 + ItemUseDelay.GetHashCode();
                hash = hash * -1521134295 + ThirdPersonNametag.GetHashCode();
                hash = hash * -1521134295 + PlayScreenFix.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(PatchSelection left, PatchSelection right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PatchSelection left, PatchSelection right) => !(left == right);
    }
}
