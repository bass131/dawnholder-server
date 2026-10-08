// Dawnholder 소개 페이지의 움직임·조작 보조. 글과 그림을 새로 만들지 않는다. 스크립트가 없어도 모든 글과 그림이 보이고,
// 스크립트가 켜지면 직업 탭이 고르지 않은 직업의 패널만 숨긴다(접점 보충 3).
// 맡는 일 여섯 가지:
// 1. 먼 장면의 띠 그림 늦게 받기: 게임 소개 2~5장과 몬스터 패널이 가까워지면 그 띠 그림을 받게 한다.
// 2. 첫 화면 대문 그림의 깊이: 스크롤 양을 .hero의 --sy에 넣는다(그림이 조금 늦게 따라 내려감).
// 3. 로고 띠 바꿔 끼우기: 정지 사본으로 먼저 보인 로고를, 움직임이 허용되면 첫 화면이 다 뜬 뒤 16칸 띠로 바꾼다.
// 4. 게임 소개 슬라이드: 넘김 단추를 보이고 한 번에 한 장씩 옮긴다(접점 보충 2). 자동 넘김은 없다.
// 5. 직업 탭: 기사·마법사 탭으로 패널 하나만 보이게 한다(접점 보충 3).
// 6. 직업 절 스킬 재생: 스킬 이름 단추(data-action)를 누르면 사이트 README 「동작 맞물림」 표대로 캐릭터와 효과를 재생한다.
// 움직임 허용의 정본은 「움직임 멈춤」 체크박스(data-motion-toggle)와 움직임 줄이기 설정이다.
// 둘 중 하나라도 멈춤이면 이 스크립트도 아무것도 움직이지 않는다(styles.css도 같은 상태로 반복 움직임을 멈춘다).
// 멈춤이면 슬라이드는 넘김 움직임 없이 바로 옮긴다. 탭 바꿈은 원래 움직임 없이 바로 바뀐다.
(() => {
  'use strict';

  // ── 1. 먼 장면의 띠 그림 늦게 받기 ──
  // 스크립트가 켜진 브라우저에서 styles.css는 게임 소개 2~5장과 몬스터 패널의 띠 그림을 비워 둔다(디자인 비평 C26).
  // 그 묶음이 화면에 들어오면(몬스터는 화면 반 높이 앞에서) is-near를 붙여 그림을 받게 한다. 다른 일보다 먼저 건다.
  // 슬라이드는 묶음이 화면에 든 뒤에야 2장 이후를 볼 수 있어 앞당기지 않는다(첫 화면에서 받지 않게).
  const DEFERRED = [['[data-carousel]', '[data-carousel-slide]', '0px'], ['.monsters', '.monster', '50% 0px']];
  for (const [groupSelector, itemSelector, margin] of DEFERRED) {
    for (const group of document.querySelectorAll(groupSelector)) {
      const mark = () => {
        for (const item of group.querySelectorAll(itemSelector)) {
          item.classList.add('is-near');
        }
      };
      if (!('IntersectionObserver' in window)) {
        mark();
        continue;
      }
      const observer = new IntersectionObserver((entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          observer.disconnect();
          mark();
        }
      }, { rootMargin: margin });
      observer.observe(group);
    }
  }

  const motionStop = document.querySelector('[data-motion-toggle]');
  if (!motionStop) {
    return;
  }

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

  // 움직임 줄이기 설정이면 「움직임 멈춤」도 체크된 모양으로 맞춘다(CSS는 설정만으로 이미 멈춘다).
  if (reducedMotion.matches) {
    motionStop.checked = true;
  }

  const motionAllowed = () => !motionStop.checked && !reducedMotion.matches;

  // ── 2. 첫 화면 깊이 ──
  // 대문 그림은 한 장이라 마우스 깊이는 두지 않고, 스크롤한 만큼만 그림을 조금 늦게 내려 보낸다(styles.css .art-layer).
  const hero = document.querySelector('.hero');
  let renderRequest = 0;

  const render = () => {
    renderRequest = 0;
    if (hero) {
      const scroll = motionAllowed() ? Math.min(window.scrollY, hero.offsetHeight) : 0;
      hero.style.setProperty('--sy', scroll.toFixed(1));
    }
  };

  const requestRender = () => {
    if (!renderRequest) {
      renderRequest = window.requestAnimationFrame(render);
    }
  };

  // 스크롤은 읽기만 한다(passive). 페이지 스크롤을 가로채지 않는다.
  window.addEventListener('scroll', requestRender, { passive: true });
  window.addEventListener('resize', requestRender);

  // ── 3. 로고 띠 바꿔 끼우기 ──
  // 로고(16칸 띠, 약 0.95MB)는 첫 화면에서 가장 큰 그림이라 HTML에는 첫 칸과 같은 정지 사본(약 0.06MB)을 둔다(디자인 비평 C25).
  // 움직임이 허용되면 첫 화면 등장 연출(1.3초)이 끝나고 브라우저가 한가할 때 띠를 받아 디코딩까지 마친 뒤 바꾼다.
  // 띠의 첫 칸이 정지 사본과 같은 그림이라 바뀌는 순간이 보이지 않는다. 멈춤이면 받지 않고, 나중에 움직임을 켜면 그때 받는다.
  const LOGO_STRIP = 'images/logo.webp';
  const LOGO_AFTER_MS = 1300;
  const logos = [...document.querySelectorAll('.logo-strip[data-sprite="images/logo-still.webp"]')];
  let logoRequested = false;

  const swapLogo = () => {
    if (logoRequested || logos.length === 0 || !motionAllowed()) {
      return;
    }
    logoRequested = true;
    const strip = new Image();
    strip.src = LOGO_STRIP;
    strip.decode().then(() => {
      for (const logo of logos) {
        logo.dataset.sprite = LOGO_STRIP;
      }
    }, () => {
      logoRequested = false;
    });
  };

  const whenIdle = (callback) => {
    if ('requestIdleCallback' in window) {
      window.requestIdleCallback(callback, { timeout: 2000 });
    } else {
      window.setTimeout(callback, 0);
    }
  };

  const scheduleLogo = () => {
    const wait = Math.max(0, LOGO_AFTER_MS - performance.now());
    window.setTimeout(() => whenIdle(swapLogo), wait);
  };

  if (document.readyState === 'complete') {
    scheduleLogo();
  } else {
    window.addEventListener('load', scheduleLogo, { once: true });
  }

  // ── 4. 게임 소개 슬라이드 ──
  // 트랙은 스크립트 없이도 가로 스크롤과 맞춤 멈춤으로 모든 장에 닿는다. 여기서는 넘김 단추를 보이고,
  // 단추와 좌우 화살표 키로 한 장씩 옮긴다. 손가락·트랙패드로 넘긴 경우는 스크롤이 멈춘 뒤 지금 장을 다시 잰다.
  // 한 장 넘김에 걸리는 시간(ms). 접점 보충 2의 600ms 안에 끝나고 방향이 보일 만큼만 둔다.
  const SLIDE_MS = 360;

  const setupCarousel = (carousel) => {
    const track = carousel.querySelector('[data-carousel-track]');
    const slides = [...carousel.querySelectorAll('[data-carousel-slide]')];
    const prev = carousel.querySelector('[data-carousel-prev]');
    const next = carousel.querySelector('[data-carousel-next]');
    if (!track || slides.length === 0 || !prev || !next) {
      return;
    }

    const last = slides.length - 1;
    const slideLeft = (index) => slides[index].offsetLeft - slides[0].offsetLeft;
    let index = 0;

    // 지금 장 = 왼쪽 끝이 스크롤 위치에 가장 가까운 장.
    const nearestIndex = () => {
      let nearest = 0;
      for (let candidate = 1; candidate <= last; candidate += 1) {
        if (Math.abs(slideLeft(candidate) - track.scrollLeft) < Math.abs(slideLeft(nearest) - track.scrollLeft)) {
          nearest = candidate;
        }
      }
      return nearest;
    };

    // 장마다 「n / 5」가 글로 있어서(스크립트 없이도 맞는 표시) 여기서는 단추의 막힘과 장 표시 네모만 맞춘다.
    // 끝 장에서 단추가 막힐 때 그 단추에 있던 키보드 초점은 트랙으로 옮긴다(초점이 페이지 맨 앞으로 사라지지 않게).
    const pips = [...carousel.querySelectorAll('.pip')];
    const show = () => {
      for (const [button, blocked] of [[prev, index === 0], [next, index === last]]) {
        if (blocked && document.activeElement === button) {
          track.focus({ preventScroll: true });
        }
        button.disabled = blocked;
      }
      pips.forEach((pip, position) => pip.classList.toggle('is-current', position === index));
    };

    // 화면 낭독: 스크롤이 멈춘 뒤 트랙의 보이는 상자 밖에 온전히 나간 장만 낭독에서 뺀다(접점 보충 2 개정, 디자인 비평 C17).
    // 조금이라도 걸친 장은 그대로 둔다. 트랙은 aria-live="polite"라 새로 드러난 장의 글을 읽어 준다. 장 안에는 초점 받을 요소가 없다.
    const expose = () => {
      const box = track.getBoundingClientRect();
      for (const slide of slides) {
        const rect = slide.getBoundingClientRect();
        if (rect.right <= box.left + 1 || rect.left >= box.right - 1) {
          slide.setAttribute('aria-hidden', 'true');
        } else {
          slide.removeAttribute('aria-hidden');
        }
      }
    };

    // 넘김 움직임은 브라우저 부드러운 스크롤 대신 직접 그린다. Firefox의 부드러운 스크롤은 한 장에 0.7초 남짓 걸려
    // 접점 보충 2의 600ms 안을 지키지 못해서다. 움직이는 동안만 맞춤 멈춤을 끈다(켜 두면 매 프레임 가까운 장으로 되돌린다).
    let animation = 0;
    const moveTo = (left) => {
      window.cancelAnimationFrame(animation);
      animation = 0;
      const from = track.scrollLeft;
      if (!motionAllowed() || Math.abs(left - from) < 1) {
        track.style.removeProperty('scroll-snap-type');
        track.scrollTo({ left, behavior: 'auto' });
        return;
      }
      const start = performance.now();
      track.style.setProperty('scroll-snap-type', 'none');
      const step = (now) => {
        const progress = Math.min(1, (now - start) / SLIDE_MS);
        track.scrollLeft = from + (left - from) * (1 - (1 - progress) ** 3);
        if (progress < 1) {
          animation = window.requestAnimationFrame(step);
          return;
        }
        animation = 0;
        track.style.removeProperty('scroll-snap-type');
        // 끝 프레임에서 스크롤 값이 그대로면 scroll·scrollend가 더 오지 않는다(scrollend는 움직이는 중에만 왔다). 여기서 멈춘 상태를 맞춘다.
        settle();
      };
      animation = window.requestAnimationFrame(step);
    };

    // 들어올 장은 움직이기 시작할 때 바로 낭독에 드러낸다(움직이는 동안 일부가 보이는 장). 나간 장은 멈춘 뒤 expose가 뺀다.
    const go = (delta) => {
      index = Math.min(last, Math.max(0, index + delta));
      slides[index].removeAttribute('aria-hidden');
      moveTo(slideLeft(index));
      show();
    };

    // 넘김 움직임 중의 scroll은 단추가 정한 장을 덮지 않는다.
    let settleTimer = 0;
    const settle = () => {
      window.clearTimeout(settleTimer);
      if (animation) {
        return;
      }
      index = nearestIndex();
      show();
      expose();
    };

    prev.addEventListener('click', () => go(-1));
    next.addEventListener('click', () => go(1));
    carousel.addEventListener('keydown', (event) => {
      const delta = { ArrowLeft: -1, ArrowRight: 1 }[event.key];
      if (delta === undefined) {
        return;
      }
      event.preventDefault();
      go(delta);
    });
    // scrollend가 없는 브라우저는 마지막 scroll 뒤 120ms 조용하면 멈춘 것으로 본다.
    track.addEventListener('scroll', () => {
      window.clearTimeout(settleTimer);
      settleTimer = window.setTimeout(settle, 120);
    }, { passive: true });
    track.addEventListener('scrollend', settle);
    // 폭이 바뀌면 장 너비도 바뀌므로 움직임을 멈추고 지금 장의 왼쪽 끝에 바로 맞춘다.
    window.addEventListener('resize', () => {
      window.cancelAnimationFrame(animation);
      animation = 0;
      track.style.removeProperty('scroll-snap-type');
      track.scrollTo({ left: slideLeft(index), behavior: 'auto' });
      expose();
    });

    prev.hidden = false;
    next.hidden = false;
    carousel.setAttribute('data-ready', '');
    index = nearestIndex();
    show();
    expose();
  };

  for (const carousel of document.querySelectorAll('[data-carousel]')) {
    setupCarousel(carousel);
  }

  // ── 5. 직업 탭 ──
  // 스크립트가 없으면 탭 목록은 숨은 채 두 패널이 다 보인다. 켜지면 탭 목록을 보이고 고른 탭의 패널만 남긴다.
  // 좌우 화살표는 끝에서 처음으로 돌아가며 Home·End와 함께 초점과 선택을 같이 옮긴다. 무대는 패널 밖이라 그대로이고,
  // 고른 패널 id를 묶음의 data-selected에 적어 무대의 그 캐릭터 머리 위에 표시를 세운다(styles.css 「직업」).
  const setupTabs = (root) => {
    const list = root.querySelector('[role="tablist"]');
    const tabs = [...root.querySelectorAll('[role="tab"]')];
    const panels = tabs.map((tab) => document.getElementById(tab.getAttribute('aria-controls')));
    if (!list || tabs.length === 0 || panels.includes(null)) {
      return;
    }

    const select = (chosen, moveFocus) => {
      tabs.forEach((tab, position) => {
        const selected = position === chosen;
        tab.setAttribute('aria-selected', String(selected));
        tab.tabIndex = selected ? 0 : -1;
        panels[position].hidden = !selected;
      });
      root.dataset.selected = panels[chosen].id;
      if (moveFocus) {
        tabs[chosen].focus();
      }
    };

    const lastTab = tabs.length - 1;
    tabs.forEach((tab, position) => {
      tab.addEventListener('click', () => select(position, false));
      tab.addEventListener('keydown', (event) => {
        const target = {
          ArrowRight: position === lastTab ? 0 : position + 1,
          ArrowLeft: position === 0 ? lastTab : position - 1,
          Home: 0,
          End: lastTab,
        }[event.key];
        if (target === undefined) {
          return;
        }
        event.preventDefault();
        select(target, true);
      });
    });

    const initial = tabs.findIndex((tab) => tab.getAttribute('aria-selected') === 'true');
    select(Math.max(0, initial), false);
    list.hidden = false;
  };

  for (const root of document.querySelectorAll('[data-tabs]')) {
    setupTabs(root);
  }

  // ── 6. 스킬 재생 ──
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
      button: null,
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
    actor.button?.classList.remove('is-playing');
    actor.button = null;
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
  // 「가까움」은 화면 높이의 20% 앞이다. 직업 절이 위로 올라와 100%로 두면 첫 화면을 열 때 바로 받아 버린다(약 0.45MB).
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
    }, { rootMargin: '20% 0px' });
    observer.observe(stage);
  } else {
    preload();
  }

  // 같은 캐릭터가 재생 중이면 그 재생을 끝내고 새로 시작한다. 재생 중인 스킬 단추는 금색으로 찬다.
  const play = (name, button) => {
    const action = ACTIONS[name];
    if (!action || !motionAllowed()) {
      return;
    }
    preload();
    const actor = actors[action.actor];
    const now = performance.now();
    if (actor.run) {
      finish(actor, now - actor.run.start);
    }
    actor.run = { action, start: now };
    actor.button = button;
    button.classList.add('is-playing');
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

  // 스킬 이름은 단추라 Enter·Space·누름이 모두 click 하나로 온다. 누를 때마다 처음부터 재생한다.
  for (const button of document.querySelectorAll('[data-action]')) {
    button.addEventListener('click', () => play(button.dataset.action, button));
  }

  const onMotionChange = () => {
    if (motionAllowed()) {
      swapLogo();
    } else {
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
