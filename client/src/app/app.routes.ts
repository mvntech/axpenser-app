import { Routes } from '@angular/router';
import { PublicLayout } from './layout/public-layout/public-layout';
import { AuthLayout } from './layout/auth-layout/auth-layout';

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
  {
    path: '',
    component: AuthLayout,
    loadChildren: () =>
      import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES),
  },
  { path: '**', redirectTo: '', pathMatch: 'full' },
];
