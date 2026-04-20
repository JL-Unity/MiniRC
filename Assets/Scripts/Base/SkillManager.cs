using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    NormalAttack,
    Ordinary,
    Ultimate,
}

/// <summary>技能注册表（示例）；UnRegister 逻辑已按「存在则移除」修正。</summary>
public class SkillManager : BaseManager<SkillManager>
{
    private readonly Dictionary<SkillType, Skill> _skillByType = new Dictionary<SkillType, Skill>();

    public void RegisterSkill(SkillType type, Skill skill)
    {
        if (skill == null || _skillByType.ContainsKey(type))
        {
            return;
        }

        _skillByType.Add(type, skill);
        LogClass.LogGame(GameLogCategory.System, "Register Skill: " + skill.Name);
    }

    public void UnRegisterSkill(SkillType type)
    {
        if (_skillByType.Remove(type))
        {
            LogClass.LogGame(GameLogCategory.System, "Unregister Skill type: " + type);
        }
    }

    public void Attack(SkillType skillType)
    {
        if (!_skillByType.TryGetValue(skillType, out Skill skill) || skill == null)
        {
            return;
        }

        if (skill.CanAttack)
        {
            skill.Attack();
            LogClass.LogGame(GameLogCategory.System, "Skill triggered: " + skill.name);
        }
    }

    public override void Clear()
    {
        base.Clear();
        _skillByType.Clear();
    }
}

/// <summary>占位基类：在正式项目中用你的 Skill 实现替换。</summary>
public abstract class Skill : MonoBehaviour
{
    public string Name => name;
    public abstract bool CanAttack { get; }
    public abstract void Attack();
}
