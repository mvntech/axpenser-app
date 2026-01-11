import { Component } from '@angular/core';
import { Cta } from '../../../components/landing-page/cta/cta';
import { Features } from '../../../components/landing-page/features/features';
import { Hero } from '../../../components/landing-page/hero/hero';
import { HowItWorks } from '../../../components/landing-page/how-it-works/how-it-works';
import { Testimonials } from '../../../components/landing-page/testimonials/testimonials';

@Component({
  selector: 'app-landing-page',
  imports: [Cta, Features, Hero, HowItWorks, Testimonials],
  templateUrl: './landing-page.html',
})
export class LandingPage {}
