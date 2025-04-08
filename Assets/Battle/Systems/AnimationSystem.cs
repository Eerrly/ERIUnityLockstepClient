using System.Collections.Generic;

/// <summary>
/// 动画系统
/// </summary>
[EntitySystem]
public class AnimationSystem
{
    /// <summary>
    /// 默认动画开始过渡时间
    /// </summary>
    public static readonly FixedNumber DefaultTransitionDuration = FixedNumber.MakeFixNum(2, 30);
    /// <summary>
    /// 默认动画结束过渡时间
    /// </summary>
    public static readonly FixedNumber DefaultAnimEndDuration = FixedNumber.MakeFixNum(3333333 * 5, 100000000);

    /// <summary>
    /// 设置动画
    /// </summary>
    public static void SetAnimation(PlayerEntity playerEntity, BattleEntity battleEntity, EAnimationID animationId)
    {
        playerEntity.Animation.animId = animationId;
        playerEntity.Animation.index = (int)EAnimationEvent.None;
        playerEntity.Animation.startTime = battleEntity.Time;
        playerEntity.Animation.currentEvents.Clear();
    }

    /// <summary>
    /// 更新触发事件
    /// </summary>
    public static void UpdateEvent(BattleEntity battleEntity)
    {
        for (int i = 0; i < battleEntity.PlayerEntities.Count; i++)
        {
            var playerEntity = battleEntity.PlayerEntities[i];
            playerEntity.Animation.currentEvents.Clear();
            // 循环动画没有事件
            if (playerEntity.Animation.animId == EAnimationID.None || !AnimationManager.Instance.CheckAnimationNotLoop(playerEntity.Animation.animId))
                continue;
            // 轮询完了所有的事件
            var index = playerEntity.Animation.index + 1;
            if (index >= (int)EAnimationEvent.Count)
                continue;
            // 是否达到了触发事件的时间
            var animationEventLength = AnimationManager.Instance.GetAnimationLength(playerEntity.Animation.animId, (EAnimationEvent)index);
            var conditionTime = playerEntity.Animation.startTime + animationEventLength;
            // 如果是结束，则在减去过渡值
            if (index == (int)EAnimationEvent.AnimEnd)
                conditionTime -= DefaultAnimEndDuration;
            // 满足时间条件
            var condition = battleEntity.Time >= conditionTime;
            if (!condition)
                continue;
            // 动画事件索引++
            playerEntity.Animation.index = index;
            playerEntity.Animation.currentEvents.Add((EAnimationEvent)playerEntity.Animation.index);
        }
    }
    
    /// <summary>
    /// 是否触发动画事件
    /// </summary>
    public static bool IsTriggerEvent(PlayerEntity playerEntity, EAnimationEvent eventType)
    {
        for (int index = playerEntity.Animation.currentEvents.Count - 1; index >= 0; --index)
        {
            if (playerEntity.Animation.currentEvents[index].Equals(eventType))
                return true;
        }
        return false;
    }
    
}