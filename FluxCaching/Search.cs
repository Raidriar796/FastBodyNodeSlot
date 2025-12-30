using ResoniteModLoader;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
using Renderite.Shared;

namespace FluxCaching;

public partial class FluxCaching : ResoniteMod
{
    private static Slot CustomGetBodyNodeSlot(BodyNodeSlot instance, User user, BodyNode node)
    {
        CachedResults cachedResults;
        BipedRig bipedRig;
        AvatarObjectSlot avatarObjectSlot = null!;

        // Returns early if the dictionary does not have the BodyNodeSlot tracked yet
        if (CachedBodyNodeSlots.ContainsKey(instance))
        {
            cachedResults = CachedBodyNodeSlots[instance];
        }
        else
        {
            return null!;
        }

        // Recreation of the original GetBodyNodeSlot method's null checking
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

        // Stores for the first time the biped rig is searched to avoid searching again if it's null
        if (!cachedResults.IsBipedRigSearched)
        {
            cachedResults.CachedBipedRig = root.GetComponentInChildren<BipedRig>();
            bipedRig = cachedResults.CachedBipedRig;
            CachedBodyNodeSlots[instance].CachedBipedRig = cachedResults.CachedBipedRig;
            CachedBodyNodeSlots[instance].IsBipedRigSearched = true;
        }
        else if (cachedResults.CachedBipedRig == null)
        {
            bipedRig = null!;
        }
        else if (cachedResults.CachedBipedRig.IsDestroyed)
        {
            cachedResults.CachedBipedRig = root.GetComponentInChildren<BipedRig>();
            bipedRig = cachedResults.CachedBipedRig;
            CachedBodyNodeSlots[instance].CachedBipedRig = cachedResults.CachedBipedRig;
        }
        else
        {
            bipedRig = cachedResults.CachedBipedRig;
        }

	      Slot bone = (bipedRig != null) ? bipedRig.TryGetBone(node) : null!;

	      if (bone != null)
	      {
		        return bone;
	      }

        // Check if any previously stored AvatarObjectSlot were destroyed
        // If so, clear the dictionary and start over
        foreach (BodyNode nodeKey in cachedResults.SearchedBodyNodes.Keys)
        {
            if (cachedResults.SearchedBodyNodes[nodeKey] != null)
            {
                if (cachedResults.SearchedBodyNodes[nodeKey].IsDestroyed)
                {
                    cachedResults.SearchedBodyNodes.Clear();
                    CachedBodyNodeSlots[instance].SearchedBodyNodes.Clear();
                    break;
                }
            }
        }

        // Check if the body node being searched has been searched already,
        // cache it if it hasn't, reuse the cached results if it has.
        if (!cachedResults.SearchedBodyNodes.ContainsKey(node))
        {
            avatarObjectSlot = root.FindSlotForNodeInChildren(node);
            CachedBodyNodeSlots[instance].SearchedBodyNodes.Add(node, avatarObjectSlot);
        }
        else
        {
            avatarObjectSlot = cachedResults.SearchedBodyNodes[node];
        }

	      if (avatarObjectSlot != null)
	      {
		        return avatarObjectSlot.Slot;
	      }

	      return null!;
    }
}
