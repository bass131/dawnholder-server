작성UTC 2026-10-14T13:32:52Z
# 공통 API 보충 1 — typed payload 노출과 recovery 진단

원천: A 공식 질문 msg_72c05c4ce435, technical-spec.md 3.2 ResultSnapshot/3.3 payload v1/5.5 RecoverAndLoad, 기존 api-contract v1과 선행 api-surface v2. 리드의 범위 안 구현 선택이며 새 사용자 범위 결정이 아니다. 최초 고정 입력/선행 시험/v2 필수 표면은 바꾸지 않는다.

## A가 쓰는 추가 공통 값 표면

OperationIdentity에 종류별 nullable get-only 멤버 IntendedClass(CharacterClass?), SafeDefaults(SafeDefaults?), ExpectedCharacterToken(PersistenceToken?), ExpectedProgressToken(PersistenceToken?), Safe(SafeCheckpoint?), ExpectedOwnerKind(PersistenceOwnerKind?), ExpectedOwnerId(Guid?), Reason(string?)를 허용한다. public setter/init/opaque byte payload는 없다. 내부 생성 경로는 검증한 request만 사용하며 kind별 불필요 필드는 null로 둔다. expected progress의 null은 token 부재이며 영 token과 다르다.

동등성은 기존 공통 필드와 종류별 canonical typed payload 전체를 비교한다. Recover의 공통 OwnerId는 새 관리 owner이고 ExpectedOwnerKind/ExpectedOwnerId는 관찰한 이전 owner다. observed LastSequence는 §3.3 Recover payload에 없으므로 그 요청의 OperationIdentity 동등성에 넣지 않는다. Kind/공통Sequence0/ExpectedFence/대상identity와 관리사유 등 명세 필수값은 빠뜨리지 않는다. SQL byte codec/정렬/드라이버를 이번에 구현하지 않는다.

## Recovery 결과의 진단 값

InspectRecoveryResult 안의 nested sealed StoredState/StoredProgress를 허용한다. 필드 의미는 §3.2의 character/progress presence, 저장 class·각 token, raw storedProgress(map/x/y/hp/maxHp/bossUnlocked)다. absence는 명시적으로 구분하고 임의의 빈 정상 상태나 SafeDefaults로 바꾸지 않는다. null shape/부분값/모순은 결과 constructor/factory 경계에서 거부한다.

RecoverAndLoadResult·ResolveRecoveryOperationResult는 필요한 historical 진단값과 receipt/current authority·token을 구분해 보존한다. release 결과에 게임 load 상태를 강제로 붙이지 않는다. Recover safe는 항상 null이며 Runtime Ready·정상화·자동수선·caller content 연결은 없다. 잘못된 저장 class/ownership을 fallback 값으로 고치지 않는다. 진단값이 현재 Runtime 권위를 부여한다는 뜻도 아니다.

A 소유목록의 recovery status 4파일과 PersistenceContractGuard는 실제 중복 검증 책임이 있을 때 사용할 수 있다. 불필요한 파일/범용 확장 hook을 목록을 채우려고 만들지 않는다. 필수 runtime5/recovery4 interface 메서드 수와 선행시험은 불변이다.

## B 소비 경계

B는 A 타입/이 보충을 소비하며 별도 사본이나 setter를 만들지 않는다. B의 이미 선택한 close 정책(미발행 대기는 NotDispatched로 정산하고 별도 final 인계 처리)은 원래 미고정 정책 범위 안이며 채택한다. 발행 중 작업의 추적/미확정/late 결과·한 deadline과 NoWriteNeeded 계약을 보존한다.

새 독립 검증자는 이 추가 의미와 실제 코드를 대조한다. 특히 Recover 동등성의 관찰 LastSequence 제외·이전 owner 포함, recovery absence/partial shape와 historical/current 분리, immutable getter의 alias를 확인한다. 이 문서 자체는 compile 또는 시험 통과 증거가 아니다.
