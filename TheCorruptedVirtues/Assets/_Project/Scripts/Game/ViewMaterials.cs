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
            if (renderer != null)
            {
                ApplyColor(renderer.material, color);
            }
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
