using FrooxEngine;
using FrooxEngine.CommonAvatar;
using ProtoFlux.Runtimes.Execution.Nodes.FrooxEngine.Avatar;
using Renderite.Shared;
using ResoniteModLoader;

namespace FluxCaching;

public partial class FluxCaching : ResoniteMod
{
    public partial class BodyNodeSlotCaching
    {
        // Used to clear the slot cache when it becomes invalid
        private static void ClearCache(BodyNodeSlot instance, User user, BodyNode node)
        {
            // SOME form of clearing no longer used entries in the main dictionary,
            // hopefully this is replaced later, there's likely a better way to do this
            foreach (BodyNodeSlot bodyNodeSlot in CachedBodyNodeSlots.Keys)
            {
                if (CachedBodyNodeSlots[bodyNodeSlot] == null)
                   CachedBodyNodeSlots.Remove(bodyNodeSlot);
            }

            if (!CachedBodyNodeSlots.ContainsKey(instance)) return;

            CachedBodyNodeSlots[instance].cache = new(instance, user, node);
        }

        // If at any point a cache invalidation or other update occured, run the usual logic to fetch the body node slot
        // Additionally, events will be assigned to limit per update validation and to allow events to handle cache invalidation
        private static Slot GetSlotAndAssignEvents(BodyNodeSlot instance, User user, BodyNode node)
        {
            if (!CachedBodyNodeSlots.ContainsKey(instance)) return null!;

            Data data = CachedBodyNodeSlots[instance];
            Cache cache = data.cache;
            HashSets hashSets = data.hashSets;

            Slot slot = CustomGetBodyNodeSlot(instance, user, node);
            cache.CachedSlot = slot;
            cache.IsBodyNodeSearched = true;

            // Subscribe the found slot and all of it's parents up to the user root if they haven't been already
            if (slot != null)
            {
                ICollection<Slot> parentCollection = [];
                slot.GetAllParents(parentCollection, true);
                foreach (Slot tempSlot in parentCollection)
                {
                    if (hashSets.SubscribedSlots.Add(tempSlot))
                    {
                        tempSlot.Destroyed += (s) => { ClearCache(instance, user, node); };
                        tempSlot.ParentChanged += (s) => { ClearCache(instance, user, node); };
                    }

                    if (tempSlot == user.Root.Slot) break;
                }
            }

            return slot!;
        }

        // Checks cached data for changes that cannot be assigned to events
        private static Slot CheckForChanges(BodyNodeSlot instance, User user, BodyNode node)
        {
            Cache cache;
            HashSets hashSets;
            bool shouldUpdate = false;

            // Probably overkill null checks to exit early incase any of these are true
            if (user == null || user.IsDestroyed ||
                user.Root == null || user.Root.IsDestroyed ||
                user.Root.Slot == null || user.Root.Slot.IsDestroyed)
            {
                return null!;
            }

            // Creates a new Cache instance and adds it to the dictionary with the instance as the key
            if (!CachedBodyNodeSlots.ContainsKey(instance))
            {
                cache = new(instance, user, node);
                hashSets = new();
                Data data = new(cache, hashSets);
                CachedBodyNodeSlots.Add(instance, data);
            }
            // If the key already exists, simply reuse it
            else
            {
                Data data = CachedBodyNodeSlots[instance];
                cache = data.cache;
                hashSets = data.hashSets;
            }

            Slot slot = cache.CachedSlot!;

            // Caches the slot if it hasn't been searched for
            // Checking against a bool to not search again if the slot returns null
            if (!cache.IsBodyNodeSearched) shouldUpdate = true;
            
            // Reassigns the user if the cached user doesn't match
            if (cache.CachedUser != user)
            {
                cache.CachedUser = user;
                shouldUpdate = true;
            }

            // Reassigns the BodyNode if the cached BodyNode doesn't match
            if (cache.CachedNode != node)
            {
                cache.CachedNode = node;
                shouldUpdate = true;
            }
            
            // Assigns the user's avatar object slot if it's not been assigned already
            if (cache.CachedAvatarObjectSlot == null)
            {
                cache.CachedAvatarObjectSlot = user.Root.Slot.GetComponent<AvatarObjectSlot>();

                if (cache.CachedAvatarObjectSlot != null)
                {
                    cache.CachedAvatarObjectSlot = cache.CachedAvatarObjectSlot;

                    // Prevents resubscribing previously cached AvatarObjectSlots
                    if (hashSets.SubscribedAvatarObjectSlots.Add(cache.CachedAvatarObjectSlot))
                    {
                        cache.CachedAvatarObjectSlot.Equipped.OnValueChange += (v) => { ClearCache(instance, user, node); };
                        cache.CachedAvatarObjectSlot.Destroyed += (v) => { ClearCache(instance, user, node); };
                    }

                    shouldUpdate = true;
                }
            }

            if (shouldUpdate) return GetSlotAndAssignEvents(instance, user, node);

            return slot!;
        }
    }
}
