import {
  AfterViewInit,
  Component,
  ElementRef,
  NgZone,
  OnDestroy,
  inject,
  viewChild,
} from '@angular/core';

interface NodePoint {
  x: number;
  y: number;
  vx: number;
  vy: number;
  radius: number;
}

@Component({
  selector: 'app-blockchain-background',
  standalone: true,
  template: `
    <canvas #canvas class="blockchain-canvas" aria-hidden="true"></canvas>
  `,
  styles: `
    .blockchain-canvas {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
    }
  `,
})
export class BlockchainBackgroundComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef =
    viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly zone = inject(NgZone);

  private animationFrameId = 0;
  private resizeObserver: ResizeObserver | null = null;
  private readonly resizeListener = (): void => this.resize();
  private nodes: NodePoint[] = [];
  private reducedMotion = false;

  ngAfterViewInit(): void {
    if (this.isTestDom()) {
      return;
    }

    this.reducedMotion = typeof window.matchMedia === 'function'
      ? window.matchMedia('(prefers-reduced-motion: reduce)').matches
      : false;

    this.zone.runOutsideAngular(() => {
      this.resize();
      this.watchResize();

      if (this.reducedMotion) {
        this.draw(0);
        return;
      }

      if (typeof requestAnimationFrame === 'function') {
        this.tick(0);
      } else {
        this.draw(0);
      }
    });
  }

  ngOnDestroy(): void {
    if (typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(this.animationFrameId);
    }
    this.resizeObserver?.disconnect();
    window.removeEventListener?.('resize', this.resizeListener);
  }

  private tick(time: number): void {
    this.draw(time);
    this.animationFrameId = requestAnimationFrame((nextTime) => this.tick(nextTime));
  }

  private resize(): void {
    const canvas = this.canvasRef().nativeElement;
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);

    canvas.width = Math.max(1, Math.floor(rect.width * dpr));
    canvas.height = Math.max(1, Math.floor(rect.height * dpr));

    const targetCount = Math.max(26, Math.min(58, Math.floor(rect.width / 28)));
    this.nodes = Array.from({ length: targetCount }, () => ({
      x: Math.random() * canvas.width,
      y: Math.random() * canvas.height,
      vx: (Math.random() - 0.5) * 0.18 * dpr,
      vy: (Math.random() - 0.5) * 0.18 * dpr,
      radius: (1.2 + Math.random() * 1.8) * dpr,
    }));
  }

  private watchResize(): void {
    if (typeof ResizeObserver === 'function') {
      this.resizeObserver = new ResizeObserver(() => this.resize());
      this.resizeObserver.observe(this.canvasRef().nativeElement);
      return;
    }

    window.addEventListener?.('resize', this.resizeListener);
  }

  private draw(time: number): void {
    const canvas = this.canvasRef().nativeElement;
    const context = canvas.getContext('2d');
    if (!context) {
      return;
    }

    context.clearRect(0, 0, canvas.width, canvas.height);
    this.drawGrid(context, canvas, time);
    this.updateNodes(canvas);
    this.drawConnections(context, canvas);
    this.drawNodes(context);
  }

  private drawGrid(
    context: CanvasRenderingContext2D,
    canvas: HTMLCanvasElement,
    time: number,
  ): void {
    const gradient = context.createRadialGradient(
      canvas.width * 0.48,
      canvas.height * 0.42,
      0,
      canvas.width * 0.48,
      canvas.height * 0.42,
      Math.max(canvas.width, canvas.height) * 0.8,
    );
    gradient.addColorStop(0, 'rgba(14, 165, 233, 0.12)');
    gradient.addColorStop(0.42, 'rgba(15, 23, 42, 0.08)');
    gradient.addColorStop(1, 'rgba(2, 6, 23, 0)');

    context.fillStyle = gradient;
    context.fillRect(0, 0, canvas.width, canvas.height);

    const spacing = 74 * Math.min(window.devicePixelRatio || 1, 2);
    const offset = this.reducedMotion ? 0 : (time * 0.006) % spacing;
    context.strokeStyle = 'rgba(148, 163, 184, 0.045)';
    context.lineWidth = 1;

    for (let x = -spacing + offset; x < canvas.width + spacing; x += spacing) {
      context.beginPath();
      context.moveTo(x, 0);
      context.lineTo(x + canvas.height * 0.28, canvas.height);
      context.stroke();
    }

    for (let y = -spacing + offset; y < canvas.height + spacing; y += spacing) {
      context.beginPath();
      context.moveTo(0, y);
      context.lineTo(canvas.width, y + canvas.width * 0.1);
      context.stroke();
    }
  }

  private updateNodes(canvas: HTMLCanvasElement): void {
    if (this.reducedMotion) {
      return;
    }

    for (const node of this.nodes) {
      node.x += node.vx;
      node.y += node.vy;

      if (node.x < 0 || node.x > canvas.width) {
        node.vx *= -1;
      }

      if (node.y < 0 || node.y > canvas.height) {
        node.vy *= -1;
      }
    }
  }

  private drawConnections(context: CanvasRenderingContext2D, canvas: HTMLCanvasElement): void {
    const maxDistance = Math.min(canvas.width, canvas.height) * 0.2;

    for (let index = 0; index < this.nodes.length; index += 1) {
      const current = this.nodes[index];

      for (let nextIndex = index + 1; nextIndex < this.nodes.length; nextIndex += 1) {
        const next = this.nodes[nextIndex];
        const distance = Math.hypot(current.x - next.x, current.y - next.y);

        if (distance > maxDistance) {
          continue;
        }

        const alpha = (1 - distance / maxDistance) * 0.28;
        context.strokeStyle = `rgba(34, 211, 238, ${alpha})`;
        context.lineWidth = 1;
        context.beginPath();
        context.moveTo(current.x, current.y);
        context.lineTo(next.x, next.y);
        context.stroke();
      }
    }
  }

  private drawNodes(context: CanvasRenderingContext2D): void {
    for (const node of this.nodes) {
      context.fillStyle = 'rgba(226, 232, 240, 0.7)';
      context.beginPath();
      context.arc(node.x, node.y, node.radius, 0, Math.PI * 2);
      context.fill();

      context.strokeStyle = 'rgba(34, 211, 238, 0.28)';
      context.lineWidth = 1;
      context.beginPath();
      context.arc(node.x, node.y, node.radius * 3.2, 0, Math.PI * 2);
      context.stroke();
    }
  }

  private isTestDom(): boolean {
    return window.navigator?.userAgent.toLowerCase().includes('jsdom') ?? false;
  }
}
