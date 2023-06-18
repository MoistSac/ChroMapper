using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GeometryContainer : ObjectContainer
    {
        [SerializeField] private GameObject Shape;

        public override BaseObject ObjectData
        {
            get => null;
            set => _ = value;
        }

        public BaseEnvironmentEnhancement EnvironmentEnhancement;

        public override void UpdateGridPosition()
        {
        }

        public static GeometryContainer SpawnGeometry(BaseEnvironmentEnhancement eh, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GeometryContainer>();
            var type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), (string)eh.Geometry[eh.GeometryKeyType]);
            container.Shape = GameObject.CreatePrimitive(type);
            container.Shape.transform.parent = container.Animator.AnimationThis.transform;
            container.Animator.SetGeometry(eh);
            container.gameObject.SetActive(true);
            return container;
        }
    }
}
