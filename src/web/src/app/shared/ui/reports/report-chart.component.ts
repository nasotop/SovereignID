import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  viewChild,
} from '@angular/core';
import { Chart } from '@antv/g2';

export type ReportChartMode = 'line' | 'grouped-line' | 'bar-horizontal';

export interface ReportLinePoint {
  time: string;
  value: number;
}

export interface ReportGroupedLinePoint {
  time: string;
  value: number;
  group: string;
}

export interface ReportBarPoint {
  category: string;
  value: number;
}

@Component({
  selector: 'app-report-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div #container class="h-64 w-full min-h-[16rem]"></div>
  `,
})
export class ReportChartComponent implements OnDestroy {
  readonly mode = input.required<ReportChartMode>();
  readonly lineData = input<readonly ReportLinePoint[]>([]);
  readonly groupedLineData = input<readonly ReportGroupedLinePoint[]>([]);
  readonly barData = input<readonly ReportBarPoint[]>([]);

  private readonly containerRef = viewChild.required<ElementRef<HTMLElement>>('container');
  private chart: Chart | null = null;
  private resizeObserver: ResizeObserver | null = null;

  constructor() {
    effect(() => {
      this.mode();
      this.lineData();
      this.groupedLineData();
      this.barData();
      this.renderChart();
    });
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  private renderChart(): void {
    const container = this.containerRef().nativeElement;
    this.destroyChart();

    const chart = new Chart({
      container,
      autoFit: true,
      height: 256,
      theme: 'classicDark',
    });

    const mode = this.mode();

    if (mode === 'line') {
      chart.options({
        type: 'line',
        data: [...this.lineData()],
        encode: { x: 'time', y: 'value', shape: 'smooth' },
        scale: { x: { type: 'band' } },
        axis: { x: { title: false }, y: { title: false } },
      });
    } else if (mode === 'grouped-line') {
      chart.options({
        type: 'view',
        children: [
          {
            type: 'line',
            data: [...this.groupedLineData()],
            encode: { x: 'time', y: 'value', color: 'group', shape: 'smooth' },
            scale: {
              x: { type: 'band' },
              color: {
                domain: ['Validas', 'Invalidas'],
                range: ['#34d399', '#f87171'],
              },
            },
            axis: { x: { title: false }, y: { title: false } },
          },
        ],
      });
    } else {
      chart.options({
        type: 'interval',
        data: [...this.barData()],
        coordinate: { transform: [{ type: 'transpose' }] },
        encode: { x: 'category', y: 'value' },
        scale: { x: { type: 'band' } },
        axis: { x: { title: false }, y: { title: false } },
        style: { fill: '#8b5cf6' },
      });
    }

    chart.render();
    this.chart = chart;

    this.resizeObserver = new ResizeObserver(() => {
      void chart.render();
    });
    this.resizeObserver.observe(container);
  }

  private destroyChart(): void {
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    this.chart?.destroy();
    this.chart = null;
  }
}
