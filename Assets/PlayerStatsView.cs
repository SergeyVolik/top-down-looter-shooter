using SV.ECS;
using SV.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PlayerStatsView : MonoBehaviour, IPageShowedListener, IPageHidedListener
{
    public TMPro.TMP_Text healthStat;
    public TMPro.TMP_Text speedStat;
    public TMPro.TMP_Text hpRegenStat;
    public TMPro.TMP_Text attackSpeedStat;
    public TMPro.TMP_Text damageStat;
    public TMPro.TMP_Text luckStat;
    public TMPro.TMP_Text critStat;

    private EntityManager em;
    private Entity entity;
    private bool awaked;

    private void Awake()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        entity = em.CreateEntity();
        em.AddComponent(entity, typeof(UpdateStatsViewComponent));
        em.SetComponentData(entity, new UpdateStatsViewComponent
        {
            value = this
        });
        em.SetName(entity, "PlayerStatsView");

   
        awaked = true;
    }
    public void OnHided()
    {
        
    }

    public void OnShowed()
    {
        
    }
}

public class UpdateStatsViewComponent : IComponentData
{
    public PlayerStatsView value;
}

public partial class UpdateStatsViewSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<PlayerStatsComponent>();
        RequireForUpdate<UpdatePlayerStatsComponent>();
    }
    protected override void OnUpdate()
    {
        var stats = SystemAPI.GetSingleton<PlayerStatsComponent>();
        foreach (var view in SystemAPI.Query<UpdateStatsViewComponent>())
        {
            var viewData = view.value;

            viewData.healthStat.text = stats.maxHealth.ToString();
            viewData.damageStat.text = stats.damage.ToString();
            viewData.luckStat.text = stats.luck.ToString();
            viewData.speedStat.text = stats.speed.ToString();
            viewData.attackSpeedStat.text = stats.attackSpeed.ToString();

            var regen = stats.hpRegenInterval != 0 ? (1 / stats.hpRegenInterval).ToString() : "0";
            viewData.hpRegenStat.text = $"{regen} per/sec";
            viewData.critStat.text = stats.critChance.ToString();

        }
    }
}