import React, {useEffect, useRef} from 'react';
import {usePrefersReducedMotion} from './hooks';
import styles from './home.module.css';

const COUNT = 440;
const LERP = 0.12;

function seedParticles(width, height) {
  const particles = [];
  for (let i = 0; i < COUNT; i += 1) {
    particles.push({
      x: Math.random() * width,
      y: Math.random() * height,
      length: 4 + Math.random() * 4,
      angle: Math.random() * Math.PI * 2,
      target: 0,
      opacity: 0.16 + Math.random() * 0.22,
    });
  }
  return particles;
}

function shortestDelta(from, to) {
  let delta = to - from;
  while (delta > Math.PI) {
    delta -= Math.PI * 2;
  }
  while (delta < -Math.PI) {
    delta += Math.PI * 2;
  }
  return delta;
}

export default function Atmosphere() {
  const canvasRef = useRef(null);
  const reduced = usePrefersReducedMotion();

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return undefined;
    }
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return undefined;
    }

    let particles = [];
    let raf = 0;
    let running = true;
    let mouseX = null;
    let mouseY = null;

    const resize = () => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const width = Math.max(1, canvas.clientWidth);
      const height = Math.max(1, canvas.clientHeight);
      canvas.width = width * dpr;
      canvas.height = height * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      particles = seedParticles(width, height);
    };

    const draw = () => {
      const width = Math.max(1, canvas.clientWidth);
      const height = Math.max(1, canvas.clientHeight);
      ctx.clearRect(0, 0, width, height);
      ctx.strokeStyle = '#ff7a18';
      ctx.lineCap = 'round';
      ctx.lineWidth = 1.15;

      let stillMoving = false;
      for (const particle of particles) {
        if (mouseX !== null && !reduced) {
          particle.target = Math.atan2(mouseY - particle.y, mouseX - particle.x);
          const delta = shortestDelta(particle.angle, particle.target);
          particle.angle += delta * LERP;
          if (Math.abs(delta) > 0.004) {
            stillMoving = true;
          }
        }

        const dx = Math.cos(particle.angle) * particle.length;
        const dy = Math.sin(particle.angle) * particle.length;
        ctx.globalAlpha = particle.opacity;
        ctx.beginPath();
        ctx.moveTo(particle.x - dx, particle.y - dy);
        ctx.lineTo(particle.x + dx, particle.y + dy);
        ctx.stroke();
      }
      ctx.globalAlpha = 1;
      return stillMoving;
    };

    const tick = () => {
      if (!running) {
        return;
      }
      const stillMoving = draw();
      raf = stillMoving ? window.requestAnimationFrame(tick) : 0;
    };

    const onMove = (event) => {
      mouseX = event.clientX;
      mouseY = event.clientY;
      if (!reduced && running && raf === 0) {
        raf = window.requestAnimationFrame(tick);
      }
    };

    const onVisibility = () => {
      if (document.hidden) {
        running = false;
        window.cancelAnimationFrame(raf);
        raf = 0;
        return;
      }
      running = true;
      draw();
      if (!reduced && mouseX !== null) {
        raf = window.requestAnimationFrame(tick);
      }
    };

    resize();
    draw();
    window.addEventListener('resize', resize);
    window.addEventListener('mousemove', onMove, {passive: true});
    document.addEventListener('visibilitychange', onVisibility);

    return () => {
      running = false;
      window.cancelAnimationFrame(raf);
      window.removeEventListener('resize', resize);
      window.removeEventListener('mousemove', onMove);
      document.removeEventListener('visibilitychange', onVisibility);
    };
  }, [reduced]);

  return (
    <div className={styles.atmosphere} aria-hidden="true">
      <span className={styles.dots} />
      <span className={styles.orbA} />
      <span className={styles.orbB} />
      <span className={styles.orbC} />
      <canvas className={styles.particles} ref={canvasRef} />
    </div>
  );
}
