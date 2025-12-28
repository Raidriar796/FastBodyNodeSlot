using ResoniteModLoader;
using HarmonyLib;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using FrooxEngine.ProtoFlux;
using Elements.Core;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
using Renderite.Shared;

namespace FastBodyNodeSlot;

public partial class FastBodyNodeSlot : ResoniteMod
{
    private static Slot CustomGetBodyNodeSlot(User user, BodyNode node)
    {
        // Recreation of the original GetBodyNodeSlot method
        Slot slot;

	      if (user == null)
	      {
		        slot = null!;
	      }
	      else
	      {
		        UserRoot root2 = user.Root;
		        slot = (root2 != null) ? root2.Slot : null!;
	      }

	      Slot root = slot;

	      if (root == null)
	      {
		        return null!;
	      }

	      if (node == BodyNode.NONE)
	      {
		        return null!;
	      }

        BipedRig componentInChildren = root.GetComponentInChildren<BipedRig>(null!, false, false);
	      Slot bone = (componentInChildren != null) ? componentInChildren.TryGetBone(node) : null!;

	      if (bone != null)
	      {
		        return bone;
	      }

	      AvatarObjectSlot avatarSlot = root.FindSlotForNodeInChildren(node);

	      if (avatarSlot != null)
	      {
		        return avatarSlot.Slot;
	      }

	      return null!;
    }
}
