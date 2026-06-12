/// <summary>SkillId → Sprite 매핑 에셋. 클래스별 아이콘을 데이터 주도로 공급한다.</summary>
#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    [CreateAssetMenu(fileName = "SkillIconSet", menuName = "Dawnholder/Skill Icon Set")]
    public class SkillIconSet : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public SkillId skillId;
            public Sprite icon;
        }

        [SerializeField] Entry[] _entries = System.Array.Empty<Entry>();

        /// <summary>skill에 대응하는 Sprite를 반환. 매핑 없으면 null.</summary>
        public Sprite? GetIcon(SkillId skill)
        {
            foreach (Entry e in _entries)
            {
                if (e.skillId == skill) return e.icon;
            }
            return null;
        }
    }
}
