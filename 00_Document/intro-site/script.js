// Dawnholder 소개 페이지의 움직임 보조. 내용을 만들거나 숨기지 않는다(스크립트가 없어도 모든 글과 그림이 보인다).
// 맡는 일 세 가지:
// 1. 첫 화면 겹 배경의 깊이: 마우스 위치와 스크롤 양을 .hero의 --mx·--my·--sy에 넣는다.
// 2. 플레이 절 파노라마: 절이 화면을 지나는 비율만큼 들판 겹의 이동 거리 --pan을 넣는다.
// 3. 직업 절 스킬 재생: 스킬 이름(data-action)을 누르면 사이트 README 「동작 맞물림」 표대로 캐릭터와 효과를 재생한다.
// 움직임 허용의 정본은 「움직임 멈춤」 체크박스(data-motion-toggle)와 움직임 줄이기 설정이다.
// 둘 중 하나라도 멈춤이면 이 스크립트도 아무것도 움직이지 않는다(styles.css도 같은 상태로 반복 움직임을 멈춘다).
(() => {
  'use strict';

  const motionStop = document.querySelector('[data-motion-toggle]');
  if (!motionStop) {
    return;
  }

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');

  // 움직임 줄이기 설정이면 「움직임 멈춤」도 체크된 모양으로 맞춘다(CSS는 설정만으로 이미 멈춘다).
  if (reducedMotion.matches) {
    motionStop.checked = true;
  }

  const motionAllowed = () => !motionStop.checked && !reducedMotion.matches;

  // ── 1·2. 첫 화면 깊이와 플레이 파노라마 ──
  const hero = document.querySelector('.hero');
  const panBand = document.querySelector('.pan-band');
  const panImage = panBand ? panBand.querySelector('.pan-6 img') : null;
  const pointer = { targetX: 0, targetY: 0, x: 0, y: 0 };
  let renderRequest = 0;

  const renderHero = (active) => {
    const goalX = active ? pointer.targetX : 0;
    const goalY = active ? pointer.targetY : 0;

    // 마우스를 바로 따라가지 않고 조금씩 다가가 부드럽게 어긋나게 한다.
    pointer.x = active ? pointer.x + (goalX - pointer.x) * 0.08 : 0;
    pointer.y = active ? pointer.y + (goalY - pointer.y) * 0.08 : 0;
    const scroll = active ? Math.min(window.scrollY, hero.offsetHeight) : 0;

    hero.style.setProperty('--mx', pointer.x.toFixed(3));
    hero.style.setProperty('--my', pointer.y.toFixed(3));
    hero.style.setProperty('--sy', scroll.toFixed(1));

    return Math.abs(goalX - pointer.x) > 0.002 || Math.abs(goalY - pointer.y) > 0.002;
  };

  // 띠가 화면 아래 끝에 들어올 때 0, 위 끝을 벗어날 때 1인 비율로 들판 겹을 왼쪽으로 민다(카메라가 오른쪽으로 감).
  const renderPan = (active) => {
    if (!active) {
      panBand.style.setProperty('--pan', '0px');
      return;
    }
    const box = panBand.getBoundingClientRect();
    const view = window.innerHeight;
    const progress = Math.min(1, Math.max(0, (view - box.top) / (view + box.height)));
    const range = Math.max(0, panImage.offsetWidth - panBand.clientWidth);
    panBand.style.setProperty('--pan', `${(progress * range).toFixed(1)}px`);
  };

  const render = () => {
    renderRequest = 0;
    const active = motionAllowed();
    const settling = hero ? renderHero(active) : false;
    if (panBand && panImage) {
      renderPan(active);
    }
    if (settling) {
      requestRender();
    }
  };

  const requestRender = () => {
    if (!renderRequest) {
      renderRequest = window.requestAnimationFrame(render);
    }
  };

  if (hero) {
    hero.addEventListener('pointermove', (event) => {
      if (!finePointer.matches) {
        return;
      }
      const box = hero.getBoundingClientRect();
      pointer.targetX = ((event.clientX - box.left) / box.width) * 2 - 1;
      pointer.targetY = ((event.clientY - box.top) / box.height) * 2 - 1;
      requestRender();
    });

    hero.addEventListener('pointerleave', () => {
      pointer.targetX = 0;
      pointer.targetY = 0;
      requestRender();
    });
  }

  // 스크롤은 읽기만 한다(passive). 페이지 스크롤을 가로채지 않는다.
  window.addEventListener('scroll', requestRender, { passive: true });
  window.addEventListener('resize', requestRender);

  // ── 3. 스킬 재생 ──
  const stage = document.querySelector('.stage');
  if (!stage) {
    return;
  }

  // 사이트 README 「동작 맞물림」 표를 옮긴 시간표. 시각은 누른 순간부터 ms, x는 시작 자리에서 잰 게임 단위,
  // face는 1 오른쪽·-1 왼쪽. since는 그 사본의 칸을 세는 기준 시각(같은 사본이 끊김 없이 이어지는 단계들의 첫 시작, 접점 3절).
  // sprite가 없는 단계는 캐릭터가 하던 반복 동작을 그대로 잇는다(표의 「계속」).
  // 효과의 since는 그 효과 칸의 기준 시각, at은 무대에 남는 효과의 자리(시작 자리 기준 게임 단위)다.
  const ACTIONS = {
    'knight-dash': {
      actor: 'knight',
      end: 2000,
      steps: [
        { from: 0, to: 400, sprite: 'images/knight-attack.webp', since: 0, x: [0, 4], face: 1, fx: { dash: { since: 0 } } },
        { from: 400, to: 767, sprite: 'images/knight-idle.webp', since: 400, x: [4, 4], face: 1, fx: { dash: { since: 0 } } },
        { from: 767, to: 1000, sprite: 'images/knight-idle.webp', since: 400, x: [4, 4], face: 1, fx: {} },
        { from: 1000, to: 2000, sprite: 'images/knight-move.webp', since: 1000, x: [4, 0], face: -1, fx: {} },
      ],
    },
    'mage-thunder': {
      actor: 'mage',
      end: 1100,
      steps: [
        { from: 0, to: 200, sprite: 'images/mage-cast.webp', since: 0, x: [0, 0], face: 1, fx: {} },
        { from: 200, to: 400, sprite: 'images/mage-cast.webp', since: 0, x: [0, 0], face: 1, fx: { thunder: { since: 200 } } },
        { from: 400, to: 1033, sprite: 'images/mage-idle.webp', since: 400, x: [0, 0], face: 1, fx: { thunder: { since: 200 } } },
        { from: 1033, to: 1100, sprite: 'images/mage-idle.webp', since: 400, x: [0, 0], face: 1, fx: { thunder: { since: 200 } } },
      ],
    },
    'mage-teleport': {
      actor: 'mage',
      end: 2017,
      steps: [
        { from: 0, to: 417, x: [3.5, 3.5], face: 1, fx: { depart: { since: 0, at: 0 }, arrive: { since: 0 } } },
        { from: 417, to: 517, x: [3.5, 3.5], face: 1, fx: { depart: { since: 0, at: 0 }, arrive: { since: 0 } } },
        { from: 517, to: 1500, x: [3.5, 3.5], face: 1, fx: {} },
        { from: 1500, to: 1917, x: [0, 0], face: -1, fx: { depart: { since: 1500, at: 3.5 }, arrive: { since: 1500 } } },
        { from: 1917, to: 2017, x: [0, 0], face: -1, fx: { depart: { since: 1500, at: 3.5 }, arrive: { since: 1500 } } },
      ],
    },
  };

  const effects = {};
  for (const element of stage.querySelectorAll('[data-fx]')) {
    effects[element.dataset.fx] = element;
  }

  // 캐릭터마다 요소 하나. 띠가 바뀌면 data-sprite를 바꾸고, styles.css가 그 값으로 그림·칸 크기를 고른다.
  const actors = {};
  for (const element of stage.querySelectorAll('[data-actor]')) {
    const name = element.dataset.actor;
    actors[name] = {
      element,
      mover: stage.querySelector(`[data-mover="${name}"]`),
      idleSprite: element.dataset.sprite,
      effectNames: new Set(),
      run: null,
    };
  }
  for (const action of Object.values(ACTIONS)) {
    for (const step of action.steps) {
      for (const name of Object.keys(step.fx)) {
        actors[action.actor].effectNames.add(name);
      }
    }
  }

  // 칸 수·fps·한 번 재생 여부는 styles.css의 data-sprite 규칙(README 「동작 배율과 기준점」의 값)을 읽는다.
  const spriteInfo = new Map();
  const readSprite = (element) => {
    const key = element.dataset.sprite;
    if (!spriteInfo.has(key)) {
      const style = getComputedStyle(element);
      spriteInfo.set(key, {
        cells: Number(style.getPropertyValue('--cells')),
        fps: Number(style.getPropertyValue('--fps')),
        once: style.getPropertyValue('--once').trim() === '1',
      });
    }
    return spriteInfo.get(key);
  };

  // 접점 3절의 칸 식: floor(경과 × fps / 1000). 한 번 재생은 끝 칸에서 멈추고 반복은 칸 수로 나눈 나머지다.
  const frameAt = (info, elapsed) => {
    const frame = Math.floor((elapsed * info.fps) / 1000);
    return info.once ? Math.min(frame, info.cells - 1) : frame % info.cells;
  };

  const applyStep = (actor, t) => {
    const { action } = actor.run;
    const step = action.steps.find((candidate) => t >= candidate.from && t < candidate.to);
    if (!step) {
      return;
    }

    // 그림과 칸을 같은 화면 갱신에서 바꿔 다른 띠의 칸이 끼지 않게 한다.
    const { element, mover } = actor;
    if (step.sprite) {
      if (element.dataset.sprite !== step.sprite) {
        element.dataset.sprite = step.sprite;
      }
      element.classList.add('is-acting');
      element.style.setProperty('--frame', String(frameAt(readSprite(element), t - step.since)));
    }

    const progress = (t - step.from) / (step.to - step.from);
    const x = step.x[0] + (step.x[1] - step.x[0]) * progress;
    mover.style.setProperty('--x', x.toFixed(4));
    mover.style.setProperty('--face', String(step.face));

    for (const name of actor.effectNames) {
      const effect = effects[name];
      const timing = step.fx[name];
      if (!timing) {
        effect.classList.remove('is-on');
        continue;
      }
      if (timing.at !== undefined) {
        effect.style.setProperty('--dx', String(timing.at));
      }
      effect.style.setProperty('--frame', String(frameAt(readSprite(effect), t - timing.since)));
      effect.classList.add('is-on');
    }
  };

  // 끝나면 첫 단계의 방향으로 대기 동작에 돌아온다. 마지막 단계가 대기 띠였으면 칸 흐름을 끊지 않고 CSS 반복으로 넘긴다.
  const finish = (actor, t) => {
    const lastStep = actor.run.action.steps[actor.run.action.steps.length - 1];
    const continuing = lastStep.sprite === actor.idleSprite && motionAllowed();
    actor.run = null;

    const { element, mover } = actor;
    element.style.setProperty('--loop-from', continuing ? String(Math.round(t - lastStep.since)) : '0');
    element.dataset.sprite = actor.idleSprite;
    element.classList.remove('is-acting');
    element.style.removeProperty('--frame');
    mover.style.removeProperty('--x');
    mover.style.removeProperty('--face');
    for (const name of actor.effectNames) {
      effects[name].classList.remove('is-on');
    }
  };

  let actionRequest = 0;
  const tickActions = (now) => {
    actionRequest = 0;
    let busy = false;
    for (const actor of Object.values(actors)) {
      if (!actor.run) {
        continue;
      }
      // 화면 갱신 시각이 누른 시각보다 조금 앞설 수 있어 0 아래로 내려가지 않게 한다.
      const t = Math.max(0, now - actor.run.start);
      if (t >= actor.run.action.end) {
        finish(actor, t);
        continue;
      }
      applyStep(actor, t);
      busy = true;
    }
    if (busy) {
      actionRequest = window.requestAnimationFrame(tickActions);
    }
  };

  // 스킬 띠·효과 사본은 무대가 가까워지면 받아 둔다(누른 첫 칸이 비지 않게). 받은 그림을 붙잡아 디코딩된 채로 둔다.
  const preloaded = [];
  const preload = () => {
    if (stage.hasAttribute('data-ready')) {
      return;
    }
    stage.setAttribute('data-ready', '');
    const urls = new Set(Object.values(effects).map((effect) => effect.dataset.sprite));
    for (const action of Object.values(ACTIONS)) {
      for (const step of action.steps) {
        if (step.sprite) {
          urls.add(step.sprite);
        }
      }
    }
    for (const url of urls) {
      const image = new Image();
      image.src = url;
      image.decode().catch(() => {});
      preloaded.push(image);
    }
  };

  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        observer.disconnect();
        preload();
      }
    }, { rootMargin: '100% 0px' });
    observer.observe(stage);
  } else {
    preload();
  }

  const play = (name) => {
    const action = ACTIONS[name];
    if (!action || !motionAllowed()) {
      return;
    }
    preload();
    const actor = actors[action.actor];
    const now = performance.now();
    // 한 번 누름이 두 이벤트(keyup과 click)로 올 수 있다. 같은 동작을 막 시작했으면 무시한다.
    if (actor.run && actor.run.action === action && now - actor.run.start < 60) {
      return;
    }
    if (actor.run) {
      finish(actor, now - actor.run.start);
    }
    actor.run = { action, start: now };
    applyStep(actor, 0);
    if (!actionRequest) {
      actionRequest = window.requestAnimationFrame(tickActions);
    }
  };

  const stopAll = () => {
    for (const actor of Object.values(actors)) {
      if (actor.run) {
        finish(actor, 0);
      }
    }
  };

  // 조작마다 따로 고르는 라디오다(이름이 저마다 달라 Tab이 하나하나 닿는다). 이미 고른 것을 다시 눌러도 click은 오므로 click마다 재생하고,
  // 같은 캐릭터의 다른 스킬 선택은 풀어 지금 고른 스킬만 표시한다.
  const controls = [...document.querySelectorAll('[data-action]')];
  for (const input of controls) {
    const name = input.dataset.action;
    input.addEventListener('click', () => {
      for (const other of controls) {
        if (other !== input && ACTIONS[other.dataset.action].actor === ACTIONS[name].actor) {
          other.checked = false;
        }
      }
      play(name);
    });
    // Chrome은 이미 고른 라디오에서 Space를 떼도 click을 보내지 않아서 keyup에서 다시 재생한다.
    // keyup 처리는 브라우저 기본 동작보다 먼저라 checked는 누르기 전 상태다(안 고른 라디오는 이어지는 click이 맡는다).
    input.addEventListener('keyup', (event) => {
      if (event.key === ' ' && input.checked) {
        play(name);
      }
    });
  }

  const onMotionChange = () => {
    if (!motionAllowed()) {
      stopAll();
    }
    requestRender();
  };

  motionStop.addEventListener('change', onMotionChange);
  reducedMotion.addEventListener('change', () => {
    if (reducedMotion.matches) {
      motionStop.checked = true;
    }
    onMotionChange();
  });
})();
