namespace Shared.GameData;

// 적의 종류 식별자. byte로 박아 S_EntitySpawn 패킷의 entityKind 필드와 1:1 매핑.
//
// stability 약속: 값은 영원히 고정. 새 종류는 *append-only* (Normal=0, Boss=1, Golem=2, 다음 = 3).
// M4.5-02에서 서버/클라 중복 정의를 98_Shared로 이사 (헌법 #4 봉합).
public enum EnemyKind : byte
{
    Normal = 0,
    Boss = 1,
    Golem = 2,
}
