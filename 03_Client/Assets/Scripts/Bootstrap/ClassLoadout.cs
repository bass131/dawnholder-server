#nullable enable
using Dawnholder.Client.Combat;
using Dawnholder.Client.Scenes;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Bootstrap
{
    // 직업 ClassConfig 해석 — PlayerPrefs 선택값 → Resources 조회.
    public static class ClassLoadout
    {
        // configs 배열에서 cls와 Class 필드가 일치하는 첫 항목 반환. 없으면 null.
        // 순수 함수 — 직업 switch 분기 없음. 테스트 대상.
        public static ClassConfig? FindConfig(ClassConfig[] configs, CharacterClass cls)
        {
            foreach (ClassConfig cfg in configs)
            {
                if (cfg.Class == cls) return cfg;
            }
            return null;
        }

        // PlayerPrefs 선택값 → Resources.LoadAll → FindConfig.
        // 못 찾으면 fail-loud (Debug.LogError) + null 반환 — 조건부 장착 실패는 LocalPlayerSpawner가 처리.
        public static ClassConfig? Resolve()
        {
            int classValue = PlayerPrefs.GetInt(
                CharacterSelectController.SelectedClassPrefsKey,
                (int)CharacterClass.Warrior);

            CharacterClass cls = (CharacterClass)classValue;

            ClassConfig[] configs = Resources.LoadAll<ClassConfig>("ClassConfigs");

            ClassConfig? found = FindConfig(configs, cls);
            if (found == null)
            {
                // Assets/Resources/ClassConfigs/ 폴더에 ClassConfig 에셋을 생성하세요.
                // Unity 메뉴: Assets > Create > Dawnholder > ClassConfig > Knight (또는 Mage)
                Debug.LogError(
                    $"[ClassLoadout] ClassConfig 미발견 — class={cls}. " +
                    "Assets/Resources/ClassConfigs/ 에 ClassConfig 에셋을 생성하세요 " +
                    "(Create > Dawnholder/ClassConfig/Knight 또는 Mage).");
            }
            return found;
        }
    }
}
