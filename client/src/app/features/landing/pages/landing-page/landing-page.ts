import { Component } from '@angular/core';
import { Cta } from '../../../components/cta/cta';
import { Features } from '../../../components/features/features';
import { Hero } from '../../../components/hero/hero';
import { HowItWorks } from '../../../components/how-it-works/how-it-works';
import { Testimonials } from '../../../components/testimonials/testimonials';
import { TrustedBy } from '../../../components/trusted-by/trusted-by';

@Component({
  selector: 'app-landing-page',
  imports: [Cta, Features, Hero, HowItWorks, Testimonials, TrustedBy],
  templateUrl: './landing-page.html',
})
export class LandingPage {}
