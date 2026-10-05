// Installed EPL layout code is excluded at build time. This layout is never selectable.
export default class DisabledLayout {
  constructor() { throw new Error('이 앱에서는 elk 레이아웃을 사용하지 않습니다.'); }
}
