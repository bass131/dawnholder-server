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

        // byte → CharacterClass 변환 + 미유효 처리 (Warrior fallback). 순수 함수 — 테스트 대상.
        // invalid byte: 서버 trusted 값이지만 확장 가능성을 고려해 방어적으로 Warrior 수렴.
        public static CharacterClass ByteToClass(byte raw)
        {
            CharacterClass cls = (CharacterClass)raw;
            if (cls == CharacterClass.Warrior || cls == CharacterClass.Ranger)
                return cls;
            Debug.LogWarning($"[ClassLoadout] 알 수 없는 characterClass byte={raw} — Warrior fallback.");
            return CharacterClass.Warrior;
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
