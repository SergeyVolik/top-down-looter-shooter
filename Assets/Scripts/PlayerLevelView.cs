using SV.ECS;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SV
{
    public class PlayerLevelView : MonoBehaviour
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
            colorTablesQ = _entityManager.CreateEntityQuery(new ComponentType[] { typeof(PlayerLevelComponent), typeof(PlayerExpUpgradeComponent) });

        }

        private void Update()
        {
           
           
            var maxhealth = colorTablesQ.ToEntityArray(Allocator.Temp);


            for (int i = 0; i < maxhealth.Length; i++)
            {
                var exp = _entityManager.GetComponentData<PlayerLevelComponent>(maxhealth[i]);
                var expSettings = _entityManager.GetBuffer<PlayerExpUpgradeComponent>(maxhealth[i], isReadOnly: true);

                if (exp.level == expSettings.Length)
                {
                    healthText.text = $"Lvl {exp.level} Max";
                    slider.value = 1f;
                }
                else {
                    var currentExp = expSettings[exp.level - 1];

                    healthText.text = $"Lvl {exp.level} {exp.currentExp} / {currentExp.expForNextLevel}";

                    slider.value = (float)exp.currentExp / currentExp.expForNextLevel;
                }
                
            }
          
            maxhealth.Dispose();

        }
    }



}
