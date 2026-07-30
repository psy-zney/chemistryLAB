using UnityEngine;
using UnityEngine.Rendering;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Builds original low-cost laboratory props from geometric primitives.
    /// Downloaded reference models inform function and proportion only; none of
    /// their meshes, materials, textures, or topology are copied here.
    /// </summary>
    public static class ProceduralLabPropFactory
    {
        public static GameObject CreateHotplateStirrer(
            Transform parent,
            Vector3 localPosition,
            Material body,
            Material metal,
            Material display,
            Material accent)
        {
            var root = CreateRoot("Original Hotplate Stirrer", parent, localPosition);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.07f, 0f);
            collider.size = new Vector3(0.44f, 0.16f, 0.36f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Chassis",
                root.transform,
                new Vector3(0f, 0.055f, 0f),
                new Vector3(0.42f, 0.11f, 0.34f),
                body);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Top Deck",
                root.transform,
                new Vector3(0f, 0.118f, -0.015f),
                new Vector3(0.39f, 0.025f, 0.30f),
                metal);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Ceramic Heating Plate",
                root.transform,
                new Vector3(0f, 0.143f, -0.035f),
                new Vector3(0.135f, 0.008f, 0.135f),
                display);

            var panel = CreatePrimitive(
                PrimitiveType.Cube,
                "Control Fascia",
                root.transform,
                new Vector3(0f, 0.061f, 0.176f),
                new Vector3(0.38f, 0.075f, 0.025f),
                display);
            panel.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);

            foreach (var x in new[] { -0.125f, 0.125f })
            {
                var knob = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    x < 0f ? "Heat Dial" : "Stir Dial",
                    root.transform,
                    new Vector3(x, 0.062f, 0.198f),
                    new Vector3(0.037f, 0.018f, 0.037f),
                    metal);
                knob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Temperature Display",
                root.transform,
                new Vector3(0f, 0.075f, 0.199f),
                new Vector3(0.085f, 0.035f, 0.008f),
                accent);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Power Indicator",
                root.transform,
                new Vector3(0.072f, 0.075f, 0.207f),
                Vector3.one * 0.012f,
                accent);

            foreach (var x in new[] { -0.165f, 0.165f })
            {
                foreach (var z in new[] { -0.125f, 0.125f })
                {
                    CreatePrimitive(
                        PrimitiveType.Cylinder,
                        "Rubber Foot",
                        root.transform,
                        new Vector3(x, -0.008f, z),
                        new Vector3(0.025f, 0.012f, 0.025f),
                        display);
                }
            }

            return root;
        }

        public static GameObject CreatePpeDisplay(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Material cabinet,
            Material suit,
            Material dark,
            Material visor,
            Material accent)
        {
            var root = CreateRoot("Original PPE Suit Display", parent, localPosition);
            root.transform.localRotation = localRotation;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.08f, 0.12f);
            collider.size = new Vector3(1.2f, 2.16f, 0.34f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Locker Back",
                root.transform,
                new Vector3(0f, 1.08f, 0.15f),
                new Vector3(1.16f, 2.16f, 0.08f),
                cabinet);
            foreach (var x in new[] { -0.56f, 0.56f })
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Locker Side",
                    root.transform,
                    new Vector3(x, 1.08f, 0f),
                    new Vector3(0.08f, 2.16f, 0.38f),
                    cabinet);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Locker Top",
                root.transform,
                new Vector3(0f, 2.16f, 0f),
                new Vector3(1.2f, 0.08f, 0.38f),
                cabinet);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Locker Base",
                root.transform,
                new Vector3(0f, 0.04f, 0f),
                new Vector3(1.2f, 0.08f, 0.38f),
                cabinet);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Suit Hanger",
                root.transform,
                new Vector3(0f, 2.00f, 0f),
                new Vector3(0.025f, 0.38f, 0.025f),
                dark,
                Quaternion.Euler(0f, 0f, 90f));

            CreatePrimitive(
                PrimitiveType.Capsule,
                "Protective Suit Torso",
                root.transform,
                new Vector3(0f, 1.30f, -0.08f),
                new Vector3(0.42f, 0.42f, 0.22f),
                suit);
            CreatePrimitive(
                PrimitiveType.Sphere,
                "Protective Hood",
                root.transform,
                new Vector3(0f, 1.84f, -0.08f),
                new Vector3(0.43f, 0.46f, 0.30f),
                suit);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Face Visor",
                root.transform,
                new Vector3(0f, 1.86f, -0.245f),
                new Vector3(0.31f, 0.17f, 0.025f),
                visor);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Respirator",
                root.transform,
                new Vector3(0f, 1.70f, -0.285f),
                new Vector3(0.08f, 0.055f, 0.08f),
                dark,
                Quaternion.Euler(90f, 0f, 0f));

            foreach (var side in new[] { -1f, 1f })
            {
                CreatePrimitive(
                    PrimitiveType.Capsule,
                    side < 0f ? "Left Protective Sleeve" : "Right Protective Sleeve",
                    root.transform,
                    new Vector3(side * 0.39f, 1.28f, -0.06f),
                    new Vector3(0.16f, 0.39f, 0.16f),
                    suit,
                    Quaternion.Euler(0f, 0f, side * -12f));
                CreatePrimitive(
                    PrimitiveType.Capsule,
                    side < 0f ? "Left Protective Leg" : "Right Protective Leg",
                    root.transform,
                    new Vector3(side * 0.16f, 0.60f, -0.05f),
                    new Vector3(0.19f, 0.42f, 0.19f),
                    suit,
                    Quaternion.Euler(0f, 0f, side * -2f));
                CreatePrimitive(
                    PrimitiveType.Cube,
                    side < 0f ? "Left Safety Boot" : "Right Safety Boot",
                    root.transform,
                    new Vector3(side * 0.17f, 0.16f, -0.13f),
                    new Vector3(0.23f, 0.14f, 0.35f),
                    dark);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "PPE Status Strip",
                root.transform,
                new Vector3(0f, 2.105f, -0.205f),
                new Vector3(0.78f, 0.025f, 0.018f),
                accent);
            return root;
        }

        public static GameObject CreateReagentRack(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Material rack,
            Material clearGlass,
            Material amberGlass,
            Material cap,
            Material label)
        {
            var root = CreateRoot("Original Reagent Bottle Rack", parent, localPosition);
            root.transform.localRotation = localRotation;
            CreatePrimitive(
                PrimitiveType.Cube,
                "Rack Tray",
                root.transform,
                new Vector3(0f, 0.025f, 0f),
                new Vector3(0.82f, 0.05f, 0.30f),
                rack);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Rack Back Rail",
                root.transform,
                new Vector3(0f, 0.22f, 0.13f),
                new Vector3(0.82f, 0.04f, 0.04f),
                rack);
            foreach (var x in new[] { -0.39f, 0.39f })
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Rack Upright",
                    root.transform,
                    new Vector3(x, 0.13f, 0.13f),
                    new Vector3(0.04f, 0.26f, 0.04f),
                    rack);
            }

            for (var index = 0; index < 5; index++)
            {
                var bottle = new GameObject("Reference Reagent Bottle " + (index + 1));
                bottle.transform.SetParent(root.transform, false);
                bottle.transform.localPosition = new Vector3(-0.31f + index * 0.155f, 0.055f, -0.01f);
                var bottleMaterial = index % 2 == 0 ? amberGlass : clearGlass;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bottle Body",
                    bottle.transform,
                    new Vector3(0f, 0.09f, 0f),
                    new Vector3(0.055f, 0.085f, 0.055f),
                    bottleMaterial);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Bottle Shoulder",
                    bottle.transform,
                    new Vector3(0f, 0.175f, 0f),
                    new Vector3(0.058f, 0.035f, 0.058f),
                    bottleMaterial);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bottle Neck",
                    bottle.transform,
                    new Vector3(0f, 0.205f, 0f),
                    new Vector3(0.027f, 0.035f, 0.027f),
                    bottleMaterial);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bottle Cap",
                    bottle.transform,
                    new Vector3(0f, 0.248f, 0f),
                    new Vector3(0.034f, 0.016f, 0.034f),
                    cap);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Bottle Label",
                    bottle.transform,
                    new Vector3(0f, 0.105f, -0.056f),
                    new Vector3(0.072f, 0.075f, 0.008f),
                    label);
            }

            return root;
        }

        public static GameObject CreateGasWashTrain(
            Transform parent,
            Vector3 localPosition,
            Material frame,
            Material glass,
            Material tubing,
            Material liquid)
        {
            var root = CreateRoot("Original Gas Wash Train", parent, localPosition);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.size = new Vector3(0.96f, 0.62f, 0.42f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Wash Train Base",
                root.transform,
                new Vector3(0f, 0.025f, 0f),
                new Vector3(0.78f, 0.05f, 0.28f),
                frame);

            foreach (var x in new[] { -0.22f, 0.22f })
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Gas Wash Bottle",
                    root.transform,
                    new Vector3(x, 0.19f, 0f),
                    new Vector3(0.10f, 0.17f, 0.10f),
                    glass);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Scrubbing Liquid",
                    root.transform,
                    new Vector3(x, 0.11f, 0f),
                    new Vector3(0.082f, 0.075f, 0.082f),
                    liquid);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bottle Stopper",
                    root.transform,
                    new Vector3(x, 0.37f, 0f),
                    new Vector3(0.055f, 0.025f, 0.055f),
                    frame);
                CreateCylinderBetween(
                    "Dip Tube",
                    root.transform,
                    new Vector3(x - 0.025f, 0.37f, 0f),
                    new Vector3(x - 0.025f, 0.10f, 0f),
                    0.012f,
                    tubing);
            }

            CreateCylinderBetween(
                "Interconnect Tube",
                root.transform,
                new Vector3(-0.195f, 0.39f, 0f),
                new Vector3(0.195f, 0.39f, 0f),
                0.014f,
                tubing);
            CreateCylinderBetween(
                "Hood Intake Tube",
                root.transform,
                new Vector3(-0.245f, 0.39f, 0f),
                new Vector3(-0.46f, 0.58f, 0f),
                0.014f,
                tubing);
            return root;
        }

        private static GameObject CreateRoot(string name, Transform parent, Vector3 localPosition)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            return root;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion? localRotation = null)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;
            instance.transform.localRotation = localRotation ?? Quaternion.identity;
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                var transparent = material != null && material.renderQueue >= 3000;
                renderer.shadowCastingMode = transparent
                    ? ShadowCastingMode.Off
                    : ShadowCastingMode.On;
                renderer.receiveShadows = !transparent;
            }

            var collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            return instance;
        }

        private static void CreateCylinderBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            var direction = end - start;
            var pipe = CreatePrimitive(
                PrimitiveType.Cylinder,
                name,
                parent,
                (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f, radius),
                material);
            pipe.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }
    }
}
