using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SV
{
    public partial class HealthBar : MonoBehaviour
    {
        public Slider slider;

        public TMPro.TMP_Text healthText;
        private EntityManager _entityManager;
        private EntityQuery colorTablesQ;


        private void Start()
        {
            slider = GetComponent<Slider>();


            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var e = _entityManager.CreateEntity();
            _entityManager.AddComponentData(e, new PlayerHeathBarComponent
            {
                healthText = healthText,
                slider = slider
            });
            colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

        }
        private void OnDestroy()
        {
          
        }
        private void Update()
        {

            var health = colorTablesQ.ToComponentDataArray<HealthComponent>(Allocator.Temp);
            var maxhealth = colorTablesQ.ToComponentDataArray<MaxHealthComponent>(Allocator.Temp);


            for (int i = 0; i < health.Length; i++)
            {

                var currentHealth = health[i].value;
                var maxHealth = maxhealth[i].value;


                slider.value = (currentHealth / (float)maxHealth);
                healthText.text = $"{currentHealth}/{maxHealth}";
            }
            health.Dispose();
            maxhealth.Dispose();

        }

        public class PlayerHeathBarComponent : IComponentData
        {
            public Slider slider;
            public TMPro.TMP_Text healthText;
        }
        public partial class UpdateHealthbarSystem : SystemBase
        {
            private EntityQuery playerQuery;

            protected override void OnCreate()
            {
                RequireForUpdate<PlayerHeathBarComponent>();
                playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerComponent, MaxHealthComponent, HealthComponent>().Build();
                RequireForUpdate(playerQuery);
            }

            protected override void OnUpdate()
            {

                var maxHeath = playerQuery.ToComponentDataArray<MaxHealthComponent>(Allocator.Temp);
                var health = playerQuery.ToComponentDataArray<HealthComponent>(Allocator.Temp);

                var currentHealth = health[0].value;
                var maxHealth = maxHeath[0].value;

                foreach (var item in SystemAPI.Query<PlayerHeathBarComponent>())
                {

                    item.slider.value = (currentHealth / (float)maxHealth);
                    item.healthText.text = $"{currentHealth}/{maxHealth}";
                }

                maxHeath.Dispose();
                health.Dispose();
            }
        }
    }



}
