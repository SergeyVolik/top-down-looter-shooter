using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SV.ECS
{
#if UNITY_EDITOR
    public partial class LoadInitSceneSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //SceneManager.LoadSceneAsync(0);
            //Enabled = false;
            //Debug.Log("Load Init Scene System");
        }
    }
#endif
}