import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-testimonials',
  templateUrl: './testimonials.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Testimonials {}
