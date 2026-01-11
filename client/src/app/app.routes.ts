import { Routes } from '@angular/router';
import { PublicLayout } from './layout/public-layout/public-layout';

export const routes: Routes = [
  {
    path: '',
    component: PublicLayout,
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/landing/pages/landing-page/landing.routes').then(m => m.LANDING_ROUTES),
      },
    ],
  },
];
