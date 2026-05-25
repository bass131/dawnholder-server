using Dawnholder.Client.Network;
using UnityEngine;

namespace Dawnholder.Client.Bootstrap
{
    /// <summary>
    /// 코드 주도 부트스트래퍼 (ADR-027). ⚠️ ADR-021의 SceneBootstrap(UI Additive 로더)과 다름 —
    /// 이건 PersistentServices(네트워크+페이드) 1회 spawn 전담.
    ///
    /// [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] 정적 메서드가
    /// *첫 씬 로드 전에* Resources/PersistentServices 프리팹을 1회 Instantiate + DontDestroyOnLoad.
    /// 에디터에서 어느 씬에서 Play하든, 빌드에서든 동일 경로 보장.
    /// 별도 _Bootstrap 씬·빌드 순서 관리 불필요.
    ///
    /// **왜 RuntimeInitializeOnLoadMethod인가**:
    /// 씬 로드보다 코드가 먼저 실행되므로 "어느 씬에 이 오브젝트를 두어야 하나"
    /// 고민이 사라집니다. 단 디버깅 시 "이 오브젝트 언제 생겼지?" 질문이 나올 수 있으니
    /// Play 시작 직후 Hierarchy에서 PersistentServices 오브젝트 확인으로 진단.
    ///
    /// **프리팹 구성 (사용자가 작업)**:
    /// Resources/PersistentServices.prefab 에 세 컴포넌트를 붙임:
    ///   1. MainThreadDispatcher — 소켓 워커 콜백을 main thread로 마샬링
    ///   2. NetworkService       — 소켓/세션 보유, Connect()/Disconnect() 공개
    ///   3. SceneTransition      — 씬 전환 페이드 (CanvasGroup 필요)
    ///
    /// **중복 방어**:
    /// 정적 bool _spawned가 1회 박히면 재호출 시 no-op.
    /// (도메인 리로드 Off 에디터 환경에서 Play/Stop/Play 반복 시 두 번 호출 가능 — 이 가드가 막음.)
    /// </summary>
    public static class PersistentServicesBootstrap
    {
        // 도메인 리로드 Off 환경(Enter Play Mode 가속) 대비 정적 가드.
        // 도메인 리로드 On 이면 Play 시작마다 정적 필드가 초기화되어 가드 불필요하지만,
        // 양쪽 모두 안전하게 동작한다.
        static bool _spawned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SpawnPersistentServices()
        {
            if (_spawned) return;

            // DontDestroyOnLoad 오브젝트가 이미 존재하는지 확인 (도메인 리로드 Off 중 재시작 방어).
            // FindObjectsByType은 BeforeSceneLoad 단계에서도 안전하게 호출 가능.
            var existing = Object.FindAnyObjectByType<NetworkService>();
            if (existing != null)
            {
                _spawned = true;
                return;
            }

            var prefab = Resources.Load<GameObject>("PersistentServices");
            if (prefab == null)
            {
                Debug.LogError(
                    "[PersistentServicesBootstrap] Resources/PersistentServices.prefab 을 찾을 수 없습니다. " +
                    "다음을 확인하세요:\n" +
                    "  1. 프리팹이 Assets/Resources/ 폴더 안에 있는지\n" +
                    "  2. 파일 이름이 정확히 'PersistentServices' 인지 (대소문자 포함)\n" +
                    "  3. 프리팹이 아직 생성 전이라면 unity-bridge 작업으로 생성 필요");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = "PersistentServices"; // Clone 접미사 제거 (Hierarchy 가독성)
            Object.DontDestroyOnLoad(instance);

            _spawned = true;
            Debug.Log("[PersistentServicesBootstrap] PersistentServices 생성 완료 (DontDestroyOnLoad). 앱 전체에 1개 유지.");
        }
    }
}
