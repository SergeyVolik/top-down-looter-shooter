using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SV
{
    public class HealthBar : MonoBehaviour
    {
        private Slider slider;
        [SerializeField]
        private TMPro.TMP_Text healthText;
        private EntityManager _entityManager;
        private EntityQuery colorTablesQ;


        private void Start()
        {
            slider = GetComponent<Slider>();


            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerComponent), typeof(HealthComponent), typeof(MaxHealthComponent) });

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
    }



}
