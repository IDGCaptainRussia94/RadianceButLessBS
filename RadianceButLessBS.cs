using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using Newtonsoft.Json.Linq;
using Radiance;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using static Radiance.Config;
using Terraria.ModLoader.Config;

namespace RadianceButLessBS
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class RadianceButLessBS : Mod
	{
        //nill
        public static Mod instance;
        public static ILHook patchRuin;
        public static ILHook patchGravity;
        public static ILHook patchArmor;

        public override void Load()
        {
            instance = this;
            ApplyPatches();
        }
        public override void Unload()
        {
            if (patchRuin != null)
                patchRuin.Undo();

            if (patchGravity != null)
                patchGravity.Undo();

            if (patchArmor != null)
                patchArmor.Undo();

        }
        public static void ApplyPatches()
        {
            patchRuin = new ILHook(typeof(Radiance.RuinDamageModel).GetMethod("ApplyRuin"), il => RemoveRuinAdd(il));
            patchGravity = new ILHook(typeof(Radiance.GravityComponent).GetMethod("PreUpdateMovement"), il => RemoveGravityOrSoftcap(il,false));
            patchArmor = new ILHook(typeof(Radiance.BossArmorComponent).GetMethod("ResistDamage"), il => RemoveGravityOrSoftcap(il, true));
            //HookEndpointManager.Modify(typeof(Radiance.RuinDamageModel.add).GetMethod("PostUpdateEquips", SGAmod.UniversalBindingFlags), (Delegate)(object)value);
            //ILHook.
            patchRuin.Apply();
            patchGravity.Apply();
            patchArmor.Apply();
            RadianceButLessBS.instance.Logger.Debug("Did we patch it?");
        }

        private static void RemoveRuinAdd(ILContext context)
        {
            ILCursor c = new ILCursor(context);
            ILLabel label = c.DefineLabel();
            c.Emit(OpCodes.Ldarg, 1);
            c.Emit(OpCodes.Ldarg, 2);
            c.EmitDelegate<Func<int, int, bool>>((int originalDamage, int finalDamage) => {
                RadianceButLessBS.instance.Logger.Debug("It seems we got hit: Damage "+originalDamage+" || Final damage: "+finalDamage);
                return RadWorld.DisableRuin;
            });
            c.Emit(OpCodes.Brfalse, label);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(label);
            RadianceButLessBS.instance.Logger.Debug("Hmmm...");
        }

        private static void RemoveGravityOrSoftcap(ILContext context, bool softCap)
        {
            ILCursor c = new ILCursor(context);
            ILLabel label = c.DefineLabel();

            c.EmitDelegate<Func<bool>>(() => {
                if (softCap)
                    return RadWorld.DisableArmor;
                return RadWorld.DisableGravity;
            });

            c.Emit(OpCodes.Brfalse, label);

            if (softCap)
            {
                c.Emit(OpCodes.Ldarg_1);//Return Damage
                c.Emit(OpCodes.Ret);
            }
            else
            {
                c.Emit(OpCodes.Ret);
            }

            c.MarkLabel(label);
            RadianceButLessBS.instance.Logger.Debug((softCap ? "Softcap" : "Gravity") +" removed?...");
        }

    }

    public class RadWorld : ModConfig
    {
        private static RadWorld instance;

        public static RadWorld Instance => instance;

        public bool disableRuin;
        public bool disableGravity;
        public bool disableArmor;

        public override ConfigScope Mode => ConfigScope.ServerSide;

        public static bool DisableRuin => Instance.disableRuin;

        public static bool DisableGravity => Instance.disableGravity;

        public static bool DisableArmor => Instance.disableArmor;

        public override void OnLoaded()
        {
            RadWorld.instance = this;
        }

        public override void OnChanged()
        {
            
        }
    }
}
