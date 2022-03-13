using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Veilheim.Patches
{
    public static class GhostRotationTranspiler
    {
        public static void PatchUpdatePlacementGhost()
        {
            IL.Player.UpdatePlacementGhost += Player_UpdatePlacementGhost;
        }

        private static void Player_UpdatePlacementGhost(MonoMod.Cil.ILContext il)
        {
            foreach (var instr in il.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldc_R4)
                {
                    if (instr.Operand is float fl)
                    {
                        if (fl == 22.5f)
                        {
                            fl = 11.25f;
                            instr.Operand = fl;
                            Jotunn.Logger.LogInfo("Found placement ghost angle");
                            break;
                        }
                    }
                }
            }
        }

    }
}