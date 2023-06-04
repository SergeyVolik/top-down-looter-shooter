using SV.ECS;
using SV.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class AddSpeedLevelUPUI : MonoBehaviour
{
    public float speed;

   
    public TMPro.TMP_Text text;
    public Button selectButton;
    private EntityManager entityManager;
    private EntityQuery query;

    private void Awake()
    {
        text.text = $"+ {speed} Speed";


        selectButton.onClick.AddListener(() =>
        {
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var item in entities)
            {
                var speedComp = entityManager.GetComponentData<CharacterControllerComponent>(item);


                speedComp.speed += speed;



                entityManager.SetComponentData(item, speedComp);
              
            }

            UINavigationManager.Instance.Pop();
        });

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        query = entityManager.CreateEntityQuery(typeof(PlayerComponent), typeof(CharacterControllerComponent));

    }


}
