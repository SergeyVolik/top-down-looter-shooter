using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace SV.ECS
{
    public interface IUpdateable
    {
        public void OnUpdate();
    }
    public class UpdateDataComponent : IComponentData
    {
        public IUpdateable value;
    }

    public partial class UpdateMonoBehaviorSystem : SystemBase
    {


        protected override void OnUpdate()
        {

          
            Entities.ForEach((in UpdateDataComponent vel) =>
            {


                vel.value.OnUpdate();

            }).WithoutBurst().Run();
        }

    }
}