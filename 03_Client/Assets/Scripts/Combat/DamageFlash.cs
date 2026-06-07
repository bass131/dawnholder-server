#nullable enable
using System.Collections;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 피격 시 sprite 빨간 플래시 — 시각 표시 전용 (헌법 #1, 데미지 계산 0).
    // LocalPlayer GameObject에 AddComponent 또는 prefab에 미리 박음.
    // EnemyAttackHandler가 Flash()를 호출.
    [DisallowMultipleComponent]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] float _flashDuration = 0.15f;

        SpriteRenderer? _sr;
        Color _originalColor;
        Coroutine? _activeFlash;

        void Awake()
        {
            Rebind();
        }

        // 비주얼이 "Visual" 자식으로 장착되는 구조(v2) — 자식 탐색 + 늦은 장착 대비 지연 재바인딩.
        void Rebind()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _originalColor = _sr.color;
        }

        public void Flash()
        {
            if (_sr == null) Rebind();
            if (_sr == null) return;
            if (_activeFlash != null) StopCoroutine(_activeFlash);
            _activeFlash = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            if (_sr == null) yield break;
            _sr.color = Color.red;
            yield return new WaitForSeconds(_flashDuration);
            _sr.color = _originalColor;
            _activeFlash = null;
        }
    }
}
