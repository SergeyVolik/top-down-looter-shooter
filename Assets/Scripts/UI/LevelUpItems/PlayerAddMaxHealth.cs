using SV.ECS;
using SV.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAddMaxHealth : MonoBehaviour
{
    public int addMaxhealth;

    public HealthAddEnum type;
    public enum HealthAddEnum
    {
        HealFullHP,
        AddMaxHP
    }
    public TMPro.TMP_Text text;
    public Button selectButton;
    private EntityManager entityManager;
    private EntityQuery query;

    private void Awake()
    {
        switch (type)
        {
            case HealthAddEnum.HealFullHP:
                text.text = $"Full HP";
                break;
            case HealthAddEnum.AddMaxHP:
                text.text = $"+ {addMaxhealth} MAX HP";
                break;
            default:
                break;
        }
     

        selectButton.onClick.AddListener(() =>
        {
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var item in entities)
            {
                var maxHealth = entityManager.GetComponentData<MaxHealthComponent>(item);
                var health = entityManager.GetComponentData<HealthComponent>(item);
              
               

                switch (type)
                {
                    case HealthAddEnum.HealFullHP:
                        health.value = maxHealth.value;
                        break;
                    case HealthAddEnum.AddMaxHP:
                        maxHealth.value += addMaxhealth;
                        health.value += addMaxhealth;
                        break;
                    default:
                        break;
                }
                entityManager.SetComponentData(item, maxHealth);
                entityManager.SetComponentData(item, health);
            }

            UINavigationManager.Instance.Pop();
        });

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = entityManager.CreateEntityQuery(typeof(PlayerComponent), typeof(MaxHealthComponent));

    }


}
