using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Runtime-created primitives get Unity's built-in Standard material,
    // which URP renders as magenta; and Renderer.material.color targets
    // "_Color", which URP/Lit lacks (it uses "_BaseColor"). This helper
    // builds a material on whichever pipeline shader exists and writes the
    // colour to every property a pipeline might use, so primitive colouring
    // (units, cursor tint, ground) works under both URP and Built-in.
    public static class ViewMaterials
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // Reused across SetColor calls so retinting never allocates. GetPropertyBlock
        // refills it from the target renderer each time, so sharing one is safe.
        private static MaterialPropertyBlock sharedBlock;

        public static Material CreateColored(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            ApplyColor(material, color);
            return material;
        }

        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            // Tint via a MaterialPropertyBlock instead of renderer.material: the
            // material getter clones a fresh Material on every access, which leaked
            // one per call in the hot paths that retint constantly (the hit-flash
            // runs every frame; the cursor recolours on every cursor move). The
            // block overrides both pipeline colour properties, same as a material.
            if (sharedBlock == null)
            {
                sharedBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(sharedBlock);
            sharedBlock.SetColor(BaseColorId, color);
            sharedBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(sharedBlock);
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }
        }
    }
}
